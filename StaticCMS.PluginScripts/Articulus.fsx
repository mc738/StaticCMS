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
open StaticCMS
open StaticCMS.DataStore
open Articulus
open Articulus.Plugin
open Articulus.Store
open Articulus.Import
open Articulus.Rendering

let buildPages (staticStore: StaticStoreReader) (cfg: ArticulusConfiguration) =
    let store = ArticulusStore.Create cfg.StorePath

    let template =
        match cfg.TemplateLocation with
        | TemplateLocationType.Store v ->
            staticStore.GetTemplate v
            |> Option.map Encoding.UTF8.GetString
            |> Option.defaultValue ""
        | TemplateLocationType.File v -> File.ReadAllText v
        |> Mustache.parse

    let navBar =
        cfg.ArticleNavBar
        |> Option.bind (fun nbc ->
            staticStore.GetFragmentTemplate nbc.TemplateName
            |> Option.map (fun ft -> nbc.DataPath, ft |> Encoding.UTF8.GetString))
        |> Option.map (fun (dp, ft) ->
            // TODO need to handle errors better!
            File.ReadAllText dp |> PageFragments.renderJsonFragment ft)

    let pageData =
        cfg.ArticlePageDataPaths
        |> List.choose (fun apd ->
            try
                match File.Exists apd with
                | true ->
                    // NOTE this could be a bit clear but would require a change to Fluff.
                    Mustache.Data.FromJson(File.ReadAllText apd, PageFragments.renderInline).Values
                    |> Map.toList
                    |> Some
                | false -> None
            with exn ->
                None)
        |> List.concat

    // TODO make configurable.
    let values =
        [ "styles",
          cfg.ArticleStyles
          |> List.map (fun s -> [ "url", Mustache.Scalar s ] |> Map.ofList |> Mustache.Object)
          |> Mustache.Array
          "scripts",
          cfg.ArticleScripts
          |> List.map (fun s -> [ "url", Mustache.Scalar s ] |> Map.ofList |> Mustache.Object)
          |> Mustache.Array
          match navBar with
          | Some navBar -> "nav_html", Mustache.Scalar navBar
          | None -> ()
          match cfg.IconScript with
          | Some icons -> "icon_script", [ "url", Mustache.Value.Scalar icons ] |> Map.ofList |> Mustache.Value.Object
          | None -> ()
          yield! pageData ]

    importFiles store printResult cfg.DataPath

    let result = renderAll store values template cfg.OutputPath cfg.OutputDirectory

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

let buildIndex (staticStore: StaticStoreReader) (cfg: ArticulusConfiguration) (fragmentData: FragmentDataItem list) =
    // Put article template in store?
    let newPathTemplate =
        match cfg.IndexTemplateLocation with
        | TemplateLocationType.Store v ->
            staticStore.GetTemplate v
            |> Option.map Encoding.UTF8.GetString
            |> Option.defaultValue ""
        | TemplateLocationType.File v -> File.ReadAllText v
        |> Mustache.parse

    let navBar =
        cfg.IndexNavBar
        |> Option.bind (fun nbc ->
            staticStore.GetFragmentTemplate nbc.TemplateName
            |> Option.map (fun ft -> nbc.DataPath, ft |> Encoding.UTF8.GetString))
        |> Option.map (fun (dp, ft) ->
            // TODO need to handle errors better!
            File.ReadAllText dp |> PageFragments.renderJsonFragment ft)

    let pageData =
        cfg.IndexPageDataPaths
        |> List.choose (fun ipd ->
            try
                match File.Exists ipd with
                | true ->
                    // NOTE this could be a bit clear but would require a change to Fluff.
                    Mustache.Data.FromJson(File.ReadAllText ipd, PageFragments.renderInline).Values
                    |> Map.toList
                    |> Some
                | false -> None
            with exn ->
                None)
        |> List.concat

    ({ Values =
        [ "items",
          fragmentData
          |> List.map (fun fd ->
              [ "title_html", Mustache.Scalar fd.TitleHtml
                "summary_html", Mustache.Scalar fd.SummaryHtml
                "link", Mustache.Scalar fd.Link
                "date", Mustache.Scalar <| fd.Date.ToString("dd MMMM yyyy") ]
              |> Map.ofList
              |> Mustache.Object)
          |> Mustache.Array
          "styles",
          cfg.IndexStyles
          |> List.map (fun s -> [ "url", Mustache.Scalar s ] |> Map.ofList |> Mustache.Object)
          |> Mustache.Array
          "scripts",
          cfg.IndexScripts
          |> List.map (fun s -> [ "url", Mustache.Scalar s ] |> Map.ofList |> Mustache.Object)
          |> Mustache.Array
          match navBar with
          | Some navBar -> "nav_html", Mustache.Scalar navBar
          | None -> ()
          match cfg.IconScript with
          | Some icons -> "icon_script", [ "url", Mustache.Value.Scalar icons ] |> Map.ofList |> Mustache.Value.Object
          | None -> ()
          yield! pageData ]
        |> Map.ofList
       Partials = Map.empty }
    : Mustache.Data)
    |> fun d -> Mustache.replace d true newPathTemplate
    |> fun r -> File.WriteAllText(Path.Combine(cfg.OutputPath, $"{cfg.IndexPageName}.html"), r)

    fragmentData

let createFragment (fragmentData: FragmentDataItem list) =
    ({ Title = "News"
       Items = fragmentData |> List.truncate 3 }
    : FragmentData)
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
                rawCfg
                |> JsonDocument.Parse
                |> fun jd -> jd.RootElement
                |> ArticulusConfiguration.TryDeserialize
            with exn ->
                Error $"Failed to parse articulus configuration. Error: {exn.Message}"
        | None -> Error $"Articulus plugin not found for site `{site}`"
        |> Result.bind (fun cfg ->
            try
                buildPages store cfg |> buildIndex store cfg |> createFragment |> Ok
            with exn ->
                Error $"Plugin failed. Error: {exn.Message}")

    match result with
    | Ok r -> r
    | Error e -> failwith $"{e}"
