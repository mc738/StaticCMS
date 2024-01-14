namespace StaticCMS.Actions.Site

open StaticCMS.Persistence

[<RequireQualifiedAccess>]
module AddPlugin =

    open System.IO
    open StaticCMS
    open StaticCMS.Actions.Common

    type Parameters =
        { Site: Records.Site
          PluginName: string
          Args: (string * string) list
          PluginSettings: Plugins.Settings }

    let run (ctx: StaticCMSContext) (parameters: Parameters) =

        ({ Steps = parameters.PluginSettings.Initialization.Steps
           Args = parameters.Args
           SiteName = parameters.Site.Name
           SiteDisplayName = parameters.Site.DisplayName |> Option.defaultValue parameters.Site.Name
           StaticCMSRoot = ctx.RootPath
           SiteRoot = parameters.Site.RootPath }
        : Plugins.InitializePluginParameters)
        |> Plugins.initializePlugin
