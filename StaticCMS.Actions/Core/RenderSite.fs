namespace StaticCMS.Actions.Core

open StaticCMS.Actions.Common

module RenderSite =

    open System.IO
    open StaticCMS

    type Parameters =
        { SiteName: string
          ScriptHost: Faaz.ScriptHost.HostContext option }

    let run (ctx: StaticCMSContext) (parameters: Parameters) =

        match ctx.Store.GetSite parameters.SiteName with
        | Some site ->
            match Pipeline.loadConfiguration (Path.Combine(site.RootPath, "build.json")) with
            | Ok cfg ->

                let scriptHost =
                    parameters.ScriptHost
                    |> Option.defaultWith (fun _ ->
                        ({ FsiSession = Faaz.ScriptHost.fsiSession () }: Faaz.ScriptHost.HostContext))

                let knownPaths =
                    ("$root", site.RootPath)
                    :: (ctx.GeneralSettings.Paths
                        |> List.ofSeq
                        |> List.map (fun p -> $"${p.Name}", p.Path))
                    |> Map.ofList

                let ctx = Pipeline.PipelineContext.Create(ctx.Store, scriptHost, knownPaths)

                Pipeline.run ctx cfg
            | Error e -> Error $"Failed to create pipeline context: {e}"
        | None -> Error $"Unknown site `{parameters.SiteName}`."
