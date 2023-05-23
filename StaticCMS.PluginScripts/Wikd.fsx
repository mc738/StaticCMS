#r """C:\Users\44748\Projects\StaticCMS\StaticCMS\bin\Debug\net6.0\StaticCMS.dll"""
#r """C:\ProjectData\static_cms\plugins\nugets\FDOM.Core.dll"""
#r """C:\ProjectData\static_cms\plugins\nugets\FDOM.Rendering.Html.dll"""
#r """C:\ProjectData\static_cms\plugins\nugets\Fluff.Core.dll"""
#r """C:\ProjectData\static_cms\plugins\nugets\Freql.Sqlite.dll"""
#r """C:\Users\44748\Projects\Wikd\Wikd\bin\Debug\net6.0\Wikd.dll"""

#r "nuget: Freql.Sqlite"
#r "nuget: Fluff.Core"
#r "nuget: FDOM.Core"
#r "nuget: FDOM.Rendering.Html"

open System.IO
open System.Text.Json
open Fluff.Core
open Wikd.DataStore
open StaticCMS.DataStore

let run storePath site =
    let staticStore = StaticStoreReader.Open storePath
    
    let result =
        match staticStore.GetSitePluginConfiguration(site, "wikd") with
        | Some rawCfg ->
            try
                JsonSerializer.Deserialize<ArticulusConfiguration> rawCfg
                |> Ok
            with
            | exn -> Error $"Failed to parse articulus configuration. Error: {exn.Message}"
        | None -> Error $"Wikd plugin not found for site `{site}`"
        |> Result.bind (fun cfg ->
            try
                let store = WikdStore.Create("C:\\ProjectData\\static_cms\\plugins\\wikd\\wikd.db")

                let template = File.ReadAllText "C:\\ProjectData\\static_cms\\plugins\\wikd\\resources\\wikd_template.mustache" |> Mustache.parse

                Wikd.Renderer.run store "C:\\ProjectData\\static_cms\\plugins\\wikd\\example\\wiki" template
                
                Ok ()
            with
            | exn -> Error $"Plugin failed. Error: {exn.Message}")
        
    match result with
    | Ok r -> r
    | Error e -> failwith $"{e}"