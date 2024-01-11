namespace StaticCMS.App.Actions

module ImportTemplate =
    
    open System.IO
    open StaticCMS.DataStore
    open StaticCMS.App.Common
    open StaticCMS.App.Common.Options    
    
    let notInUse = ()
    
    (*
    let action (options: ImportTemplateOptions) =
        try
            match File.Exists options.Path with
            | true ->
                let storePath = getStorePath options.StorePath

                let store = StaticStore.Create storePath

                match store.GetTemplate(options.Name) with
                | None -> store.AddTemplate(options.Name, File.ReadAllBytes options.Path) |> Ok
                | Some _ -> Error $"Template `{options.Name}` already exists"
            | false -> Error $"File `{options.Path}` does not exist"
        with exn ->
            Error $"Failed to import template. Error: {exn.Message}"

    *)