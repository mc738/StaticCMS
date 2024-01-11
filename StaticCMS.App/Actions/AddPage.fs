namespace StaticCMS.App.Actions

module AddPage =

    open StaticCMS.App.Common
      
    let notInUse = ()
    
    (*
    let addPage (ctx: AppContext) (site: string) (name: string) (nameSlug: string) =
        match ctx.Store.GetPage(site, name) with
        | Some p -> ActionResult.Skipped($"Page `{name}` (site: `{site}`) already exists.", Some p.Reference)
        | None ->
            let ref = createRef ()
            ctx.Store.AddPage(ref, site, name, nameSlug)
            ActionResult.Success($"Page `{name}` added to site `{site}`.", Some ref)
    *)

