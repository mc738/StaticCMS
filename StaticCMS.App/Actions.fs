namespace StaticCMS.App

open System
open System.Security.Cryptography.Xml
open Faaz.Messaging
open StaticCMS.DataStore

module Actions =

    module Logging =
        
        open System
        open System.Diagnostics
        
        [<RequireQualifiedAccess>]
        type LogItemType =
            | Information of From: string * Message: string
            | Error of From: string * Message: string * Exception: exn option
            | Warning of From: string * Message: string * Exception: exn option
            | Success of From: string * Message: string
            | Trace of From: string * Message: string * Reference: string
            | Debug of From: string * Message: string

            member lit.Print() =
                match lit with
                | LogItemType.Information (from, message) ->
                    Console.ForegroundColor <- ConsoleColor.DarkGray
                    printfn $"[{DateTime.UtcNow} info  ] {from} - {message}"
                | LogItemType.Error (from, message, exceptionOption) ->
                    Console.ForegroundColor <- ConsoleColor.Red

                    let exnMessage =
                        exceptionOption
                        |> Option.map (fun exn -> $" Exception: {exn.Message}")
                        |> Option.defaultValue ""

                    printfn $"[{DateTime.UtcNow} error ] {from} - {message}{exnMessage}"
                | LogItemType.Warning (from, message, exceptionOption) ->
                    Console.ForegroundColor <- ConsoleColor.DarkYellow

                    let exnMessage =
                        exceptionOption
                        |> Option.map (fun exn -> $" Exception: {exn.Message}")
                        |> Option.defaultValue ""

                    printfn $"[{DateTime.UtcNow} warn  ] {from} - {message}{exnMessage}"
                | LogItemType.Success (from, message) ->
                    Console.ForegroundColor <- ConsoleColor.Green
                    printfn $"[{DateTime.UtcNow} ok    ] {from} - {message}"
                | LogItemType.Trace (from, message, reference) ->
                    Console.ForegroundColor <- ConsoleColor.DarkMagenta
                    printfn $"[{DateTime.UtcNow} trace ] {from} - {message} (Reference: {reference})"
                | LogItemType.Debug (from, message) ->
                    Console.ForegroundColor <- ConsoleColor.Magenta
                    printfn $"[{DateTime.UtcNow} debug ] {from} - {message}"

                Console.ResetColor()

        type Logger() =

            let agent =
                MailboxProcessor<LogItemType>.Start
                    (fun inbox ->
                        let rec loop () =
                            async {
                                let! item = inbox.Receive()

                                item.Print()

                                return! loop ()
                            }

                        loop ())

            member _.LogInfo(from, message) =
                agent.Post(LogItemType.Information(from, message))

            member _.LogError(from, message, ?exn) =
                agent.Post(LogItemType.Error(from, message, exn))

            member _.LogWarning(from, message, ?exn) =
                agent.Post(LogItemType.Warning(from, message, exn))

            member _.LogSuccess(from, message) =
                agent.Post(LogItemType.Success(from, message))

            member _.LogTrace(from, message, reference) =
                agent.Post(LogItemType.Trace(from, message, reference))

            member _.LogDebug(from, message) =
                if Debugger.IsAttached then
                    agent.Post(LogItemType.Debug(from, message))
    
    
    open Logging
    
    type AppContext = {
        Store: StaticStore
        Log: Logger
    }
    
    type ActionType =
        | InitializeSite of Name: string * Url: string * Root: string
        | AddPage of Site: string * Name: string * NameSlug: string

    let createRef _ = Guid.NewGuid().ToString("n")

    type ActionResult<'T> =
        | Success of Message: string * Result: 'T option
        | Skipped of Message: string * Result: 'T option
        | Failed of Message: string

    let addSite (ctx: AppContext) (name: string) (url: string) (root: string) =
        match ctx.Store.TryAddSite(name, url, root) with
        | AddSiteResult.Success -> ctx.Log.LogSuccess("add-site", $"Site `{name}` added.")
        | AddSiteResult.AlreadyExists -> ctx.Log.LogInfo("add-site", $"Site `{name}` already exists.")
        | AddSiteResult.Failure e -> ctx.Log.LogError("add-site", $"Failed to add site `{name}`. Error: {e.Message}")
        
    let addPage (store: StaticStore) (site: string) (name: string) (nameSlug: string) =
        match store.GetPage(site, name) with
        | Some p -> ActionResult.Skipped($"Page `{name}` (site: `{site}`) already exists.", Some p.Reference)
        | None ->
            let ref = createRef ()
            store.AddPage(ref, site, name, nameSlug)
            ActionResult.Success($"Page `{name}` added to site `{site}`.", Some ref)

    let addPlugin (store: StaticStore) (name: string) (pluginType: string) =
        match store.GetPlugin name with
        | Some p -> ActionResult.Skipped($"Plugin `{name}` already exists.", Some p.Name)
        | None ->
            store.AddPlugin(name, pluginType)
            ActionResult.Success($"Plugin `{name}` added.", None)