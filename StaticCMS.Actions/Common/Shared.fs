namespace StaticCMS.Actions.Common

open StaticCMS

[<AutoOpen>]
module Shared =

    open System
    open System.IO
    open System.Text.Json
    open System.Text.Json.Serialization
    open StaticCMS.DataStore

    [<CLIMutable>]
    type GeneralSettings =
        { [<JsonPropertyName("includeExamplePage")>]
          IncludeExamplePage: bool
          [<JsonPropertyName("defaultIndexBodyPage")>]
          DefaultIndexBodyPath: string
          [<JsonPropertyName("examplePageBodyPath")>]
          ExamplePageBodyPath: string
          [<JsonPropertyName("defaultIconScriptUrl")>]
          DefaultIconScriptUrl: string
          [<JsonPropertyName("defaultTheme")>]
          DefaultTheme: DefaultTheme
          [<JsonPropertyName("paths")>]
          Paths: NamedPath seq
          [<JsonPropertyName("defaultResources")>]
          DefaultResources: DefaultResource seq
          [<JsonPropertyName("defaultStyles")>]
          DefaultStyles: string seq
          [<JsonPropertyName("defaultScripts")>]
          DefaultScripts: string seq }

    and [<CLIMutable>] NamedPath =
        { [<JsonPropertyName("name")>]
          Name: string
          [<JsonPropertyName("path")>]
          Path: string }

    and [<CLIMutable>] DefaultTheme =
        { [<JsonPropertyName("name")>]
          Name: string
          [<JsonPropertyName("path")>]
          Path: string }

    and [<CLIMutable>] DefaultResource =
        { [<JsonPropertyName("path")>]
          Path: string
          [<JsonPropertyName("copyTo")>]
          CopyTo: string }

    type StaticCMSContext =
        { Store: StaticStore
          Logger: LogItem -> unit
          RootPath: string
          GeneralSettings: GeneralSettings }

    and [<RequireQualifiedAccess>] LogItem =
        | Information of From: string * Message: string
        | Error of From: string * Message: string * Exception: exn option
        | Warning of From: string * Message: string * Exception: exn option
        | Success of From: string * Message: string
        | Trace of From: string * Message: string * Reference: string
        | Debug of From: string * Message: string

    let createRef _ = Guid.NewGuid().ToString("n")

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

    let passThruLogger (_: LogItem) = ()

    let createContext (storePath: string option) (generalSettingsPath: string option) (logger: LogItem -> unit) =

        getGeneralSettings generalSettingsPath
        |> Result.map (fun gs ->

            { Store = StaticStore.Create <| getStorePath storePath
              Logger = logger
              RootPath = getStaticRoot () 
              GeneralSettings = gs })

    let serializeJson<'T> (value: 'T) =
        let options = JsonSerializerOptions()

        options.WriteIndented <- true


        JsonSerializer.Serialize(value, options)

    let deserializeJson<'T> (json: string) = JsonSerializer.Deserialize<'T> json

    let loadConfiguration (path: string) =
        JsonDocument.Parse(File.ReadAllText path).RootElement
        |> Pipeline.PipelineConfiguration.Deserialize
