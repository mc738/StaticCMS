namespace StaticCMS.App

open System
open System.ComponentModel.DataAnnotations
open System.IO
open Spectre.Console
open StaticCMS
open StaticCMS.Actions.Common
open StaticCMS.App.Common

module InteractiveMode =

    open FsToolbox.Extensions.Strings
    open StaticCMS.DataStore
    open StaticCMS.App.Common.Logging
    //open StaticCMS.App.Actions
    open StaticCMS.App.Common
    open StaticCMS.Actions.Site

    let textPrompt prompt (optional: bool) =
        let tp = TextPrompt<string>(prompt)

        tp.PromptStyle <- "green"

        if optional then
            tp.AllowEmpty <- true

        AnsiConsole.Prompt(tp)

    let createSite (ctx: StaticCMSContext) =

        let namePrompt =
            TextPrompt<string>("Enter new site name: ")
                .Validate(fun name ->
                    match System.String.IsNullOrWhiteSpace name with
                    | true -> ValidationResult.Error "Site name can not be blank"
                    | false -> ValidationResult.Success())

        namePrompt.PromptStyle <- "green"

        let siteName = AnsiConsole.Prompt(namePrompt)

        let urlPrompt =
            TextPrompt<string>("Enter new site url: ")
                .Validate(fun name ->
                    match System.String.IsNullOrWhiteSpace name with
                    | true -> ValidationResult.Error "Url name can not be blank"
                    | false -> ValidationResult.Success())

        urlPrompt.PromptStyle <- "green"

        let url = AnsiConsole.Prompt(urlPrompt)

        let displayNamePrompt =
            TextPrompt<string>("Enter new site display name (if left blank default site name will be used): ")

        displayNamePrompt.PromptStyle <- "green"

        displayNamePrompt.AllowEmpty <- true

        let displayName = AnsiConsole.Prompt(displayNamePrompt)

        let rootPrompt =
            TextPrompt<string>("Enter new site root path (if left blank default path will be used): ")

        rootPrompt.PromptStyle <- "green"

        rootPrompt.AllowEmpty <- true

        let root = AnsiConsole.Prompt(rootPrompt)

        ({ Name = siteName
           Url = url
           DisplayName = displayName.ToOption()
           Root = root.ToOption() }
        : InitializeSite.Parameters)
        |> InitializeSite.run ctx
        |> Result.bind (fun _ ->
            match ctx.Store.GetSite siteName with
            | Some site -> Ok site
            | None -> Error "Site not created")

    let loadSite (ctx: StaticCMSContext) =

        let sitesSelectionPrompt =
            SelectionPrompt<string>()
                .AddChoices(ctx.Store.ListSites() |> List.map (fun s -> s.Name))

        sitesSelectionPrompt.Title <- "Select site"
        sitesSelectionPrompt.PageSize <- 10

        let siteName = AnsiConsole.Prompt(sitesSelectionPrompt)

        match ctx.Store.GetSite siteName with
        | Some site -> Ok site
        | None -> Error $"Site `{siteName}` not found"

    let run _ =
        match createContext None None consoleLogger with
        | Ok ctx ->
            let createOrLoadSelection =
                SelectionPrompt<string>()
                    .AddChoices([| "Create new site"; "Load existing site" |])

            createOrLoadSelection.Title <- "Please select an option"

            let site =
                match AnsiConsole.Prompt createOrLoadSelection with
                | "Create new site" -> createSite ctx
                | "Load existing site" -> loadSite ctx
                | _ -> Error "Unknown selection"

            match site with
            | Ok s ->
                let rec handle _ =
                    AnsiConsole.Clear()

                    let optionsPrompt =
                        SelectionPrompt<string>()
                            .AddChoices([| "Add page"; "Render site"; "Add plugin"; "[red]Delete site[/]" |])

                    let selection = AnsiConsole.Prompt optionsPrompt

                    match selection with
                    | "Add page" ->
                        let pageName = textPrompt "Enter new page name: " false
                        let pageNameSlug = textPrompt "Enter new page name slug: " false
                        let pageTitle = textPrompt "Enter new page title: " false

                        let includeDefaultStyles = AnsiConsole.Ask<bool>("Include default styles?")
                        let includeDefaultScripts = AnsiConsole.Ask<bool>("Include default scripts?")
                        let navigableTo = AnsiConsole.Ask<bool>("Page is navigable to?")


                        let status = AnsiConsole.Status()

                        //status.AutoRefresh <- false
                        status.Spinner <- Spinner.Known.Star

                        status.Start(
                            "Adding page",
                            fun c ->

                                ({ SiteName = s.Name
                                   SiteDisplayName = s.DisplayName |> Option.defaultValue s.Name
                                   SiteRoot = s.RootPath
                                   PageName = pageName
                                   PageNameSlug = pageNameSlug
                                   PageTitle = pageTitle
                                   PageTemplate = ""
                                   IncludeDefaultStyles = includeDefaultStyles
                                   IncludeDefaultScripts = includeDefaultScripts
                                   NavigableTo = navigableTo
                                   Styles = []
                                   Scripts = []
                                   AdditionalData = Map.empty
                                   PageFiles = [] }
                                : AddPage.Parameters)
                                |> AddPage.run ctx

                                AnsiConsole.MarkupLine("Adding to store")

                                Async.Sleep 1000 |> Async.RunSynchronously

                                AnsiConsole.MarkupLine("Generating page data")
                                Async.Sleep 1000 |> Async.RunSynchronously
                        )

                        AnsiConsole.Markup("[green]Complete. Press any key to continue[/]")
                        Console.ReadLine() |> ignore

                        handle ()
                    | "Render site" ->

                        match RenderSite.run ctx { SiteName = s.Name; ScriptHost = None } with
                        | Ok resultValue ->

                            handle ()
                        | Error errorValue -> ()
                    | "Add plugin" ->

                        let pluginSelectionPrompt =
                            SelectionPrompt<string>()
                                .AddChoices(ctx.Store.ListPlugins() |> List.map (fun s -> s.Name))

                        pluginSelectionPrompt.Title <- "Select a plugin"
                        pluginSelectionPrompt.PageSize <- 10

                        let plugin = AnsiConsole.Prompt<string>(pluginSelectionPrompt)

                        let pluginCfg = Path.Combine(ctx.RootPath, "plugins", plugin, "settings.json")

                        let result =
                            Plugins.Settings.Load pluginCfg
                            |> Result.bind (fun cfg ->
                                let args =
                                    cfg.Initialization.Args
                                    |> List.map (fun a ->
                                        let argValue = textPrompt $"Enter a value for `{a.Name}`: " false

                                        a.Name, argValue)

                                AddPlugin.run
                                    ctx
                                    { Site = s
                                      PluginName = plugin
                                      Args = args
                                      PluginSettings = cfg })

                        match result with
                        | Ok _ -> ()
                        | Error e -> ()

                    | "[red]Delete site[/]" ->
                        let confirmationPrompt =
                            TextPrompt<string>(
                                $"[red]Are you sure you want to delete `{s.Name}`?[/] To confirm enter the site name here: "
                            )

                        match AnsiConsole.Prompt(confirmationPrompt) with
                        | v when v.Equals(s.Name) ->
                            match DeleteSite.run ctx { SiteName = s.Name } with
                            | Ok _ ->
                                AnsiConsole.MarkupLine("[red]Site deleted. Press any key to continue[/]")
                                Console.ReadLine() |> ignore
                            | Error e -> AnsiConsole.MarkupLineInterpolated($"[red]Error deleting site: {e}[/]")

                        | _ ->
                            AnsiConsole.MarkupLine("Names do not match. Press any key to return to menu.")
                            Console.ReadLine() |> ignore
                            handle ()
                    | _ -> failwith "Unknown selection"


                handle ()
            | Error e -> AnsiConsole.MarkupInterpolated($"[red]Failed to create or load site. Error: {e}[/]")
        | Error e -> AnsiConsole.MarkupInterpolated($"[red]Failed to create app context. Error: {e}[/]")
