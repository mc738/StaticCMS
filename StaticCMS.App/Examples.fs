namespace StaticCMS.App.Examples

open System
open System.IO
open System.Text
open System.Text.Json
open Fluff.Core
open StaticCMS
open StaticCMS.DataStore
open StaticCMS.Pipeline

module MetadataTest =

    let run _ =
        let lines1 =
            File.ReadAllText
                "C:\\Users\\44748\\Projects\\Wikd\\Documents\\overview\\stand_alone\\stand_alone_examples.md"
            |> fun r -> r.Split(Environment.NewLine) |> List.ofArray

        let lines2 =
            File.ReadAllText "C:\\Users\\44748\\Projects\\Wikd\\Documents\\tools.md"
            |> fun r -> r.Split(Environment.NewLine) |> List.ofArray

        let (r1, md1) = FDOM.Core.Parsing.Parser.ParseLinesAndMetadata(lines1)

        let (r2, md2) = FDOM.Core.Parsing.Parser.ParseLinesAndMetadata(lines2)

        let bc1 = r1.CreateBlockContent()
        let bc2 = r2.CreateBlockContent()

        ()

module ArticulusTest =

    open Articulus
    open Articulus.Store
    open Articulus.Import
    open Articulus.Rendering


    let run _ =
        let store =
            ArticulusStore.Create("C:\\ProjectData\\static_cms\\sites\\Freql\\plugins\\articulus\\store.db")

        let template =
            File.ReadAllText
                "C:\\ProjectData\\static_cms\\sites\\Freql\\plugins\\articulus\\templates\\article_template.mustache"
            |> Mustache.parse
        // Load in data.

        let values =
            [ "style_url", Mustache.Scalar "../css/style.css"
              "fa_url", Mustache.Scalar "https://kit.fontawesome.com/f5ae0cbcfc.js"
              "script_url", Mustache.Scalar "../js/index.js" ]

        importFiles store printResult "C:\\ProjectData\\static_cms\\sites\\Freql\\data\\news"

        let result =
            renderAll store values template "C:\\ProjectData\\static_cms\\sites\\Freql\\rendered" "news"

        let fds =
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

        // Create [out directory]/index.html

        let newPathTemplate =
            File.ReadAllText "C:\\ProjectData\\static_cms\\sites\\Freql\\page_templates\\news_page_template.mustache"
            |> Mustache.parse

        ({ Values =
            [ "items",
              fds
              |> List.map (fun fd ->
                  [ "title", Mustache.Scalar fd.TitleHtml
                    "summary", Mustache.Scalar fd.SummaryHtml
                    "link", Mustache.Scalar fd.Link
                    "date", Mustache.Scalar <| fd.Date.ToString("dd MMMM yyyy") ]
                  |> Map.ofList
                  |> Mustache.Object)
              |> Mustache.Array ]
            |> Map.ofList
           Partials = Map.empty }
        : Mustache.Data)
        |> fun d -> Mustache.replace d true newPathTemplate
        |> fun r -> File.WriteAllText("C:\\ProjectData\\static_cms\\sites\\Freql\\rendered\\news.html", r)


        // Create fragment.

        let fragmentData =
            ({ Title = "News"
               Items = fds |> List.truncate 3 }
            : FragmentData)
            |> JsonSerializer.Serialize
            |> fun fdj -> File.WriteAllText("C:\\ProjectData\\static_cms\\sites\\Freql\\fragments\\news.json", fdj)



        // Add the page to static store?

        ()

