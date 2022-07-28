namespace StaticCMS.WebApi

open StaticCMS.DataStore
open StaticCMS.Services

[<RequireQualifiedAccess>]
module Routes =

    open System
    open Microsoft.AspNetCore.Authentication.JwtBearer
    open Microsoft.AspNetCore.Http
    open Microsoft.Extensions.Logging
    open Giraffe


    module Utils =

        let errorHandler (logger: ILogger) name code message =
            logger.LogError("Error '{code}' in route '{name}', message: '{message};.", code, name, message)
            setStatusCode code >=> text message

        let authorize: (HttpFunc -> HttpContext -> HttpFuncResult) =
            requiresAuthentication (challenge JwtBearerDefaults.AuthenticationScheme)

        let getClaim (ctx: HttpContext) (name: string) = ctx.User.FindFirst(name).Value

        let getUserRef (ctx: HttpContext) =
            match Guid.TryParse(getClaim ctx "userRef") with
            | true, ref -> Some(ref)
            | false, _ -> None

        let tryBindRequest<'a> (ctx: HttpContext) =
            try
                let result =
                    ctx.BindJsonAsync<'a>() |> Async.AwaitTask

                Ok(result)
            with
            | ex -> Error(sprintf "Could not bind request to type '%s'" typeof<'a>.Name)

        let tryGetHeader (ctx: HttpContext) header =
            match ctx.Request.Headers.TryGetValue header with
            | true, v -> Some v
            | false, _ -> None

        let tryGetQS (ctx: HttpContext) key = ctx.TryGetQueryStringValue key

        let tryGetFormValue (ctx: HttpContext) key = ctx.GetFormValue key

        let isTrue (str: string) =
            [ "true"; "yes"; "1"; "ok" ]
            |> List.contains (str.ToLower())

    module TestRoutes =
        //open RouteConfiguration

        let helloWorld = text "Hello, World!"

        let authTest = text "Signed in."

        let routes: (HttpFunc -> HttpContext -> HttpFuncResult) list =
            [ GET
              >=> choose [ route "/test" >=> helloWorld
                           route "/test/auth"
                           >=> Utils.authorize
                           >=> authTest ] ]

    module Sites =

        (*
        let getBlob =
            fun (next: HttpFunc) (ctx: HttpContext) ->
                async {
                    let log = ctx.GetLogger("get-blob")

                    // TODO check subscription-token.

                    match ctx.TryGetQueryStringValue("b"), ctx.TryGetQueryStringValue("k") with
                    | Some bucket, Some key ->
                        let skipCache =
                            ctx.TryGetQueryStringValue("no_cache")
                            |> Option.map isTrue
                            |> Option.defaultValue false

                        log.LogInformation($"Get blob {bucket}/{key} (Skip cache: {skipCache}).")
                        let service = ctx.GetService<BlobStoreService>()

                        match
                            service.GetBlob
                                (
                                    { Bucket = bucket
                                      Key = key
                                      Subscription = ""
                                      SkipCache = skipCache }
                                )
                            with
                        | Ok stream -> return streamData true (stream) None None next ctx
                        | Error e ->
                            printfn $"{e}"
                            // TODO log error.
                            return
                                (setStatusCode 400
                                 >=> text "Missing subscriber-token header.")
                                    next
                                    ctx
                    | _ ->
                        return
                            (setStatusCode 400
                             >=> text "Missing or invalid query parameters")
                                next
                                ctx
                }
                |> Async.RunSynchronously
        *)

        let addSite =
            let name = "/sites/add"

            fun (next: HttpFunc) (ctx: HttpContext) ->
                async {
                    let log = ctx.GetLogger("add-site")
                    //let formFeature = ctx.Features.Get<IFormFeature>()
                    let formContentType =
                        ctx.Request.HasFormContentType

                    let form = ctx.Request.Form // formFeature.ReadFormAsync CancellationToken.None |> Async.AwaitTask

                    let service =
                        ctx.GetService<StaticCMSService>()

                    let get key = Utils.tryGetFormValue ctx key

                    match get "name", get "url", get "rootpath" with
                    | Some n, Some u, Some rp ->
                        use s = log.BeginScope("Adding site")
                        log.LogInformation($"Name: {n}")
                        log.LogInformation($"Url: {u}")
                        log.LogInformation($"Root path: {rp}")

                        match service.AddSite(n, u, rp) with
                        | Ok _ -> return text "Site added." next ctx
                        | Error e ->
                            return (Utils.errorHandler log name 400 $"Error while processing the request: {e}") next ctx
                    | None, _, _ -> return (Utils.errorHandler log name 400 "Missing `site` parameter.") next ctx
                    | _, None, _ -> return (Utils.errorHandler log name 400 "Missing `url` parameter.") next ctx
                    | _, _, None -> return (Utils.errorHandler log name 400 "Missing `rootpath` parameter.") next ctx
                }
                |> Async.RunSynchronously

        let addPage =
            let name = "/pages/add"

            fun (next: HttpFunc) (ctx: HttpContext) ->
                async {
                    let log = ctx.GetLogger("add-page")
                    //let formFeature = ctx.Features.Get<IFormFeature>()
                    let formContentType =
                        ctx.Request.HasFormContentType

                    let form = ctx.Request.Form // formFeature.ReadFormAsync CancellationToken.None |> Async.AwaitTask

                    let service =
                        ctx.GetService<StaticCMSService>()

                    let get key = Utils.tryGetFormValue ctx key

                    match get "reference", get "site", get "name", get "nameslug" with
                    | Some r, Some s, Some n, Some ns ->
                        use ls = log.BeginScope("Adding page")
                        log.LogInformation($"Reference: {r}")
                        log.LogInformation($"Site: {s}")
                        log.LogInformation($"Name: {n}")
                        log.LogInformation($"Name slug: {ns}")

                        match service.AddPage(r, s, n, ns) with
                        | Ok _ -> return text "Page added." next ctx
                        | Error e ->
                            return (Utils.errorHandler log name 400 $"Error while processing the request: {e}") next ctx
                    | None, _, _, _ ->
                        return (Utils.errorHandler log name 400 "Missing `reference` parameter.") next ctx
                    | _, None, _, _ -> return (Utils.errorHandler log name 400 "Missing `site` parameter.") next ctx
                    | _, _, None, _ -> return (Utils.errorHandler log name 400 "Missing `name` parameter.") next ctx
                    | _, _, _, None -> return (Utils.errorHandler log name 400 "Missing `nameslug` parameter.") next ctx
                }
                |> Async.RunSynchronously

        let addTemplate =
            let name = "/templates/add"

            fun (next: HttpFunc) (ctx: HttpContext) ->
                async {
                    let log = ctx.GetLogger("add-template")
                    //let formFeature = ctx.Features.Get<IFormFeature>()
                    let formContentType =
                        ctx.Request.HasFormContentType

                    let form = ctx.Request.Form // formFeature.ReadFormAsync CancellationToken.None |> Async.AwaitTask

                    let service =
                        ctx.GetService<StaticCMSService>()

                    let get key = Utils.tryGetFormValue ctx key

                    match get "name" with
                    | Some n ->
                        use ls = log.BeginScope("Adding template")
                        log.LogInformation($"Name: {n}")

                        match service.AddTemplate(n, form.Files.[0].OpenReadStream()) with
                        | Ok _ -> return text "Template added." next ctx
                        | Error e ->
                            return (Utils.errorHandler log name 400 $"Error while processing the request: {e}") next ctx
                    | None -> return (Utils.errorHandler log name 400 "Missing `name` parameter.") next ctx
                }
                |> Async.RunSynchronously

        let addFragmentTemplate =
            let name = "/templates/fragment/add"

            fun (next: HttpFunc) (ctx: HttpContext) ->
                async {
                    let log =
                        ctx.GetLogger("add-fragment-template")
                    //let formFeature = ctx.Features.Get<IFormFeature>()
                    let formContentType =
                        ctx.Request.HasFormContentType

                    let form = ctx.Request.Form // formFeature.ReadFormAsync CancellationToken.None |> Async.AwaitTask

                    let service =
                        ctx.GetService<StaticCMSService>()

                    let get key = Utils.tryGetFormValue ctx key

                    match get "name" with
                    | Some n ->
                        use ls =
                            log.BeginScope("Adding fragment template")

                        log.LogInformation($"Name: {n}")

                        match service.AddFragmentTemplate(n, form.Files.[0].OpenReadStream()) with
                        | Ok _ -> return text "Fragment template added." next ctx
                        | Error e ->
                            return (Utils.errorHandler log name 400 $"Error while processing the request: {e}") next ctx
                    | None -> return (Utils.errorHandler log name 400 "Missing `name` parameter.") next ctx
                }
                |> Async.RunSynchronously

        let addPageVersion =
            let name = "/pages/versions/add"

            fun (next: HttpFunc) (ctx: HttpContext) ->
                async {
                    let log = ctx.GetLogger("add-page-version")
                    //let formFeature = ctx.Features.Get<IFormFeature>()
                    let formContentType =
                        ctx.Request.HasFormContentType

                    let form = ctx.Request.Form // formFeature.ReadFormAsync CancellationToken.None |> Async.AwaitTask

                    let service =
                        ctx.GetService<StaticCMSService>()

                    let get key = Utils.tryGetFormValue ctx key

                    match get "reference", get "site", get "page", get "template", get "isDraft" with
                    | Some r, Some s, Some p, Some t, Some d ->
                        use ls = log.BeginScope("Adding page")
                        log.LogInformation($"Reference: {r}")
                        log.LogInformation($"Site: {s}")
                        log.LogInformation($"Page: {p}")
                        log.LogInformation($"Template: {t}")
                        log.LogInformation($"Is draft: {d}")

                        match service.AddPageVersion(r, s, p, t, d |> Utils.isTrue) with
                        | Ok _ -> return text "Page version added." next ctx
                        | Error e ->
                            return (Utils.errorHandler log name 400 $"Error while processing the request: {e}") next ctx
                    | None, _, _, _, _ ->
                        return (Utils.errorHandler log name 400 "Missing `reference` parameter.") next ctx
                    | _, None, _, _, _ -> return (Utils.errorHandler log name 400 "Missing `site` parameter.") next ctx
                    | _, _, None, _, _ -> return (Utils.errorHandler log name 400 "Missing `page` parameter.") next ctx
                    | _, _, _, None, _ ->
                        return (Utils.errorHandler log name 400 "Missing `template` parameter.") next ctx
                    | _, _, _, _, None ->
                        return (Utils.errorHandler log name 400 "Missing `isdraft` parameter.") next ctx
                }
                |> Async.RunSynchronously

        let addPageFragment =
            let name = "/pages/fragment/add"

            fun (next: HttpFunc) (ctx: HttpContext) ->
                async {
                    let log = ctx.GetLogger("add-page-fragment")
                    //let formFeature = ctx.Features.Get<IFormFeature>()
                    let formContentType =
                        ctx.Request.HasFormContentType

                    let form = ctx.Request.Form // formFeature.ReadFormAsync CancellationToken.None |> Async.AwaitTask

                    let service =
                        ctx.GetService<StaticCMSService>()

                    let get key = Utils.tryGetFormValue ctx key

                    match get "versionref", get "template", get "blobtype" with
                    | Some vr, Some t, Some bt ->
                        use s =
                            log.BeginScope("Adding page fragment")

                        log.LogInformation($"Version ref: {vr}")
                        log.LogInformation($"Template: {t}")
                        log.LogInformation($"Blob type: {bt}")

                        match service.AddPageFragment(vr, t, form.Files.[0].OpenReadStream(), bt) with
                        | Ok _ -> return text "Page fragment saved." next ctx
                        | Error e ->
                            return (Utils.errorHandler log name 400 $"Error while processing the request: {e}") next ctx
                    | None, _, _ -> return (Utils.errorHandler log name 400 "Missing `versionref` parameter.") next ctx
                    | _, None, _ -> return (Utils.errorHandler log name 400 "Missing `template` parameter.") next ctx
                    | _, _, None -> return (Utils.errorHandler log name 400 "Missing `blobtype` parameter.") next ctx
                }
                |> Async.RunSynchronously

        let renderPage =
            let name = "/pages/render"

            fun (next: HttpFunc) (ctx: HttpContext) ->
                async {
                    let log = ctx.GetLogger("render-page")
                    //let formFeature = ctx.Features.Get<IFormFeature>()
                    let formContentType =
                        ctx.Request.HasFormContentType

                    let form = ctx.Request.Form // formFeature.ReadFormAsync CancellationToken.None |> Async.AwaitTask

                    let service =
                        ctx.GetService<StaticCMSService>()

                    let get key = Utils.tryGetFormValue ctx key

                    match get "site", get "page" with
                    | Some s, Some p ->
                        use ls =
                            log.BeginScope("Adding fragment template")

                        log.LogInformation($"Site: {s}")
                        log.LogInformation($"Page: {p}")

                        match service.RenderPage(s, p) with
                        | Ok _ -> return text "Page rendered." next ctx
                        | Error e ->
                            return (Utils.errorHandler log name 400 $"Error while processing the request: {e}") next ctx
                    | None, _ -> return (Utils.errorHandler log name 400 "Missing `site` parameter.") next ctx
                    | _, None -> return (Utils.errorHandler log name 400 "Missing `page` parameter.") next ctx
                }
                |> Async.RunSynchronously

        let routes: (HttpFunc -> HttpContext -> HttpFuncResult) list =
            (*GET >=> (*Routes.Utils.authorize >=>*) choose [ route "/pages/fragment/add" >=> (*Routes.Utils.authorize >=>*) getBlob ]*)
            [ POST
              >=> (*Routes.Utils.authorize >=>*) choose [ route "/sites/add" >=> addSite
                                                          route "/pages/add" >=> addPage
                                                          route "/templates/add" >=> addTemplate
                                                          route "/templates/fragments/add"
                                                          >=> addFragmentTemplate
                                                          route "/pages/versions/add" >=> addPageVersion
                                                          route "/pages/render" >=> renderPage
                                                          route "/pages/fragments/add"
                                                          >=> (*Routes.Utils.authorize >=>*) addPageFragment ] ]

    let all =
        List.concat [ TestRoutes.routes
                      Sites.routes
                      [ Utils.authorize
                        >=> choose Peeps.Monitoring.PeepsMetricRoutes.routes ] ]

    let app: (HttpFunc -> HttpContext -> HttpFuncResult) =
        choose all
