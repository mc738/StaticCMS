namespace StaticCMS.App.Actions

open System.IO
open System.Text.Json.Serialization
open Articulus.Rendering
open StaticCMS
open StaticCMS.App.Actions.Data
open StaticCMS.App.Common
open StaticCMS.DataStore
open StaticCMS.Pipeline

module InitializeSite =

    open System.IO
    open StaticCMS.App.Common.Options
    open StaticCMS.DataStore
    open StaticCMS.Pipeline
    open StaticCMS.App.Common
    open StaticCMS.App.Actions.Data

    let defaultIndexTemplate = "default#index"

    let defaultNavTemplate = "default#nav_bar"

    let defaultFeatureCardsTemplate = "default#feature_cards"

    let defaultArticleTemplate = "default#article"

    let createDirectoryStructure (ctx: AppContext) (root: string) =
        Directory.CreateDirectory(root) |> ignore
        Directory.CreateDirectory(Path.Combine(root, "artifacts")) |> ignore
        Directory.CreateDirectory(Path.Combine(root, "data")) |> ignore
        Directory.CreateDirectory(Path.Combine(root, "data", "fragments")) |> ignore
        Directory.CreateDirectory(Path.Combine(root, "fragment_templates")) |> ignore
        Directory.CreateDirectory(Path.Combine(root, "page_templates")) |> ignore
        Directory.CreateDirectory(Path.Combine(root, "pages")) |> ignore
        Directory.CreateDirectory(Path.Combine(root, "plugins")) |> ignore
        Directory.CreateDirectory(Path.Combine(root, "rendered")) |> ignore
        Directory.CreateDirectory(Path.Combine(root, "resources")) |> ignore
        Directory.CreateDirectory(Path.Combine(root, "resources", "css")) |> ignore
        Directory.CreateDirectory(Path.Combine(root, "resources", "js")) |> ignore
        Directory.CreateDirectory(Path.Combine(root, "resources", "img")) |> ignore
        Directory.CreateDirectory(Path.Combine(root, "pages", "index")) |> ignore

        if ctx.GeneralSettings.IncludeExamplePage then
            Directory.CreateDirectory(Path.Combine(root, "pages", "example")) |> ignore

    let createSite (store: StaticStore) (name: string) (url: string) (root: string) = store.AddSite(name, url, root)

    let addIndexPage (ctx: AppContext) (name: string) (displayName: string) (root: string) =

        let apr = ctx.Store.AddPage(name, "index", "index")

        (*
        match apr with
        | AddPageResult.Success reference
        | AddPageResult.AlreadyExists reference ->
            ctx.Store.AddPageVersion(reference, createRef (), defaultIndexTemplate, false)
        | AddPageResult.Failure storeOperationFailure -> failwith "todo"
        *)

        { SiteName = displayName
          Title = displayName
          Styles =
            ctx.GeneralSettings.DefaultStyles
            |> List.ofSeq
            |> List.map (fun ds -> { Url = ds })
          Scripts =
            ctx.GeneralSettings.DefaultScripts
            |> List.ofSeq
            |> List.map (fun ds -> { Url = ds })
          IconScript = { Url = ctx.GeneralSettings.DefaultIconScriptUrl }
          AdditionData =
            [ "summary_html",
              "You can change this message by editing the <span class='code'>summary_html</span> value in <span class='code'>$root/pages/index/data.json</span>" ]
            |> Map.ofList }
            .Serialize()
        |> fun data -> File.WriteAllText(Path.Combine(root, "pages", "index", "data.json"), data)

        match File.Exists ctx.GeneralSettings.DefaultIndexBodyPath with
        | true ->
            File.Copy(
                ctx.GeneralSettings.DefaultIndexBodyPath,
                Path.Combine(root, "data", "fragments", "index_body.md")
            )
        | false -> ()

        ({ Name = "index"
           Template = defaultIndexTemplate
           Steps =
             [ { Path = "$root/data/fragments/nav_bar.json"
                 Fragment =
                   { Template = defaultNavTemplate
                     DataName = "navbar_html"
                     ContentType = FragmentBlobType.Json } }
               |> BuildPageStep.AddPageFragment

               { Path = "$root/data/fragments/feature_cards.json"
                 Fragment =
                   { Template = defaultFeatureCardsTemplate
                     DataName = "feature_cards"
                     ContentType = FragmentBlobType.Json } }
               |> BuildPageStep.AddPageFragment

               { Path = "$root/data/fragments/index_body.md"
                 Fragment =
                   { Template = "__blank"
                     DataName = "index_body"
                     ContentType = FragmentBlobType.Markdown } }
               |> BuildPageStep.AddPageFragment

               { OutputName = "fragments_html"
                 // TODO add to app initialization
                 Template = "default_index_body"
                 Fragments = [ "feature_cards"; "index_body" ] }
               |> BuildPageStep.CombinePageFragments
               BuildPageStep.AddPageData { Path = "$root/pages/index/data.json" } ] }
        : BuildPageAction)

    let addExamplePage (ctx: AppContext) (name: string) (displayName: string) (root: string) =

        let apr = ctx.Store.AddPage(name, "example", "example")

        (*
        match apr with
        | AddPageResult.Success reference
        | AddPageResult.AlreadyExists reference ->
            ctx.Store.AddPageVersion(reference, createRef (), defaultIndexTemplate, false)
        | AddPageResult.Failure storeOperationFailure -> failwith "todo"
        *)

        match File.Exists ctx.GeneralSettings.ExamplePageBodyPath with
        | true ->
            File.Copy(
                ctx.GeneralSettings.ExamplePageBodyPath,
                Path.Combine(root, "data", "fragments", "example_body.md")
            )
        | false -> ()

        { SiteName = displayName
          Title = "Example"
          Styles =
            ctx.GeneralSettings.DefaultStyles
            |> List.ofSeq
            |> List.map (fun ds -> { Url = ds })
          Scripts =
            ctx.GeneralSettings.DefaultScripts
            |> List.ofSeq
            |> List.map (fun ds -> { Url = ds })
          IconScript = { Url = ctx.GeneralSettings.DefaultIconScriptUrl }
          AdditionData = Map.empty }
            .Serialize()
        |> fun data -> File.WriteAllText(Path.Combine(root, "pages", "example", "data.json"), data)

        ({ Name = "example"
           Template = defaultArticleTemplate
           Steps =
             [ { Path = "$root/data/fragments/nav_bar.json"
                 Fragment =
                   { Template = defaultNavTemplate
                     DataName = "navbar_html"
                     ContentType = FragmentBlobType.Json } }
               |> BuildPageStep.AddPageFragment

               { Path = "$root/data/fragments/example_body.md"
                 Fragment =
                   { Template = "__blank"
                     DataName = "content"
                     ContentType = FragmentBlobType.Markdown } }
               |> BuildPageStep.AddPageFragment
               BuildPageStep.AddPageData { Path = "$root/pages/example/data.json" } ] }
        : BuildPageAction)

    let addPageFragments (ctx: AppContext) (root: string) =
        ({ Items =
            [ { Url = "./index.html"; Title = "Home" }
              if ctx.GeneralSettings.IncludeExamplePage then
                  { Url = "./example.html"
                    Title = "Example" } ] }
        : NavBarData)
        |> serializeJson
        |> fun data -> File.WriteAllText(Path.Combine(root, "data", "fragments", "nav_bar.json"), data)

        ({ Items =
            [ { Url = "../index.html"
                Title = "Home" }
              if ctx.GeneralSettings.IncludeExamplePage then
                  { Url = "../example.html"
                    Title = "Example" } ] }
        : NavBarData)
        |> serializeJson
        |> fun data -> File.WriteAllText(Path.Combine(root, "data", "fragments", "nav_bar_embedded.json"), data)

        ({ Id = "features"
           Features =
             [ { Id = "create"
                 Icon = "fas fa-plus"
                 Title = "Create"
                 DetailHtml =
                   "Create new static websites quick and easily with StaticCMS. For more information see <a href='#'>here</a>." }
               { Id = "edit"
                 Icon = "fas fa-edit"
                 Title = "Edit"
                 DetailHtml =
                   "Create new static websites quick and easily with StaticCMS. For more information see <a href='#'>here</a>." }
               { Id = "extend"
                 Icon = "fas fa-plug"
                 Title = "Extend"
                 DetailHtml =
                   "Extend your site by adding custom themes and plugins. For more information see <a href='#'>here</a>." } ] }
        : FeatureCardsData)
        |> serializeJson
        |> fun data -> File.WriteAllText(Path.Combine(root, "data", "fragments", "feature_cards.json"), data)

    let copyResources (ctx: AppContext) (root: string) =

        Directory.EnumerateFiles(Path.Combine(ctx.GeneralSettings.DefaultTheme.Path, "resources", "css"))
        |> List.ofSeq
        |> List.iter (fun f -> File.Copy(f, Path.Combine(root, "resources", "css", Path.GetFileName(f))))

        Directory.EnumerateFiles(Path.Combine(ctx.GeneralSettings.DefaultTheme.Path, "resources", "js"))
        |> List.ofSeq
        |> List.iter (fun f -> File.Copy(f, Path.Combine(root, "resources", "js", Path.GetFileName(f))))

        ctx.GeneralSettings.DefaultResources
        |> List.ofSeq
        |> List.iter (fun dr ->
            File.Copy(dr.Path, Path.Combine(root, "resources", dr.CopyTo, Path.GetFileName(dr.Path))))

    let createDefaultBuildConfiguration (name: string) (root: string) (buildPageActions: BuildPageAction list) =

        let build =
            ({ Site = name
               Steps =
                 [ StepType.ClearRendered
                   { Directories = [ "$root/rendered/css"; "$root/rendered/js"; "$root/rendered/img" ] }
                   |> StepType.CreateDirectories
                   { Directories =
                       [ { From = "$root/resources/css"
                           To = "$root/rendered/css" }
                         { From = "$root/resources/js"
                           To = "$root/rendered/js" }
                         { From = "$root/resources/img"
                           To = "$root/rendered/img" } ]
                     Files = [] }
                   |> StepType.CopyResources
                   yield! buildPageActions |> List.map StepType.BuildPage
                   { OutputPath = "$root/artifacts"
                     NameFormat = None }
                   |> StepType.BundleSite ] }
            : PipelineConfiguration)
                .Serialize()

        File.WriteAllText(Path.Combine(root, "build.json"), build)

    let run (ctx: AppContext) (name: string) (url: string) (displayName: string option) (root: string option) =
        try
            match ctx.Store.GetSite name with
            | Some _ -> Error $"Site `{name}` already exists"
            | None ->

                let root =
                    root
                    |> Option.defaultWith (fun _ -> Path.Combine(getStaticRoot (), "sites", name))

                let displayName = displayName |> Option.defaultValue name

                createDirectoryStructure ctx root
                createSite ctx.Store name url root

                let buildActions =
                    [ addIndexPage ctx name displayName root
                      if ctx.GeneralSettings.IncludeExamplePage then
                          addExamplePage ctx name displayName root ]

                addPageFragments ctx root
                copyResources ctx root
                createDefaultBuildConfiguration name root buildActions

                Ok()
        with exn ->
            Error $"Failed to initialize site. Error: {exn.Message}"

    let action (options: InitializeSiteOptions) =
        try
            let store = getStorePath options.StorePath |> StaticStore.Create


            failwith "TODO - fix"

        //run store options.Name options.Url options.DisplayName options.Root
        with exn ->
            Error $"Failed to initialize site. Error: {exn.Message}"
