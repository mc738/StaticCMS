namespace StaticCMS.App.Actions

module AddSite =

    open StaticCMS.App.Common
    open StaticCMS.DataStore

    let notInUse = ()
(*
    let action (ctx: AppContext) (name: string) (url: string) (root: string) =
        match ctx.Store.TryAddSite(name, url, root) with
        | AddSiteResult.Success -> ctx.Log.LogSuccess("add-site", $"Site `{name}` added.")
        | AddSiteResult.AlreadyExists -> ctx.Log.LogInfo("add-site", $"Site `{name}` already exists.")
        | AddSiteResult.Failure e -> ctx.Log.LogError("add-site", $"Failed to add site `{name}`. Error: {e.Message}")
    *)
