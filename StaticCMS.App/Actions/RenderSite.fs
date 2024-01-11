namespace StaticCMS.App.Actions

open StaticCMS.App.Common

module RenderSite =

    open System.IO
    open StaticCMS
    open StaticCMS.DataStore
    open StaticCMS.App.Common
    open StaticCMS.App.Common.Options

    let notInUse = ()
    
    (*
    let run (ctx: AppContext) (siteName) =
        match ctx.Store.GetSite siteName with
        | Some site ->
            match Pipeline.loadConfiguration (Path.Combine(site.RootPath, "build.json")) with
            | Ok cfg ->

                let scriptHost =
                    ({ FsiSession = Faaz.ScriptHost.fsiSession () }: Faaz.ScriptHost.HostContext)

                let knownPaths =
                    ("$root", site.RootPath)
                    :: (ctx.GeneralSettings.Paths
                        |> List.ofSeq
                        |> List.map (fun p -> $"${p.Name}", p.Path))
                    |> Map.ofList

                let ctx = PipelineContext.Create(ctx.Store, scriptHost, knownPaths)

                Pipeline.run ctx cfg
            | Error e -> Error $"Failed to create pipeline context: {e}"
        | None -> Error $"Unknown site `{siteName}`."


    let action (options: RenderSiteOptions) =

        let generalSettings =
            match getGeneralSettings options.ConfigurationPath with
            | Ok gs -> gs
            | Error _ -> failwith ""

        let storePath = getStorePath options.StorePath

        let store = StaticStore.Create storePath

        match store.GetSite options.Name with
        | Some site ->
            match Pipeline.loadConfiguration (Path.Combine(site.RootPath, "build.json")) with
            | Ok cfg ->

                let scriptHost =
                    ({ FsiSession = Faaz.ScriptHost.fsiSession () }: Faaz.ScriptHost.HostContext)

                let knownPaths =
                    ("$root", site.RootPath)
                    :: (generalSettings.Paths |> List.ofSeq |> List.map (fun p -> $"${p.Name}", p.Path))
                    |> Map.ofList

                let ctx = PipelineContext.Create(store, scriptHost, knownPaths)

                Pipeline.run ctx cfg
            | Error e -> Error $"Failed to create pipeline context: {e}"
        | None -> Error $"Unknown site `{options.Name}`."
    *)