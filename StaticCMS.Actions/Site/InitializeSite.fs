namespace StaticCMS.Actions.Site

open System.IO
open StaticCMS
open StaticCMS.Actions.Common
open StaticCMS.Actions.Common.Data

[<RequireQualifiedAccess>]
module InitializeSite =

    type Parameters =
        { Name: string
          Url: string
          DisplayName: string option
          Root: string option }

    let defaultIndexTemplate = "default#index"

    let defaultNavTemplate = "default#navbar"

    let defaultFeatureCardsTemplate = "default#feature_cards"

    let defaultArticleTemplate = "default#article"

    let createDirectoryStructure (ctx: StaticCMSContext) (root: string) =
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

    let createSite (ctx: StaticCMSContext) (name: string) (url: string) (root: string) (displayName: string) =
        ctx.Store.AddSite(name, url, root, displayName)

    let addIndexPage (ctx: StaticCMSContext) (siteName: string) (displayName: string) (root: string) =

        let addPageResult =
            ({ SiteName = siteName
               SiteDisplayName = displayName
               SiteRoot = root
               PageName = "index"
               PageNameSlug = "index"
               PageTitle = "Home"
               PageTemplate = defaultIndexTemplate
               IncludeDefaultStyles = true
               IncludeDefaultScripts = true
               NavigableTo = false // This is left as false because navigation is handled separately when initializing a site.
               Styles = []
               Scripts = []
               AdditionalData =
                 [ "summary_html",
                   "You can change this message by editing the <span class='code'>summary_html</span> value in <span class='code'>$root/pages/index/data.json</span>" ]
                 |> Map.ofList
               PageFiles =
                 [ { Path = ctx.GeneralSettings.DefaultIndexBodyPath
                     OutputPath = Path.Combine(root, "pages", "index", "fragments", "body.md") } ] }
            : AddPage.Parameters)
            |> AddPage.handler ctx

        match addPageResult with
        | Ok _ ->
            ({ Name = "index"
               Template = defaultIndexTemplate
               Steps =
                 [ ({ Path = "$root/data/fragments/navbar.json"
                      Fragment =
                        { Template = defaultNavTemplate
                          DataName = "navbar_html"
                          ContentType = FragmentBlobType.Json } }
                   : Pipeline.AddPageFragmentPageBuildStep)
                   |> Pipeline.BuildPageStep.AddPageFragment

                   ({ Path = "$root/pages/index/fragments/feature_cards.json"
                      Fragment =
                        { Template = defaultFeatureCardsTemplate
                          DataName = "feature_cards"
                          ContentType = FragmentBlobType.Json } }
                   : Pipeline.AddPageFragmentPageBuildStep)
                   |> Pipeline.BuildPageStep.AddPageFragment

                   ({ Path = "$root/pages/index/fragments/body.md"
                      Fragment =
                        { Template = "__blank"
                          DataName = "body"
                          ContentType = FragmentBlobType.Markdown } }
                   : Pipeline.AddPageFragmentPageBuildStep)
                   |> Pipeline.BuildPageStep.AddPageFragment

                   ({ OutputName = "fragments_html"
                      // TODO add to app initialization
                      Template = "default_index_body"
                      Fragments = [ "feature_cards"; "body" ] }
                   : Pipeline.CombinePageFragmentsPageBuildStep)
                   |> Pipeline.BuildPageStep.CombinePageFragments
                   Pipeline.BuildPageStep.AddPageData { Path = "$root/pages/index/data.json" } ] }
            : Pipeline.BuildPageAction)
        | Error f -> failwith "TODO Handle"

    let addExamplePage (ctx: StaticCMSContext) (siteName: string) (displayName: string) (root: string) =
        let addPageResult =
            ({ SiteName = siteName
               SiteDisplayName = displayName
               SiteRoot = root
               PageName = "example"
               PageNameSlug = "example"
               PageTitle = "Example"
               PageTemplate = defaultArticleTemplate
               IncludeDefaultStyles = true
               IncludeDefaultScripts = true
               NavigableTo = false // This is left as false because navigation is handled separately when initializing a site.
               Styles = []
               Scripts = []
               AdditionalData = Map.empty
               PageFiles =
                 [ { Path = ctx.GeneralSettings.ExamplePageBodyPath
                     OutputPath = Path.Combine(root, "pages", "example", "fragments", "body.md") } ] }
            : AddPage.Parameters)
            |> AddPage.handler ctx

        match addPageResult with
        | Ok _ ->
            ({ Name = "example"
               Template = defaultArticleTemplate
               Steps =
                 [ ({ Path = "$root/data/fragments/navbar.json"
                      Fragment =
                        { Template = defaultNavTemplate
                          DataName = "navbar_html"
                          ContentType = FragmentBlobType.Json } }
                   : Pipeline.AddPageFragmentPageBuildStep)
                   |> Pipeline.BuildPageStep.AddPageFragment

                   ({ Path = "$root/pages/example/fragments/body.md"
                      Fragment =
                        { Template = "__blank"
                          DataName = "content"
                          ContentType = FragmentBlobType.Markdown } }
                   : Pipeline.AddPageFragmentPageBuildStep)
                   |> Pipeline.BuildPageStep.AddPageFragment
                   Pipeline.BuildPageStep.AddPageData { Path = "$root/pages/example/data.json" } ] }
            : Pipeline.BuildPageAction)
        | Error e -> failwith "TODO handle"

    let addPageFragments (ctx: StaticCMSContext) (root: string) =
        ({ Items =
            [ { Url = "./index.html"; Title = "Home" }
              if ctx.GeneralSettings.IncludeExamplePage then
                  { Url = "./example.html"
                    Title = "Example" } ] }
        : NavBarData)
        |> serializeJson
        |> fun data -> File.WriteAllText(Path.Combine(root, "data", "fragments", "navbar.json"), data)

        ({ Items =
            [ { Url = "../index.html"
                Title = "Home" }
              if ctx.GeneralSettings.IncludeExamplePage then
                  { Url = "../example.html"
                    Title = "Example" } ] }
        : NavBarData)
        |> serializeJson
        |> fun data -> File.WriteAllText(Path.Combine(root, "data", "fragments", "navbar_embedded.json"), data)

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
        |> fun data -> File.WriteAllText(Path.Combine(root, "pages", "index", "fragments", "feature_cards.json"), data)

    let copyResources (ctx: StaticCMSContext) (root: string) =

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

    let createDefaultBuildConfiguration
        (name: string)
        (root: string)
        (buildPageActions: Pipeline.BuildPageAction list)
        =

        let build =
            ({ Site = name
               Steps =
                 [ Pipeline.StepType.ClearRendered
                   ({ Directories = [ "$root/rendered/css"; "$root/rendered/js"; "$root/rendered/img" ] }
                   : Pipeline.CreateDirectoriesAction)
                   |> Pipeline.StepType.CreateDirectories
                   ({ Directories =
                       [ { From = "$root/resources/css"
                           To = "$root/rendered/css" }
                         { From = "$root/resources/js"
                           To = "$root/rendered/js" }
                         { From = "$root/resources/img"
                           To = "$root/rendered/img" } ]
                      Files = [] }
                   : Pipeline.CopyResourcesAction)
                   |> Pipeline.StepType.CopyResources
                   yield! buildPageActions |> List.map Pipeline.StepType.BuildPage
                   ({ OutputPath = "$root/artifacts"
                      NameFormat = None }
                   : Pipeline.BundleSiteAction)
                   |> Pipeline.StepType.BundleSite ] }
            : Pipeline.PipelineConfiguration)
                .Serialize()

        File.WriteAllText(Path.Combine(root, "build.json"), build)

    let run (ctx: StaticCMSContext) (parameters: Parameters) =
        try
            match ctx.Store.GetSite parameters.Name with
            | Some _ -> Error $"Site `{parameters.Name}` already exists"
            | None ->

                let root =
                    parameters.Root
                    |> Option.defaultWith (fun _ -> Path.Combine(getStaticRoot (), "sites", parameters.Name))

                let displayName = parameters.DisplayName |> Option.defaultValue parameters.Name

                createDirectoryStructure ctx root
                createSite ctx parameters.Name parameters.Url root displayName
                
                [ addIndexPage ctx parameters.Name displayName root
                  if ctx.GeneralSettings.IncludeExamplePage then
                      addExamplePage ctx parameters.Name displayName root ]
                |> createDefaultBuildConfiguration parameters.Name root
                
                addPageFragments ctx root
                copyResources ctx root
                |> Ok
        with exn ->
            Error $"Failed to initialize site. Error: {exn.Message}"
