namespace StaticCMS

open System
open System.IO
open System.Text
open Fluff.Core
open Microsoft.FSharp.Core


module PageRenderer =

    open StaticCMS.DataStore
    open StaticCMS.Persistence

    let tryRenderPageFragment (store: StaticStore) (fragment: Records.PageFragment) =
        store.GetFragmentTemplate fragment.Template
        |> Option.bind (fun ft ->
            let template =
                ft.Template.ToBytes() |> Encoding.UTF8.GetString //|> Mustache.parse

            let raw =
                fragment.RawBlob.ToBytes()
                |> Encoding.UTF8.GetString //|> fun r -> r.Split(Environment.NewLine)

            match fragment.BlobType with
            | "json" ->
                Some
                <| (ft.Name, PageFragments.renderJsonFragment template raw |> Mustache.Value.Scalar)
            | "markdown" ->
                Some
                <| (ft.Name, PageFragments.renderMarkdownFragment PageFragments.rewriteTitles raw |> Mustache.Value.Scalar)
            | _ -> None)

    let run (store: StaticStore) (site: string) (page: string) =

        store.GetPageByName(site, page)
        |> Option.bind (fun p  -> store.GetLatestPageVersion p.Reference)
        |> Option.map (fun pv ->

            match store.GetTemplateString pv.Template with
            | Some template ->
                let tokens = Mustache.parse template
                
                store.GetPageFragments pv.Reference
                |> List.choose (tryRenderPageFragment store)
                |> Map.ofList
                |> fun v ->
                    ({
                        Values = v
                        Partials = Map.empty
                    }: Mustache.Data)
                |> fun d -> Mustache.replace d true tokens |> Ok
            | None -> Error $"Template `{pv.Template}` not found.")
        |> Option.defaultWith (fun _ -> Error "Page not found.")