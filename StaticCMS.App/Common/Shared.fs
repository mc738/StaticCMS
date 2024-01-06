namespace StaticCMS.App.Common

[<AutoOpen>]
module Shared =

    open System
    open StaticCMS.DataStore
    open Logging

    let createRef _ = Guid.NewGuid().ToString("n")

    let getStorePath (path: string option) =
        path
        |> Option.defaultWith (fun _ -> Environment.GetEnvironmentVariable "STATIC_CMS_STORE")

    let getStaticRoot _ =
        Environment.GetEnvironmentVariable "STATIC_CMS_ROOT"

    type AppContext = { Store: StaticStore; Log: Logger }
