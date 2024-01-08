namespace StaticCMS.App

open System.ComponentModel.DataAnnotations
open Spectre.Console
open StaticCMS.App.Common

module InteractiveModeV2 =

    open FsToolbox.Extensions.Strings
    open StaticCMS.DataStore
    open StaticCMS.App.Common.Logging
    open StaticCMS.App.Actions
    open StaticCMS.App.Common

    let createContext (storePath: string option) (generalSettingsPath: string option) =

        getGeneralSettings generalSettingsPath
        |> Result.map (fun gs ->

            { Store = StaticStore.Create <| getStorePath storePath
              Log = Logger()
              GeneralSettings = gs })

    let createSite (ctx: AppContext) =

        let namePrompt =
            TextPrompt<string>("Enter new site name")
                .Validate(fun name ->
                    match System.String.IsNullOrWhiteSpace name with
                    | true -> ValidationResult.Error "Site name can not be blank"
                    | false -> ValidationResult.Success())
                
        namePrompt.PromptStyle <- "green"
        
        let siteName = AnsiConsole.Prompt(namePrompt)
        
        let urlPrompt =
            TextPrompt<string>("Enter new site url")
                .Validate(fun name ->
                    match System.String.IsNullOrWhiteSpace name with
                    | true -> ValidationResult.Error "Url name can not be blank"
                    | false -> ValidationResult.Success())
                
        urlPrompt.PromptStyle <- "green"
        
        let url = AnsiConsole.Prompt(urlPrompt)
        
        let rootPrompt =
            TextPrompt<string>("Enter new site root path (if left blank default path will be used)")
                
        rootPrompt.PromptStyle <- "green"
        
        rootPrompt.AllowEmpty <- true
        
        let root = AnsiConsole.Prompt(rootPrompt)

        // TODO change display name
        InitializeSite.run ctx siteName url (Some "Example") (root.ToOption())
        |> Result.map (fun _ -> siteName)

    let loadSite (ctx: AppContext) =
        
        let sitesSelectionPrompt =
            SelectionPrompt<string>().AddChoices(ctx.Store.ListSites() |> List.map (fun s -> s.Name))
            
        sitesSelectionPrompt.Title <- "Select site"
        
        let siteName = AnsiConsole.Prompt(sitesSelectionPrompt)
        
        match ctx.Store.GetSite siteName with
            | Some _ -> Ok siteName
            | None -> Error $"Site `{siteName}` not found"

    let run _ =
        match createContext None None with
        | Ok ctx ->
            let createOrLoadSelection =
                SelectionPrompt<string>()
                    .AddChoices([| "Create new site"; "Load existing site" |])

            createOrLoadSelection.Title <- "Please select an option"

            let siteName =
                match AnsiConsole.Prompt createOrLoadSelection with
                | "Create new site" -> createSite ctx
                | "Load existing site" -> loadSite ctx
                | _ -> Error "Unknown selection"
            
            let r =
                match siteName with
                | Ok sn -> RenderSite.run ctx sn
                | Error e -> Error e

            match r with
            | Ok v -> ()
            | Error e -> failwith e
            
            ()
        | Error e -> AnsiConsole.MarkupInterpolated($"[red]Failed to create app context. Error: {e}[/]")
