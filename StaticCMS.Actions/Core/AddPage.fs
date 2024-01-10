namespace StaticCMS.Actions.Core

open System.IO
open StaticCMS
open StaticCMS.Actions.Common
open StaticCMS.Actions.Common.Data
open StaticCMS.Persistence

[<RequireQualifiedAccess>]
module AddPage =

    open System.IO
    open StaticCMS.DataStore
    open StaticCMS
    open StaticCMS.Actions.Common
    open StaticCMS.Actions.Common.Data

    type Parameters =
        { SiteName: string
          SiteDisplayName: string
          SiteRoot: string
          PageName: string
          PageNameSlug: string
          PageTitle: string
          PageTemplate: string
          IncludeDefaultStyles: bool
          IncludeDefaultScripts: bool
          NavigableTo: bool
          Styles: string list
          Scripts: string list
          AdditionalData: Map<string, string>
          PageFiles: PageFile list }

    and PageFile = { Path: string; OutputPath: string }

    let defaultNavTemplate = "default#navbar"

    let defaultArticleTemplate = "default#article"

    /// <summary>
    /// The failure point allows for a way to check how far the action got.
    /// This can be used to rollback changes.
    /// </summary>
    [<RequireQualifiedAccess>]
    type FailurePoint =
        | AddPageToStore of Failure: StoreOperationFailure
        | CreatePageDirectory of Message: string
        | GeneratePageData of Message: string
        | CopyResources of Message: string

    /// <summary>
    /// A handler for the basics of adding a new page.
    /// This won't create relevant build configuration commands.
    /// Use AddPage.run for that.
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="parameters"></param>
    let handler (ctx: StaticCMSContext) (parameters: Parameters) =

        let pageDirectory =
            Path.Combine(parameters.SiteRoot, "pages", parameters.PageNameSlug)

        // Add the page to the store...
        ctx.Store.AddPage(parameters.SiteName, parameters.PageName, parameters.PageNameSlug)
        |> function
            | AddPageResult.Success _
            | AddPageResult.AlreadyExists _ -> Ok()
            | AddPageResult.Failure f -> Error <| FailurePoint.AddPageToStore f
        // ...create the page directory...
        |> Result.bind (fun _ ->
            try
                Directory.CreateDirectory(pageDirectory) |> ignore
                Directory.CreateDirectory(Path.Combine(pageDirectory, "fragments")) |> Ok
            with ex ->
                Error <| FailurePoint.CreatePageDirectory $"Unhandled exception: {ex.Message}")
        // ...create the page data...
        |> Result.bind (fun _ ->
            try
                { SiteName = parameters.SiteDisplayName
                  Title = parameters.SiteDisplayName
                  Styles =
                    [ yield! parameters.Styles
                      if parameters.IncludeDefaultStyles then
                          yield! ctx.GeneralSettings.DefaultStyles ]
                    |> List.map (fun ds -> { Url = ds })
                  Scripts =
                    [ yield! parameters.Scripts
                      if parameters.IncludeDefaultScripts then
                          yield! ctx.GeneralSettings.DefaultScripts ]
                    |> List.map (fun ds -> { Url = ds })
                  IconScript = { Url = ctx.GeneralSettings.DefaultIconScriptUrl }
                  AdditionData = parameters.AdditionalData }
                    .Serialize()
                |> fun data ->
                    File.WriteAllText(
                        Path.Combine(parameters.SiteRoot, "pages", parameters.PageNameSlug, "data.json"),
                        data
                    )
                |> Ok
            with ex ->
                Error <| FailurePoint.GeneratePageData $"Unhandled exception: {ex.Message}")
        // ...and copy any relevant files for the page.
        |> Result.bind (fun _ ->
            try
                parameters.PageFiles
                |> List.iter (fun pf ->
                    match File.Exists pf.Path && File.Exists(pf.OutputPath) |> not with
                    | true -> File.Copy(pf.Path, pf.OutputPath)
                    | false -> ())
                |> Ok
            with ex ->
                Error <| FailurePoint.CopyResources $"Unhandled exception: {ex.Message}")

    let createBuildPageAction (ctx: StaticCMSContext) (parameters: Parameters) =
        // First add some content
        let contentPath =
            Path.Combine(parameters.SiteRoot, "pages", parameters.PageName, "fragments", "body.md")

        File.WriteAllText(
            contentPath,
            $"Page created. You can change this content `$root/pages/{parameters.PageName}/fragments/body.md` or editing `$root/build.json`."
        )

        ({ Name = parameters.PageName
           Template = defaultArticleTemplate
           Steps =
             [ ({ Path = "$root/data/fragments/navbar.json"
                  Fragment =
                    { Template = defaultNavTemplate
                      DataName = "navbar_html"
                      ContentType = FragmentBlobType.Json } }
               : Pipeline.AddPageFragmentPageBuildStep)
               |> Pipeline.BuildPageStep.AddPageFragment

               ({ Path = $"$root/pages/{parameters.PageName}/fragments/body.md"
                  Fragment =
                    { Template = "__blank"
                      DataName = "content"
                      ContentType = FragmentBlobType.Markdown } }
               : Pipeline.AddPageFragmentPageBuildStep)
               |> Pipeline.BuildPageStep.AddPageFragment
               Pipeline.BuildPageStep.AddPageData { Path = $"$root/pages/{parameters.PageName}/data.json" } ] }
        : Pipeline.BuildPageAction)

    let run (ctx: StaticCMSContext) (parameters: Parameters) =
        match ctx.Store.GetPage(parameters.SiteName, parameters.PageName) with
        | Some _ -> ()
        | None ->
            match handler ctx parameters with
            | Ok _ ->
                // Attempt to update the build script
                let buildPageAction = createBuildPageAction ctx parameters
                let buildCfgPath = Path.Combine(parameters.SiteRoot, "build.json")

                match File.Exists buildCfgPath with
                | true ->
                    match loadConfiguration buildCfgPath with
                    | Ok buildCfg ->
                        // Create a backup of the current build configuration.
                        File.WriteAllText($"{buildCfgPath}.bk", buildCfg.Serialize())

                        { buildCfg with
                            Steps =
                                buildCfg.Steps @ [ Pipeline.StepType.BuildPage buildPageAction ]
                                |> List.map (fun s -> s.GetDefaultOrder(), s)
                                |> List.sortBy fst
                                |> List.map snd }
                            .Serialize()
                        |> fun nbc -> File.WriteAllText(buildCfgPath, nbc)

                    | Error _ -> ()
                | false -> ()

                match parameters.NavigableTo with
                | true ->
                    // Attempt to add the site to the navbar (if applicable)
                    let navbarDataPath =
                        Path.Combine(parameters.SiteRoot, "data", "fragments", "navbar.json")

                    match File.Exists navbarDataPath with
                    | true ->
                        let navbarData = File.ReadAllText navbarDataPath |> deserializeJson<NavBarData>

                        { navbarData with
                            Items =
                                [ yield! navbarData.Items
                                  { Url = $"./{parameters.PageName}.html"
                                    Title = parameters.PageTitle } ] }
                        |> serializeJson
                        |> fun nnd -> File.WriteAllText(navbarDataPath, nnd)
                    | false -> ()

                    let navbarEmbeddedDataPath =
                        Path.Combine(parameters.SiteRoot, "data", "fragments", "navbar_embedded.json")

                    match File.Exists navbarEmbeddedDataPath with
                    | true ->
                        let navbarEmbeddedData =
                            File.ReadAllText navbarEmbeddedDataPath |> deserializeJson<NavBarData>

                        { navbarEmbeddedData with
                            Items =
                                [ yield! navbarEmbeddedData.Items
                                  { Url = $"../{parameters.PageName}.html"
                                    Title = parameters.PageTitle } ] }
                        |> serializeJson
                        |> fun nnd -> File.WriteAllText(navbarEmbeddedDataPath, nnd)
                    | false -> ()
                | false -> ()
            | Error e -> failwith "TODO"
