namespace StaticCMS

open System
open System.Text.Json
open FDOM.Core.Common
open FDOM.Core.Serialization
open Fluff.Core

module PageFragments =

    let rewriteTitles (block: DOM.BlockContent) =
        match block with
        | DOM.BlockContent.Header h ->
            match h.Level with
            | DOM.HeaderLevel.H2 ->
                { h with Style = DOM.Style.Ref [ "title" ] }
                |> DOM.BlockContent.Header
            | _ -> block
        | _ -> block

    let renderMarkdownFragment (rewriter: DOM.BlockContent -> DOM.BlockContent) (raw: string) =
        FDOM
            .Core
            .Parsing
            .Parser
            .ParseLines(raw.Split(Environment.NewLine) |> List.ofArray)
            .CreateBlockContent()
        |> List.map rewriter
        |> FDOM.Rendering.Html.renderFromBlocks

    let passThru (str: string) = str
    
    let renderInline (str: string) =
        FDOM.Core.Parsing.InlineParser.parseInlineContent str
        |> FDOM.Rendering.Html.renderInlineItems
        // Here to handle cases where the input has already been html encoded.
        // This possibly could be handled better.
        |> System.Web.HttpUtility.HtmlDecode
    
    let rec elementToValue (element: JsonElement) =
        match element.ValueKind with
        | JsonValueKind.Array ->
            element.EnumerateArray()
            |> List.ofSeq
            |> List.choose elementToValue
            |> Mustache.Value.Array
            |> Some // None
        | JsonValueKind.False -> Mustache.Value.Scalar "false" |> Some
        | JsonValueKind.Null -> None
        | JsonValueKind.Number ->
            element.GetDecimal()
            |> string
            |> Mustache.Value.Scalar
            |> Some
        | JsonValueKind.Object ->
            element.EnumerateObject()
            |> List.ofSeq
            |> List.choose (fun p ->
                elementToValue p.Value
                |> Option.map (fun v -> p.Name, v))
            |> Map.ofList
            |> Mustache.Value.Object
            |> Some
        | JsonValueKind.String ->
            element.GetString()
            |> renderInline
            |> Mustache.Value.Scalar
            |> Some
        | JsonValueKind.True -> Mustache.Value.Scalar "true" |> Some
        | JsonValueKind.Undefined -> None
        | _ -> None

    let renderJsonFragment (template: string) (json: string) =
        let tokens = Mustache.parse template
        let data = Mustache.Data.FromJson(json, renderInline)
        
        Mustache.replace data true tokens
        