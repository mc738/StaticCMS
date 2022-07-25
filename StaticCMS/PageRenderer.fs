namespace StaticCMS

open System
open System.Text
open Fluff.Core


module PageRenderer =

    open StaticCMS.DataStore
    open StaticCMS.Persistence
    
    let tryRenderPageFragment (store: StaticStore) (fragment: Records.PageFragment) =
        store.GetFragmentTemplate fragment.Template
        |> Option.map (fun ft ->
            let template = ft.Template.ToBytes() |> Encoding.UTF8.GetString |> Mustache.parse
            let lines = fragment.RawBlob.ToBytes() |> Encoding.UTF8.GetString |> fun r -> r.Split(Environment.NewLine)
            
            match fragment.BlobType with
            | "json" ->
                
                
                
                ()
            | "markdown" ->
                let d = FDOM.Core.Parsing.Parser.ParseLines([]).CreateBlockContent()
                FDOM.Rendering.Html.renderFromBlocks
                //FDOM.Rendering.Html.r
                ()
            
            )
        
        
    
    let run (store: StaticStore) (pageReference: string) =
        
        // fetch the page
        store.GetLatestPageVersion pageReference
        |> Option.map (fun pv ->
            let fragments =
                store.GetPageFragments pv.Reference
            
                    
            
            ())
        

