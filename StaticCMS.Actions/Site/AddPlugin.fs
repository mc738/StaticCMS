namespace StaticCMS.Actions.Site

[<RequireQualifiedAccess>]
module AddPlugin =

    open System.IO
    open StaticCMS
    open StaticCMS.Actions.Common
    
    type Parameters =
        {
            SiteName: string
            SiteRoot: string
            PluginName: string
        }
    
    let run (ctx: StaticCMSContext) (parameters: Parameters) =
        
        let pluginCfg = Path.Combine(ctx.RootPath, "plugins", parameters.PluginName, "settings.json")
        
        Plugins.Settings.Load pluginCfg
        |> Result.bind (fun cfg ->
            Plugins.initializePlugin cfg.Initialization.Steps ctx.RootPath parameters.SiteRoot)
        
    