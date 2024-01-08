namespace StaticCMS.App

open System
open FsToolbox.Core

module InteractiveMode =

    open StaticCMS.App.Actions
    open StaticCMS.App.Common

    [<AutoOpen>]
    module Helpers =

        type UserOption =
            { Key: string
              InputValue: string
              Message: string
              Hidden: bool
              Normalize: bool }

            static member Create(key, inputValue, message, ?hidden, ?normalize) =
                { Key = key
                  InputValue = inputValue
                  Message = message
                  Hidden = hidden |> Option.defaultValue false
                  Normalize = normalize |> Option.defaultValue false }

        let stringToOption (str: string) =
            match String.IsNullOrWhiteSpace str with
            | true -> None
            | false -> Some str

        let readInput _ = Console.ReadLine()

        let readOptionalInput _ = readInput () |> stringToOption

        let inputPrompt (message: string) =
            Console.WriteLine message
            printf "> "

            readInput ()

        let optionalInputPrompt (message: string) =
            Console.WriteLine message
            printf "> "

            readOptionalInput ()

        let optionPrompt (message: string) (options: UserOption list) =
            let rec handle () =
                printf "> "

                let input = Console.ReadLine()

                match options |> List.tryFind (fun o -> o.InputValue = input) with
                | Some o -> o
                | None ->
                    ConsoleIO.printWarning $"Invalid option `{input}`"
                    handle ()

            Console.WriteLine message

            options
            |> List.filter (fun o -> o.Hidden |> not)
            |> List.iter (fun o -> Console.WriteLine o.Message)

            handle ()

    let handleAction (ctx: AppContext) (site: string) =

        ()

    let createOrLoadSite (ctx: AppContext) =

        printfn "Please select an option:"
        printfn "1. Create a new site"
        printfn "2. Load a site"

        let selectedOption = readInput

        let createSite () =

            let siteName = inputPrompt "Enter new site name:"
            let url = inputPrompt "Enter new site url:"

            let root =
                inputPrompt $"Enter site root directory (if blank or empty default path will be used):"


            failwith "TODO"
            
            (*
            match InitializeSite.run ctx siteName url None with
            | Ok _ -> Ok siteName
            | Error e -> Error e
*)            

        let rec loadSite () =

            printfn "Enter site name: "


            let siteName = Console.ReadLine()

            match ctx.Store.GetSite siteName with
            | Some _ -> Ok siteName
            | None ->
                ConsoleIO.printWarning $"Site `{siteName}` not found"
                loadSite ()


        let option =
            [
                UserOption.Create("new-site", "1", "1. Create a new site")
                UserOption.Create("load-site", "2", "2. Load a site")
            ]
            |> optionPrompt "Please select an option:"
        
        match option.Key with
        | "new-site" -> createSite ()
        | "load-site" -> loadSite ()
        | _ -> Error "Unknown option"
        
    let run _ =
        
        
        match createContext None None with
        | Ok ctx ->
            match createOrLoadSite ctx with
            | Ok siteName->
                
                
                ()
            | Error e -> ConsoleIO.printError e
        | Error e -> ConsoleIO.printError $"Failed to create app context. Error: {e}"
        
        
