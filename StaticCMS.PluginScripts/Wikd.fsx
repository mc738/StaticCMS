#r """C:\Users\44748\Projects\StaticCMS\StaticCMS\bin\Debug\net6.0\StaticCMS.dll"""
#r """C:\ProjectData\static_cms\plugins\nugets\FDOM.Core.dll"""
#r """C:\ProjectData\static_cms\plugins\nugets\FDOM.Rendering.Html.dll"""
#r """C:\ProjectData\static_cms\plugins\nugets\Fluff.Core.dll"""
#r """C:\ProjectData\static_cms\plugins\nugets\Freql.Sqlite.dll"""
#r """C:\Users\44748\Projects\Wikd\Wikd\bin\Debug\net6.0\Wikd.dll"""

#r "nuget: Freql.Sqlite"
#r "nuget: Fluff.Core"
#r "nuget: FDOM.Core"
#r "nuget: FDOM.Rendering.Html"

open System.IO
open System.Text
open System.Text.Json
open Fluff.Core
open StaticCMS
open Wikd
open Wikd.DataStore
open StaticCMS.DataStore
open Wikd.Plugin

let run (staticStore: StaticStoreReader) (cfg: WikdConfiguration) =
    try
        let store = WikdStore.Create cfg.StorePath

        Tools.import store Tools.printResult "index" cfg.ContentRootPath

        let template = File.ReadAllText cfg.TemplatePath |> Mustache.parse

        let navBar =
            cfg.NavBar
            |> Option.bind (fun nbc ->
                staticStore.GetFragmentTemplate nbc.TemplateName
                |> Option.map (fun ft -> nbc.DataPath, ft |> Encoding.UTF8.GetString))
            |> Option.map (fun (dp, ft) ->
                // TODO need to handle errors better!
                File.ReadAllText dp |> PageFragments.renderJsonFragment ft)

        ({ RootPath = cfg.OutputPath
           NavBarHtml = navBar
           Template = template
           RendererSettings = cfg.GetRenderSettings() }
        : Wikd.Renderer.WikdParameters)
        |> Wikd.Renderer.run store
        |> Ok
    with exn ->
        Error $"Plugin failed. Error: {exn.Message}"

let runFromStore storePath site =
    let staticStore = StaticStoreReader.Open storePath

    let result =
        match staticStore.GetSitePluginConfiguration(site, "wikd") with
        | Some rawCfg ->
            try
                rawCfg
                |> JsonDocument.Parse
                |> fun jd -> jd.RootElement
                |> WikdConfiguration.TryDeserialize
            with exn ->
                Error $"Failed to parse wikd configuration. Error: {exn.Message}"
        | None -> Error $"Wikd plugin not found for site `{site}`"
        |> Result.bind (run staticStore)

    match result with
    | Ok r -> r
    | Error e -> failwith $"{e}"

let runFromPath storePath site =
    let staticStore = StaticStoreReader.Open storePath

    let result =
        match staticStore.GetSiteRoot site with
        | Some root ->
            try
                let cfgPath = File.ReadAllText(Path.Combine(root, "plugins", "wikd", "config.json"))

                match File.Exists cfgPath with
                | true ->
                    File.ReadAllText cfgPath
                    |> JsonDocument.Parse
                    |> fun jd -> jd.RootElement
                    |> WikdConfiguration.TryDeserialize
                | false -> Error $"Wikd configuration `{cfgPath}` not found"
            with exn ->
                Error $"Failed to parse wikd configuration. Error: {exn.Message}"
        | None -> Error $"Site `{site}` not found"
        |> Result.bind (run staticStore)

    match result with
    | Ok r -> r
    | Error e -> failwith $"{e}"

    ()
