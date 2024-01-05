namespace StaticCMS.App

open System
open System.Security.Cryptography.Xml
open Faaz.Messaging
open StaticCMS.DataStore

module Actions =

    
    type ActionType =
        | InitializeSite of Name: string * Url: string * Root: string
        | AddPage of Site: string * Name: string * NameSlug: string


    type ActionResult<'T> =
        | Success of Message: string * Result: 'T option
        | Skipped of Message: string * Result: 'T option
        | Failed of Message: string

    let addSite (ctx: AppContext) (name: string) (url: string) (root: string) =
        match ctx.Store.TryAddSite(name, url, root) with
        | AddSiteResult.Success -> ctx.Log.LogSuccess("add-site", $"Site `{name}` added.")
        | AddSiteResult.AlreadyExists -> ctx.Log.LogInfo("add-site", $"Site `{name}` already exists.")
        | AddSiteResult.Failure e -> ctx.Log.LogError("add-site", $"Failed to add site `{name}`. Error: {e.Message}")
        
    let addPage (store: StaticStore) (site: string) (name: string) (nameSlug: string) =
        match store.GetPage(site, name) with
        | Some p -> ActionResult.Skipped($"Page `{name}` (site: `{site}`) already exists.", Some p.Reference)
        | None ->
            let ref = createRef ()
            store.AddPage(ref, site, name, nameSlug)
            ActionResult.Success($"Page `{name}` added to site `{site}`.", Some ref)

    let addPlugin (store: StaticStore) (name: string) (pluginType: string) =
        match store.GetPlugin name with
        | Some p -> ActionResult.Skipped($"Plugin `{name}` already exists.", Some p.Name)
        | None ->
            store.AddPlugin(name, pluginType)
            ActionResult.Success($"Plugin `{name}` added.", None)