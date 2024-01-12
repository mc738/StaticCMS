namespace StaticCMS.App.Actions

open StaticCMS.App.Common

module AddPlugin =

    let notInUse = ()
    
    (*
    let action (ctx: AppContext) (name: string) (pluginType: string) =
        match ctx.Store.GetPlugin name with
        | Some p -> ActionResult.Skipped($"Plugin `{name}` already exists.", Some p.Name)
        | None ->
            ctx.Store.AddPlugin(name, pluginType)
            ActionResult.Success($"Plugin `{name}` added.", None)
    *)