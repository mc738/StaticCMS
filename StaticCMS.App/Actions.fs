namespace StaticCMS.App

open System
open System.IO
open System.Security.Cryptography.Xml
open Faaz.Messaging
open StaticCMS
open StaticCMS.App.Options
open StaticCMS.DataStore
open StaticCMS.Pipeline

module Actions =
    
    type ActionType =
        | InitializeSite of Name: string * Url: string * Root: string
        | AddPage of Site: string * Name: string * NameSlug: string


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
            
    let initializeSite (options: InitializeSiteOptions) =
        try
            let store = getStorePath options.StorePath |> StaticStore.Create

            match store.GetSite options.Name with
            | Some _ -> Error $"Site `{options.Name}` already exists"
            | None ->
                let root =
                    options.Root
                    |> Option.defaultWith (fun _ -> Path.Combine(getStaticRoot (), "sites", options.Name))

                Directory.CreateDirectory(root) |> ignore

                store.AddSite(options.Name, options.Url, root)
                store.AddPage(options.Name, "index", "index") |> ignore
                Directory.CreateDirectory(Path.Combine(root, "data")) |> ignore
                Directory.CreateDirectory(Path.Combine(root, "fragment_templates")) |> ignore
                Directory.CreateDirectory(Path.Combine(root, "page_templates")) |> ignore
                Directory.CreateDirectory(Path.Combine(root, "pages")) |> ignore
                Directory.CreateDirectory(Path.Combine(root, "plugins")) |> ignore
                Directory.CreateDirectory(Path.Combine(root, "rendered")) |> ignore
                Directory.CreateDirectory(Path.Combine(root, "resources")) |> ignore

                let build =
                    ({ Site = options.Name
                       Steps =
                         [ { Directories = [ "$root/rendered/css"; "$root/rendered/js"; "$root/rendered/img" ] }
                           |> StepType.CreateDirectories
                           { Directories =
                               [ { From = "$root/resources/css"
                                   To = "$root/rendered/css" }
                                 { From = "$root/resources/js"
                                   To = "$root/rendered/js" }
                                 { From = "$root/resources/img"
                                   To = "$root/rendered/img" } ]
                             Files = [] }
                           |> StepType.CopyResources ] }
                    : PipelineConfiguration)
                        .Serialize()

                File.WriteAllText(Path.Combine(root, "build.json"), build)

                Ok()
        with exn ->
            Error $"Failed to initialize site. Error: {exn.Message}"

    let importTemplate (options: ImportTemplateOptions) =
        try
            match File.Exists options.Path with
            | true ->
                let storePath = getStorePath options.StorePath

                let store = StaticStore.Create storePath

                match store.GetTemplate(options.Name) with
                | None -> store.AddTemplate(options.Name, File.ReadAllBytes options.Path) |> Ok
                | Some _ -> Error $"Template `{options.Name}` already exists"
            | false -> Error $"File `{options.Path}` does not exist"
        with exn ->
            Error $"Failed to import template. Error: {exn.Message}"

    let importFragmentTemplate (options: ImportFragmentTemplateOptions) =
        try
            match File.Exists options.Path with
            | true ->
                let storePath = getStorePath options.StorePath

                let store = StaticStore.Create storePath

                match store.GetFragmentTemplate(options.Name) with
                | None -> store.AddFragmentTemplate(options.Name, File.ReadAllBytes options.Path) |> Ok
                | Some _ -> Error $"Fragment template `{options.Name}` already exists"
            | false -> Error $"File `{options.Path}` does not exist"
        with exn ->
            Error $"Failed to import template. Error: {exn.Message}"

    let renderSite (options: RenderSiteOptions) =

        let generalSettings =
            match getGeneralSettings options.ConfigurationPath with
            | Ok gs -> gs
            | Error _ -> { Paths = [] }

        let storePath = getStorePath options.StorePath

        let store = StaticStore.Create storePath

        match store.GetSite options.Name with
        | Some site ->
            match Pipeline.loadConfiguration (Path.Combine(site.RootPath, "build.json")) with
            | Ok cfg ->

                let scriptHost =
                    ({ FsiSession = Faaz.ScriptHost.fsiSession () }: Faaz.ScriptHost.HostContext)

                let knownPaths =
                    ("$root", site.RootPath)
                    :: (generalSettings.Paths |> List.ofSeq |> List.map (fun p -> $"${p.Name}", p.Path))
                    |> Map.ofList

                let ctx = PipelineContext.Create(store, scriptHost, knownPaths)

                Pipeline.run ctx cfg
            | Error e -> Error $"Failed to create pipeline context: {e}"
        | None -> Error $"Unknown site `{options.Name}`."