namespace StaticCMS.App

[<AutoOpen>]
module Common =

    open System
    open StaticCMS.DataStore
        
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

    let createRef _ = Guid.NewGuid().ToString("n")
