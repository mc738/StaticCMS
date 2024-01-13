namespace StaticCMS.App.Common

open System.Diagnostics
open Spectre.Console
open StaticCMS.Actions.Common

[<AutoOpen>]
module Shared =

    let consoleLogger (item: StaticCMS.Actions.Common.Shared.LogItem) =
        match item with
        | LogItem.Information(from, message) -> AnsiConsole.MarkupLineInterpolated($"[gray]{from} - {message}[/]")
        | LogItem.Error(from, message, ``exception``) -> AnsiConsole.MarkupLineInterpolated($"[red]{from} - {message}[/]")
        | LogItem.Warning(from, message, ``exception``) -> AnsiConsole.MarkupLineInterpolated($"[orange]{from} - {message}[/]")
        | LogItem.Success(from, message) -> AnsiConsole.MarkupLineInterpolated($"[green]{from} - {message}[/]")
        | LogItem.Trace(from, message, reference) -> AnsiConsole.MarkupLineInterpolated($"[blue]{from} - {message}[/]")
        | LogItem.Debug(from, message) ->
            if Debugger.IsAttached then
                AnsiConsole.MarkupLineInterpolated($"[purple]{from} - {message}[/]")

    ()

(*
    open System
    open System.IO
    open System.Text.Json
    open System.Text.Json.Serialization
    open StaticCMS.DataStore
    open Logging

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

    type AppContext =
        { Store: StaticStore
          Log: Logger
          GeneralSettings: GeneralSettings }

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

    let createContext (storePath: string option) (generalSettingsPath: string option) =

        getGeneralSettings generalSettingsPath
        |> Result.map (fun gs ->

            { Store = StaticStore.Create <| getStorePath storePath
              Log = Logger()
              GeneralSettings = gs })
    *)