module PeepsTest =

    open Articulus
    open Articulus.Store
    open Articulus.Import
    open Articulus.Rendering

    let root = "C:\\ProjectData\\static_cms\\sites\\Peeps"

    let renderedPath = Path.Combine(root, "rendered")

    let resourcesPath = Path.Combine(root, "resources")

    let pluginsPath = Path.Combine(root, "plugins")

    let initialize (store: StaticStore) =

        let template =
            File.ReadAllBytes "C:\\ProjectData\\static_cms\\templates\\peeps_index.mustache"

        store.AddTemplate("peeps_index", template)

        store.AddSite("peeps", "https://peeps.psionic.cloud", "/home/max/sites/peeps")

        store.AddFragmentTemplate("features", [||])

        store.AddFragmentTemplate(
            "news",
            File.ReadAllBytes "C:\\ProjectData\\static_cms\\example_fragments\\peeps_news_template.mustache"
        )

        store.AddFragmentTemplate(
            "docs",
            File.ReadAllBytes "C:\\ProjectData\\static_cms\\example_fragments\\peeps_docs_template.mustache"
        )

        store.AddPluginType("script")
        store.AddPlugin("articulus", "script")

        store.AddSitePlugin(
            "peeps",
            "articulus",
            File.ReadAllText("C:\\ProjectData\\static_cms\\sites\\Peeps\\plugins\\articulus\\config.json")
        )

    let copyResources _ =
        let cssPath = Path.Combine(resourcesPath, "css")

        let jsPath = Path.Combine(resourcesPath, "js")

        let imgPath = Path.Combine(resourcesPath, "img")

        let cssOutPath = Path.Combine(renderedPath, "css")

        let jsOutPath = Path.Combine(renderedPath, "js")

        let imgOutPath = Path.Combine(renderedPath, "img")

        if Directory.Exists cssOutPath |> not then
            Directory.CreateDirectory(cssOutPath) |> ignore

        if Directory.Exists jsOutPath |> not then
            Directory.CreateDirectory(jsOutPath) |> ignore

        if Directory.Exists imgOutPath |> not then
            Directory.CreateDirectory(imgOutPath) |> ignore

        [ "fxd.css"; "prism.css"; "style.css" ]
        |> List.iter (fun fn -> File.Copy(Path.Combine(cssPath, fn), Path.Combine(cssOutPath, fn)))

        [ "hero.jpg" ]
        |> List.iter (fun fn -> File.Copy(Path.Combine(imgPath, fn), Path.Combine(imgOutPath, fn)))

        [ "fxd.js"; "index.js"; "prism.js" ]
        |> List.iter (fun fn -> File.Copy(Path.Combine(jsPath, fn), Path.Combine(jsOutPath, fn)))

    let runArticulus (staticStore: StaticStore) (versionRef: string) =
        let articulusPath = Path.Combine(pluginsPath, "articulus")

        let store = ArticulusStore.Create(Path.Combine(articulusPath, "store.db"))

        let template =
            File.ReadAllText(Path.Combine(articulusPath, "templates", "article_template.mustache"))
            |> Mustache.parse
        // Load in data.

        let values =
            [ "style_url", Mustache.Scalar "../css/style.css"
              "fa_url", Mustache.Scalar "https://kit.fontawesome.com/f5ae0cbcfc.js"
              "script_url", Mustache.Scalar "../js/index.js" ]

        importFiles store printResult (Path.Combine(root, "data", "news"))

        let result = renderAll store values template renderedPath "news"

        let fds =
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

        // Create [out directory]/index.html

        let newPathTemplate =
            File.ReadAllText(Path.Combine(root, "page_templates", "news_page_template.mustache"))
            |> Mustache.parse

        ({ Values =
            [ "items",
              fds
              |> List.map (fun fd ->
                  [ "title", Mustache.Scalar fd.TitleHtml
                    "summary", Mustache.Scalar fd.SummaryHtml
                    "link", Mustache.Scalar fd.Link
                    "date", Mustache.Scalar <| fd.Date.ToString("dd MMMM yyyy") ]
                  |> Map.ofList
                  |> Mustache.Object)
              |> Mustache.Array ]
            |> Map.ofList
           Partials = Map.empty }
        : Mustache.Data)
        |> fun d -> Mustache.replace d true newPathTemplate
        |> fun r -> File.WriteAllText(Path.Combine(renderedPath, "news.html"), r)

        // Create fragment.

        let fragmentData =
            ({ Title = "News"
               Items = fds |> List.truncate 3 }
            : FragmentData)
            |> fun fd -> fd.Serialize("dd MMMM yyyy")
            |> Encoding.UTF8.GetBytes
        //|> fun fdj -> File.WriteAllText("C:\\ProjectData\\static_cms\\sites\\Freql\\fragments\\news.json", fdj)

        staticStore.AddPageFragment(versionRef, "news", "news", fragmentData, "json")

        // Add the page to static store?

        ()

    let runPlugins (staticStore: StaticStore) (versionRef: string) =
        printfn "Starting fsi."
        let fsi = Faaz.ScriptHost.fsiSession ()

        match
            Faaz.ScriptHost.eval<string>
                "C:\\Users\\44748\\Projects\\StaticCMS\\StaticCMS.PluginScripts\\Articulus.fsx"
                $"""Articulus.run "C:\\ProjectData\\static_cms\\static_store-test.db" "peeps" """
                fsi
        with
        | Ok r ->
            staticStore.AddPageFragment(versionRef, "news", "news", r |> Encoding.UTF8.GetBytes, "json")
            ()
        | Error e -> printfn $"Error: {e}"

    let buildSite (store: StaticStore) (pageRef: string) (versionRef: string) =

        store.AddPage(pageRef, "peeps", "index", "index_generated")
        store.AddPageVersion(pageRef, versionRef, "peeps_index", true)

        store.AddPageFragment(
            versionRef,
            "features",
            "features",
            File.ReadAllBytes "C:\\ProjectData\\static_cms\\example_fragments\\peeps_features.md",
            "markdown"
        )

        (*
        store.AddPageFragment(
            versionRef,
            "news",
            "news",
            File.ReadAllBytes "C:\\ProjectData\\static_cms\\example_fragments\\peeps_news.json",
            "json"
        )
        *)

        store.AddPageFragment(
            versionRef,
            "docs",
            "docs",
            File.ReadAllBytes "C:\\ProjectData\\static_cms\\example_fragments\\peeps_docs.json",
            "json"
        )

    let render (store: StaticStore) =
        match PageRenderer.run store "peeps" "index" with
        | Ok p -> File.WriteAllText(Path.Combine(renderedPath, "index.html"), p)
        | Error e -> printfn $"Error: {e}"

    let run _ =

        let createRef _ = Guid.NewGuid().ToString("n")

        let store = StaticStore.Create("C:\\ProjectData\\static_cms\\static_store-test.db")

        let pageRef = createRef ()
        let versionRef = createRef ()

        initialize store
        copyResources ()
        buildSite store pageRef versionRef
        runPlugins store versionRef
        //runArticulus store versionRef
        render store

