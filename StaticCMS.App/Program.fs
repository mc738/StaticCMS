open System
open System.IO
open System.Text
open System.Text.Json
open System.Text.Json.Serialization
open Fluff.Core
open FsToolbox.AppEnvironment.Args
open FsToolbox.AppEnvironment.Args.Mapping
open FsToolbox.Core
open StaticCMS
open StaticCMS.DataStore
open StaticCMS.Pipeline
open Wikd.DataStore

[<CLIMutable>]
type GeneralSettings =
    { [<JsonPropertyName("paths")>]
      Paths: NamedPath seq }

and [<CLIMutable>] NamedPath =
    { [<JsonPropertyName("name")>]
      Name: string
      [<JsonPropertyName("path")>]
      Path: string }

type Options =
    | [<CommandValue("init-site")>] InitializeSite of InitializeSiteOptions
    | [<CommandValue("add-site")>] AddSite of AddSiteOptions
    | [<CommandValue("import-template")>] ImportTemplate of ImportTemplateOptions
    | [<CommandValue("render-site")>] RenderSite of RenderSiteOptions
    | [<CommandValue("add-page")>] AddPath of AddPageOptions
    | [<CommandValue("import-fragment-template")>] ImportFragmentTemplate of ImportFragmentTemplateOptions
    | [<CommandValue("add-site-plugin")>] AddSitePlugin
    | [<CommandValue("init")>] RunInit

and InitializeSiteOptions =
    { [<ArgValue("-n", "--name")>]
      Name: string
      [<ArgValue("-u", "--url")>]
      Url: string
      [<ArgValue("-r", "--root")>]
      Root: string option
      [<ArgValue("-s", "--store")>]
      StorePath: string option }

and AddSiteOptions =
    { [<ArgValue("-n", "--name")>]
      Name: string
      [<ArgValue("-u", "--url")>]
      Url: string
      [<ArgValue("-r", "--root")>]
      Root: string }

and ImportTemplateOptions =
    { [<ArgValue("-n", "--name")>]
      Name: string
      [<ArgValue("-p", "--path")>]
      Path: string
      [<ArgValue("-s", "--store")>]
      StorePath: string option }

and RenderSiteOptions =
    { [<ArgValue("-n", "--name")>]
      Name: string
      [<ArgValue("-s", "--store")>]
      StorePath: string option
      [<ArgValue("-c", "--config")>]
      ConfigurationPath: string option
      [<ArgValue("-d", "--draft")>]
      IsDraft: bool option }

and AddPageOptions =
    { [<ArgValue("-n", "--name")>]
      Name: string
      [<ArgValue("-w", "--site")>]
      Site: string
      [<ArgValue("-c", "--config")>]
      ConfigurationPath: string option
      [<ArgValue("-s", "--store")>]
      StorePath: string option }

and ImportFragmentTemplateOptions =
    { [<ArgValue("-n", "--name")>]
      Name: string
      [<ArgValue("-p", "--path")>]
      Path: string
      [<ArgValue("-s", "--store")>]
      StorePath: string option }

let options =
    Environment.GetCommandLineArgs()
    |> List.ofArray
    |> ArgParser.tryGetOptions<Options>

let getStorePath (path: string option) =
    path
    |> Option.defaultWith (fun _ -> Environment.GetEnvironmentVariable "STATIC_CMS_STORE")

let getStaticRoot _ =
    Environment.GetEnvironmentVariable "STATIC_CMS_ROOT"

let getGeneralSettings (path: string option) =
    path
    |> Option.defaultWith (fun _ -> Path.Combine(getStaticRoot (), "settings.json"))
    |> fun p ->
        try
            File.ReadAllText p |> JsonSerializer.Deserialize<GeneralSettings> |> Ok
        with exn ->
            Error "Failed to deserialize general settings"

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

let result =
    options
    |> Result.bind (fun o ->
        match o with
        | InitializeSite initializeSiteOptions -> initializeSite initializeSiteOptions
        | AddSite addSiteOptions -> failwith "todo"
        | ImportTemplate importTemplateOptions -> failwith "todo"
        | RenderSite renderSiteOptions -> renderSite renderSiteOptions
        | AddPath addPageOptions -> failwith "todo"
        | ImportFragmentTemplate importFragmentTemplateOptions -> importFragmentTemplate importFragmentTemplateOptions
        | AddSitePlugin -> failwith "todo"
        | RunInit -> failwith "todo")

match result with
| Ok _ -> ()
| Error e -> ConsoleIO.printError e


// For more information see https://aka.ms/fsharp-console-apps
//printfn "Hello from F#"
