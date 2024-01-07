namespace StaticCMS.App.Actions

open StaticCMS.DataStore

module ImportFragmentTemplate =

    open System.IO
    open StaticCMS.App.Common
    open StaticCMS.App.Common.Options
        
    let action (options: ImportFragmentTemplateOptions) =
        try
            match File.Exists options.Path with
            | true ->
                let storePath = getStorePath options.StorePath

                let store = StaticStore.Create storePath

                match store.GetFragmentTemplate(options.Name) with
                | None -> store.AddFragmentTemplate(options.Name, File.ReadAllBytes options.Path) |> Ok
                | Some _ -> Error $"Fragment template `{options.Name}` already exists"
            | false -> Error $"File `{options.Path}` does not exist"
        with exn ->
            Error $"Failed to import template. Error: {exn.Message}"


