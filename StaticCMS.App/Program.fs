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
open StaticCMS.App

let options =
    Environment.GetCommandLineArgs()
    |> List.ofArray
    |> ArgParser.tryGetOptions<Options>

let getGeneralSettings (path: string option) =
    path
    |> Option.defaultWith (fun _ -> Path.Combine(getStaticRoot (), "settings.json"))
    |> fun p ->
        try
            File.ReadAllText p |> JsonSerializer.Deserialize<GeneralSettings> |> Ok
        with exn ->
            Error "Failed to deserialize general settings"

let result =
    options
    |> Result.bind (fun o ->
        match o with
        | InitializeSite initializeSiteOptions -> initializeSite initializeSiteOptions
        | AddSite addSiteOptions -> failwith "todo"
        | ImportTemplate importTemplateOptions -> importTemplate importTemplateOptions
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
