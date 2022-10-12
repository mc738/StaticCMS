namespace StaticCMS

open System.IO
open StaticCMS.DataStore

module Services =

    module Internal =

        type AgentResult =
            | Added
            | Failed of string

            member ar.ToResult() =
                match ar with
                | Added -> Ok()
                | Failed e -> Error e

        type AgentRequest =
            | AddSite of Name: string * Url: string * RootPath: string
            | AddPage of Reference: string * Site: string * Name: string * NameSlug: string
            | AddTemplate of Name: string * Raw: Stream
            | AddPageFragment of VersionRef: string * Template: string * DataName: string * Raw: Stream * BlobType: string
            | AddFragmentTemplate of Name: string * Raw: Stream
            | AddPageVersion of Reference: string * Site: string * Page: string * Template: string * IsDraft: bool
            | RenderPage of Site: string * Page: string

        type AgentMessage =
            { Request: AgentRequest
              ReplyChannel: AsyncReplyChannel<AgentResult> option }

            member am.Reply(result: AgentResult) =
                am.ReplyChannel
                |> Option.iter (fun rc -> rc.Reply result)

        let streamToBytes (stream: Stream) =
            async {
                use ms = new MemoryStream()

                do! stream.CopyToAsync ms |> Async.AwaitTask

                return ms.ToArray()
            }

        let agent (store: StaticStore) =
            MailboxProcessor<AgentMessage>.Start
                (fun inbox ->
                    let rec loop () =
                        async {

                            let! message = inbox.Receive()

                            match message.Request with
                            | AddSite (name, url, rootPath) ->

                                store.AddSite(name, url, rootPath)

                                message.Reply AgentResult.Added
                            | AddPage (reference, site, name, nameSlug) ->
                                store.AddPage(reference, site, name, nameSlug)
                                message.Reply Added

                            | AddTemplate (name, stream) ->
                                let! raw = streamToBytes stream

                                store.AddTemplate(name, raw)
                                message.Reply Added

                            | AddPageFragment (versionRef, template, dataName, stream, blobType) ->
                                let! raw = streamToBytes stream

                                store.AddPageFragment(versionRef, template, dataName, raw, blobType)

                                message.Reply AgentResult.Added

                            | AddFragmentTemplate (name, stream) ->
                                let! raw = streamToBytes stream

                                store.AddFragmentTemplate(name, raw)
                                message.Reply AgentResult.Added

                            | AddPageVersion (reference, site, page, template, isDraft) ->
                                match store.GetPage(site, page) with
                                | Some pi ->

                                    store.AddPageVersion(pi.Reference, reference, template, isDraft)
                                    message.Reply AgentResult.Added
                                | None ->
                                    message.Reply
                                    <| AgentResult.Failed $"Site `{site}` does not contain page `{page}`."

                            | RenderPage (site, page) ->
                                match store.GetSite site, store.GetPage(site, page) with
                                | Some sd, Some pd ->
                                    match PageRenderer.run store site page with
                                    | Ok r ->
                                        // TODO save as rendered page.
                                        File.WriteAllText(Path.Combine(sd.RootPath, $"{pd.NameSlug}.html"), r)
                                        message.Reply AgentResult.Added
                                    | Error e -> message.Reply <| AgentResult.Failed e
                                | None, _ ->
                                    message.Reply
                                    <| AgentResult.Failed $"Could not find site `{site}`."
                                | _, None ->
                                    message.Reply
                                    <| AgentResult.Failed $"Site `{site}` does not contain page `{page}`."


                            return! loop ()
                        }

                    loop ())

    open Internal

    type StaticCMSService(store: StaticStore) =

        let agent = Internal.agent store

        member _.AddSite(name, url, rootPath) =

            let result =
                agent.PostAndReply (fun rc ->
                    { Request = AgentRequest.AddSite(name, url, rootPath)
                      ReplyChannel = Some rc })

            result.ToResult()

        member _.AddPage(reference, site, name, nameSlug) =

            let result =
                agent.PostAndReply (fun rc ->
                    { Request = AgentRequest.AddPage(reference, site, name, nameSlug)
                      ReplyChannel = Some rc })

            result.ToResult()

        member _.AddTemplate(name, stream) =
            let result =
                agent.PostAndReply (fun rc ->
                    { Request = AgentRequest.AddTemplate(name, stream)
                      ReplyChannel = Some rc })

            result.ToResult()

        member _.AddFragmentTemplate(name, stream) =
            let result =
                agent.PostAndReply (fun rc ->
                    { Request = AgentRequest.AddFragmentTemplate(name, stream)
                      ReplyChannel = Some rc })

            result.ToResult()

        member _.AddPageVersion(reference, site, page, template, isDraft) =
            let result =
                agent.PostAndReply (fun rc ->
                    { Request = AgentRequest.AddPageVersion(reference, site, page, template, isDraft)
                      ReplyChannel = Some rc })

            result.ToResult()

        member _.AddPageFragment(versionReference, template, dataName, stream, blobType) =

            let result =
                agent.PostAndReply (fun rc ->
                    { Request = AgentRequest.AddPageFragment(versionReference, template, dataName, stream, blobType)
                      ReplyChannel = Some rc })

            result.ToResult()

        member _.RenderPage(site, page) =

            let result =
                agent.PostAndReply (fun rc ->
                    { Request = AgentRequest.RenderPage(site, page)
                      ReplyChannel = Some rc })

            result.ToResult()
