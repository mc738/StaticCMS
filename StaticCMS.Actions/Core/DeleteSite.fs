namespace StaticCMS.Actions.Core

open System.IO
open StaticCMS.Actions.Common
open StaticCMS.Actions.Core.RenderSite

[<RequireQualifiedAccess>]
module DeleteSite =

    type Parameters = { SiteName: string }

    let run (ctx: StaticCMSContext) (parameters: Parameters) =
        match ctx.Store.GetSite parameters.SiteName with
        | Some s ->
            ctx.Store.DeleteSite s
            |> Result.bind (fun _ ->
                try
                    Directory.Delete(s.RootPath, true) |> Ok
                with ex ->
                    Error $"Failed to delete site directory. This should be manually removed. Path: `{s.RootPath}`")
        | None -> Ok()