module PipelineTest =

    let knownPaths = [ "$plugins", "C:\\ProjectData\\static_cms\\plugins" ]

    let run _ =
        match Pipeline.loadConfiguration "C:\\ProjectData\\static_cms\\sites\\StaticCMS\\build.json" with
        | Ok cfg ->
            let store = StaticStore.Create "C:\\ProjectData\\static_cms\\static_store.db"

            let scriptHost =
                ({ FsiSession = Faaz.ScriptHost.fsiSession () }: Faaz.ScriptHost.HostContext)

            match store.GetSite cfg.Site with
            | Some site ->
                let ctx =
                    PipelineContext.Create(store, scriptHost, knownPaths @ [ "$root", site.RootPath ] |> Map.ofList)

                let r = Pipeline.run ctx cfg

                ()
            | None -> failwith $"Unknown site `{cfg.Site}`."

            // Create the context


            ()
        | Error e -> ()

module ThemeTest =

    let run _ =
        let store = StaticStore.Create "C:\\ProjectData\\static_cms\\static_store.db"

        store.AddTemplate(
            "static_cms-index",
            File.ReadAllBytes
                "C:\\ProjectData\\static_cms\\sites\\StaticCMS\\page_templates\\index_page_template.mustache"
        )

        store.AddTemplate(
            "default#index",
            File.ReadAllBytes "C:\\ProjectData\\static_cms\\themes\\default\\templates\\pages\\index.mustache"
        )

        store.AddFragmentTemplate(
            "default#feature_cards",
            File.ReadAllBytes
                "C:\\ProjectData\\static_cms\\themes\\default\\templates\\fragments\\feature_cards.mustache"
        )

        store.AddFragmentTemplate(
            "default#nav_bar",
            File.ReadAllBytes "C:\\ProjectData\\static_cms\\themes\\default\\templates\\fragments\\nav_bar.mustache"
        )

        store.AddFragmentTemplate(
            "default#news",
            File.ReadAllBytes "C:\\ProjectData\\static_cms\\themes\\default\\templates\\fragments\\news.mustache"
        )

module FPype =

    let knownPaths = [ "$plugins", "C:\\ProjectData\\static_cms\\plugins" ]

    let run _ =
        match Pipeline.loadConfiguration "C:\\ProjectData\\static_cms\\sites\\FPype\\build.json" with
        | Ok cfg ->
            let store = StaticStore.Create "C:\\ProjectData\\static_cms\\static_store.db"

            let scriptHost =
                ({ FsiSession = Faaz.ScriptHost.fsiSession () }: Faaz.ScriptHost.HostContext)

            match store.GetSite cfg.Site with
            | Some site ->
                let ctx =
                    PipelineContext.Create(store, scriptHost, knownPaths @ [ "$root", site.RootPath ] |> Map.ofList)

                let r = Pipeline.run ctx cfg

                ()
            | None -> failwith $"Unknown site `{cfg.Site}`."

            // Create the context


            ()
        | Error e -> ()
        

