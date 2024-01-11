namespace StaticCMS

open System
open System.IO
open System.Security.Cryptography

[<AutoOpen>]
module Common =

    let hashStream (hasher: SHA256) (stream: Stream) =
        stream.Seek(0L, SeekOrigin.Begin) |> ignore
        let hash = hasher.ComputeHash stream |> Convert.ToHexString
        stream.Seek(0L, SeekOrigin.Begin) |> ignore
        hash

    let hashBytes (hasher: SHA256) (bytes: byte array) =
        hasher.ComputeHash bytes |> Convert.ToHexString

    let splitPath (path: string) = path.Split([| '\\'; '/' |])

    let expandPath (replacements: Map<string, string>) (path: string) =
        let splitPath = splitPath path

        [| splitPath
           |> Array.tryHead
           |> Option.map (fun p -> replacements.TryFind p |> Option.defaultValue p)
           |> Option.defaultValue ""
           yield! splitPath |> Array.tail |]
        |> Path.Combine

[<RequireQualifiedAccess>]
type FragmentBlobType =
    | Json
    | Markdown
    | Html

    static member TryDeserialize(str: string) =
        match str.ToLower() with
        | "json" -> Ok FragmentBlobType.Json
        | "markdown" -> Ok FragmentBlobType.Markdown
        | "html" -> Ok FragmentBlobType.Html
        | _ -> Error "$Unknown fragment blob type - `{str}`"

    static member All() =
        [ FragmentBlobType.Json; FragmentBlobType.Markdown; FragmentBlobType.Html ]

    member fbt.Serialize() =
        match fbt with
        | Json -> "json"
        | Markdown -> "markdown"
        | Html -> "html"

type FragmentTemplate =
    | Blank

    member ft.Serialize() = "__blank"
