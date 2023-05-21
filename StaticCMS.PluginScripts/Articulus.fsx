#r """C:\Users\44748\Projects\StaticCMS\StaticCMS\bin\Debug\net6.0\StaticCMS.dll"""
#r """C:\ProjectData\static_cms\plugins\nugets\FDOM.Core.dll"""
#r """C:\ProjectData\static_cms\plugins\nugets\FDOM.Rendering.Html.dll"""
#r """C:\ProjectData\static_cms\plugins\nugets\Fluff.Core.dll"""
#r """C:\ProjectData\static_cms\plugins\nugets\Freql.Sqlite.dll"""
#r """C:\Users\44748\Projects\Articulus\Articulus\bin\Debug\net6.0\Articulus.dll"""

#r "nuget: Freql.Sqlite"
#r "nuget: Fluff.Core"
#r "nuget: FDOM.Core"
#r "nuget: FDOM.Rendering.Html"

open System.IO
open System.Text
open System.Text.Json
open System.Text.Json.Serialization
open FDOM.Rendering
open Fluff.Core
open StaticCMS.DataStore
open Articulus
open Articulus.Plugin
open Articulus.Store
open Articulus.Import
open Articulus.Rendering

let buildPages (cfg: ArticulusConfiguration) =
    let store =
        ArticulusStore.Create cfg.StorePath

    let template =
        File.ReadAllText cfg.TemplatePath
        |> Mustache.parse

    // TODO make configurable.
    let values =
        [ "style_url", Mustache.Scalar "../css/style.css"
          "fa_url", Mustache.Scalar "https://kit.fontawesome.com/f5ae0cbcfc.js"
          "script_url", Mustache.Scalar "../js/index.js" ]


    importFiles store printResult cfg.DataPath

    let result =
        renderAll store values template cfg.OutputPath cfg.OutputDirectory

    result
    |> List.fold
        (fun acc r ->
            match r with
            | Ok fd -> acc @ [ fd ]
            | Error e ->
                printfn $"Error - {e}"
                acc)
        []
    |> List.sortByDescending (fun fd -> fd.Date)

let buildIndex (cfg: ArticulusConfiguration) (fragmentData: FragmentDataItem list) =
    let newPathTemplate =
        File.ReadAllText cfg.IndexPageTemplatePath
        |> Mustache.parse

    ({ Values =
        [ "items",
          fragmentData
          |> List.map (fun fd ->
              [ "title_html", Mustache.Scalar fd.TitleHtml
                "summary_html", Mustache.Scalar fd.SummaryHtml
                "link", Mustache.Scalar fd.Link
                "date",
                Mustache.Scalar
                <| fd.Date.ToString("dd MMMM yyyy") ]
              |> Map.ofList
              |> Mustache.Object)
          |> Mustache.Array ]
        |> Map.ofList
       Partials = Map.empty }: Mustache.Data)
    |> fun d -> Mustache.replace d true newPathTemplate
    |> fun r -> File.WriteAllText(Path.Combine(cfg.OutputPath, $"{cfg.IndexPageName}.html"), r)
    fragmentData

let createFragment (fragmentData: FragmentDataItem list) =
    ({ Title = "News"
       Items = fragmentData |> List.truncate 3 }: FragmentData)
    |> fun fd -> fd.Serialize("dd MMMM yyyy")
    //|> JsonSerializer.Serialize
        //|> fun fdj -> File.WriteAllText("C:\\ProjectData\\static_cms\\sites\\Freql\\fragments\\news.json", fdj)
     
let run storePath site =
    let store = StaticStoreReader.Open storePath

    // Load the configuration
    let result =
        match store.GetSitePluginConfiguration(site, "articulus") with
        | Some rawCfg ->
            try
                JsonSerializer.Deserialize<ArticulusConfiguration> rawCfg
                |> Ok
            with
            | exn -> Error $"Failed to parse articulus configuration. Error: {exn.Message}"
        | None -> Error $"Articulus plugin not found for site `{site}`"
        |> Result.bind (fun cfg ->
            try
                buildPages cfg
                |> buildIndex cfg
                |> createFragment
                |> Ok
            with
            | exn -> Error $"Plugin failed. Error: {exn.Message}")

    match result with
    | Ok r -> r
    | Error e -> failwith $"{e}"
