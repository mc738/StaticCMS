open System
open FsToolbox.AppEnvironment.Args
open FsToolbox.Core
open StaticCMS.App.Actions
open StaticCMS.App
open StaticCMS.App.Common.Options

let args = Environment.GetCommandLineArgs()

match args.Length > 1 with
| true -> failwith "TODO"
    (*
    let options =
        Environment.GetCommandLineArgs()
        |> List.ofArray
        |> ArgParser.tryGetOptions<Options>

    let result =
        options
        |> Result.bind (fun o ->
            match o with
            | InitializeSite initializeSiteOptions -> InitializeSite.action initializeSiteOptions
            | AddSite addSiteOptions -> failwith "todo"
            | ImportTemplate importTemplateOptions -> ImportTemplate.action importTemplateOptions
            | RenderSite renderSiteOptions -> RenderSite.action renderSiteOptions
            | AddPath addPageOptions -> failwith "todo"
            | ImportFragmentTemplate importFragmentTemplateOptions -> ImportFragmentTemplate.action importFragmentTemplateOptions
            | AddSitePlugin -> failwith "todo"
            | RunInit -> failwith "todo")

    match result with
    | Ok _ -> ()
    | Error e -> ConsoleIO.printError e
    *)
| false -> InteractiveMode.run ()

// For more information see https://aka.ms/fsharp-console-apps
//printfn "Hello from F#"