module Peeps2 =
    let run _ =
        
        
        let store = StaticStore.Create("C:\\ProjectData\\static_cms\\static_store.db")

        let createRef _ = Guid.NewGuid().ToString("n")

        let template =
            File.ReadAllBytes "C:\\ProjectData\\static_cms\\templates\\peeps_index.mustache"

        store.AddTemplate("peeps_index", template)

        store.AddSite("peeps", "https://peeps.psionic.cloud", "/home/max/sites/peeps")
        let pageRef = createRef ()
        let versionRef = createRef ()

        store.AddPage(pageRef, "peeps", "index", "index_generated")
        store.AddPageVersion(pageRef, versionRef, "peeps_index", true)
        store.AddFragmentTemplate("features", [||])

        store.AddFragmentTemplate(
            "news",
            File.ReadAllBytes "C:\\ProjectData\\static_cms\\example_fragments\\peeps_news_template.mustache"
        )

        store.AddFragmentTemplate(
            "docs",
            File.ReadAllBytes "C:\\ProjectData\\static_cms\\example_fragments\\peeps_docs_template.mustache"
        )

        store.AddPageFragment(
            versionRef,
            "features",
            "features",
            File.ReadAllBytes "C:\\ProjectData\\static_cms\\example_fragments\\peeps_features.md",
            "markdown"
        )

        store.AddPageFragment(
            versionRef,
            "news",
            "news",
            File.ReadAllBytes "C:\\ProjectData\\static_cms\\example_fragments\\peeps_news.json",
            "json"
        )

        store.AddPageFragment(
            versionRef,
            "docs",
            "docs",
            File.ReadAllBytes "C:\\ProjectData\\static_cms\\example_fragments\\peeps_docs.json",
            "json"
        )

        match PageRenderer.run store "peeps" "index" with
        | Ok p -> File.WriteAllText("C:\\ProjectData\\Peeps\\website\\index_generated.html", p)
        | Error e -> printfn $"Error: {e}"


        (*
        let pageTemplate = File.ReadAllText "C:\\ProjectData\\static_cms\\templates\\peeps_index.mustache" |> Mustache.parse

        [ "features",
          Mustache.Value.Scalar
          <| PageFragments.renderMarkdownFragment
              PageFragments.rewriteTitles
              (File.ReadAllText "C:\\ProjectData\\static_cms\\example_fagrments\\peeps_features.md")
          "news",
          Mustache.Value.Scalar
          <| PageFragments.renderJsonFragment
              (File.ReadAllText "C:\\ProjectData\\static_cms\\example_fagrments\\peeps_news_template.mustache")
              (File.ReadAllText "C:\\ProjectData\\static_cms\\example_fagrments\\peeps_news.json")
          "docs",
          Mustache.Value.Scalar
          <| PageFragments.renderJsonFragment
              (File.ReadAllText "C:\\ProjectData\\static_cms\\example_fagrments\\peeps_docs_template.mustache")
              (File.ReadAllText "C:\\ProjectData\\static_cms\\example_fagrments\\peeps_docs.json") ]
        |> Map.ofList
        |> fun r -> ({ Values = r; Partials = Map.empty }: Mustache.Data)
        |> fun d -> Mustache.replace d true pageTemplate
        *)
        //|> fun p -> File.WriteAllText("C:\\ProjectData\\Peeps\\website\\index_generated.html", p)


(*
module WikdTest =

    let run _ =
        let store = WikdStore.Create("C:\\ProjectData\\static_cms\\plugins\\wikd\\wikd.db")

        let template = File.ReadAllText "C:\\ProjectData\\static_cms\\plugins\\wikd\\resources\\wikd_template.mustache" |> Mustache.parse

        Wikd.Renderer.run store "C:\\ProjectData\\static_cms\\plugins\\wikd\\example\\wiki" template

        ()
*)

(*

//let p = Pipeline.expandPath ([ "$root", "C:\\ProjectData\\static_cms\\sites\\StaticCMS" ] |> Map.ofList) "$root/data/fragments/features/features.md"

//ThemeTest.run ()

FPype.run ()
PipelineTest.run ()

PeepsTest.run ()

ArticulusTest.run ()

MetadataTest.run ()

//WikdTest.run ()
*)
