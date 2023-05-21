open System
open System.IO
open System.Text
open System.Text.Json
open System.Text.Json.Serialization
open Fluff.Core
open FsToolbox.AppEnvironment.Args
open FsToolbox.AppEnvironment.Args.Mapping
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
    | [<CommandValue("add-site")>] AddSite of AddSiteOptions
    | [<CommandValue("import-template")>] ImportTemplate of ImportTemplateOptions
    | [<CommandValue("render-site")>] RenderSite of RenderSiteOptions

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

let options =
    Environment.GetCommandLineArgs()
    |> List.ofArray
    |> ArgParser.tryGetOptions<Options>

let getStorePath (path: string option) =
    path
    |> Option.defaultWith (fun _ -> Environment.GetEnvironmentVariable "STATIC_CMS_STORE")


let getGeneralSettings (path: string option) =
    path
    |> Option.defaultWith (fun _ -> Path.Combine(Environment.GetEnvironmentVariable "STATIC_CMS_ROOT", "settings.json"))
    |> fun p ->
        try
            JsonSerializer.Deserialize<GeneralSettings> p |> Ok
        with exn ->
            Error "Failed to deserialize general settings"

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


let renderSite (options: RenderSiteOptions) =

    let generalSettings =
        match getGeneralSettings options.ConfigurationPath with
        | Ok gs -> gs
        | Error _ -> { Paths = [] }

    match Pipeline.loadConfiguration "C:\\ProjectData\\static_cms\\sites\\FPype\\build.json" with
    | Ok cfg ->

        let store = StaticStore.Create "C:\\ProjectData\\static_cms\\static_store.db"

        let scriptHost =
            ({ FsiSession = Faaz.ScriptHost.fsiSession () }: Faaz.ScriptHost.HostContext)

        match store.GetSite cfg.Site with
        | Some site ->

            let knownPaths =
                ("$root", site.RootPath)
                :: (generalSettings.Paths |> List.ofSeq |> List.map (fun p -> $"${p.Name}", p.Path))
                |> Map.ofList

            let ctx =
                PipelineContext.Create(store, scriptHost, knownPaths)

            Pipeline.run ctx cfg
        | None -> Error $"Unknown site `{cfg.Site}`."
    | Error e -> Error $"Failed to create pipeline context: {e}"


match options with
| Ok o ->
    match o with
    | AddSite addSiteOptions -> failwith "todo"
    | ImportTemplate importTemplateOptions -> failwith "todo"
    | RenderSite renderSiteOptions -> failwith "todo"


    ()
| Error e ->


    ()


// For more information see https://aka.ms/fsharp-console-apps
//printfn "Hello from F#"
