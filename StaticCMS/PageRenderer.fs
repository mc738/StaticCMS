namespace StaticCMS

open System
open System.IO
open System.Text
open Fluff.Core
open Microsoft.FSharp.Core
open StaticCMS.DataStore


module PageRenderer =

    open StaticCMS.DataStore
    open StaticCMS.Persistence

    //let renderPageFragment (store: Records.PageFragment) =

    let combineFragments (store: StaticStore) (template: string) (pageVersion: string) (fragments: string list) =
        store.GetFragmentTemplate template
        |> Option.map (fun t ->
            fragments
            |> List.choose (fun fn ->
                store.GetPageFragment(pageVersion, fn)
                |> Option.bind (fun f ->
                    match store.GetFragmentTemplate f.Template with
                    | Some ft -> Some(f, ft)
                    | None -> None)
                |> Option.bind (fun (f, ft) ->
                    let template = ft.Template.ToBytes() |> Encoding.UTF8.GetString

                    let raw = f.RawBlob.ToBytes() |> Encoding.UTF8.GetString

                    match f.BlobType |> FragmentBlobType.TryDeserialize with
                    | Ok FragmentBlobType.Json ->
                        Some
                        <| (f.DataName, PageFragments.renderJsonFragment template raw |> Mustache.Value.Scalar)
                    | Ok FragmentBlobType.Markdown ->
                        Some
                        <| (f.DataName,
                            PageFragments.renderMarkdownFragment PageFragments.rewriteTitles raw
                            |> Mustache.Value.Scalar)
                    | Ok FragmentBlobType.Html -> Some(f.DataName, raw |> Mustache.Value.Scalar)
                    | Error _ -> None))
            |> fun vs ->
                let tokens = t.Template.ToBytes() |> Encoding.UTF8.GetString |> Mustache.parse

                ({ Values = vs |> Map.ofList
                   Partials = Map.empty }
                : Mustache.Data)
                |> fun d -> Mustache.replace d true tokens |> Ok)
        |> Option.defaultWith (fun _ -> Error $"Template `{template}` not found.")

    let tryRenderPageFragment (store: StaticStore) (fragment: Records.PageFragment) =
        store.GetFragmentTemplate fragment.Template
        |> Option.bind (fun ft ->
            let template = ft.Template.ToBytes() |> Encoding.UTF8.GetString //|> Mustache.parse

            let raw = fragment.RawBlob.ToBytes() |> Encoding.UTF8.GetString //|> fun r -> r.Split(Environment.NewLine)

            match fragment.BlobType |> FragmentBlobType.TryDeserialize with
            | Ok FragmentBlobType.Json ->
                Some
                <| (fragment.DataName, PageFragments.renderJsonFragment template raw |> Mustache.Value.Scalar)
            | Ok FragmentBlobType.Markdown ->
                Some
                <| (fragment.DataName,
                    PageFragments.renderMarkdownFragment PageFragments.rewriteTitles raw
                    |> Mustache.Value.Scalar)
            | Ok FragmentBlobType.Html -> Some(fragment.DataName, raw |> Mustache.Value.Scalar)
            | Error _ -> None)

    let run (store: StaticStore) (site: string) (page: string) =

        store.GetPageByName(site, page)
        |> Option.bind (fun p -> store.GetLatestPageVersion p.Reference)
        |> Option.map (fun pv ->

            match store.GetTemplateString pv.Template with
            | Some template ->
                let tokens = Mustache.parse template

                [ // Page data
                  yield!
                      store.GetPageData pv.Reference
                      |> List.choose (fun pd ->
                          try
                              // NOTE this could be a bit clear but would require a change to Fluff.
                              Mustache.Data
                                  .FromJson(
                                      pd.RawBlob.ToBytes() |> Encoding.UTF8.GetString,
                                      PageFragments.renderInline
                                  )
                                  .Values
                              |> Map.toList
                              |> Some
                          with exn ->
                              None)
                      |> List.concat

                  // Page fragments
                  yield! store.GetPageFragments pv.Reference |> List.choose (tryRenderPageFragment store) ]
                |> Map.ofList
                |> fun v -> ({ Values = v; Partials = Map.empty }: Mustache.Data)
                |> fun d -> Mustache.replace d true tokens |> Ok
            | None -> Error $"Template `{pv.Template}` not found.")
        |> Option.defaultWith (fun _ -> Error "Page not found.")
