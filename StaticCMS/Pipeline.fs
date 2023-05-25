namespace StaticCMS

open System
open System.IO
open System.Text
open System.Text.Json
open Faaz
open Faaz.ScriptHost
open FsToolbox.Core
open StaticCMS.DataStore

module Pipeline =

    let toPathParts (str: string) = str.Split([| '\\'; '/' |])

    let collectErrors (results: Result<'T, string> list) =
        results
        |> List.fold
            (fun (acc, errors) r ->
                match r with
                | Ok v -> acc @ [ v ], errors
                | Error e -> acc, errors @ [ e ])
            ([], [])
        |> fun (acc, errors) ->
            match errors.IsEmpty with
            | true -> Ok acc
            | false ->
                "The following errors occurred:" :: errors
                |> String.concat Environment.NewLine
                |> Error

    let collectResults (results: Result<'T, string> list) =
        results
        |> List.fold
            (fun (acc, errors) r ->
                match r with
                | Ok v -> acc @ [ v ], errors
                | Error e -> acc, errors @ [ e ])
            ([], [])

    let collectResults1 (results: Result<'T, string> list) =
        results
        |> collectResults
        |> fun (acc, errors) ->
            match errors.IsEmpty with
            | true -> Ok acc
            | false ->
                [ "The following errors occurred:"; yield! errors ]
                |> String.concat Environment.NewLine
                |> Error

    let collectResults2<'T1, 'T2> (resultsA: Result<'T1, string> list, resultsB: Result<'T2, string> list) =
        let (acc1, errors1) = resultsA |> collectResults

        let (acc2, errors2) = resultsB |> collectResults

        match errors1.IsEmpty && errors2.IsEmpty with
        | true -> Ok(acc1, acc2)
        | false ->
            [ "The following errors occurred:"; yield! errors1; yield! errors2 ]
            |> String.concat Environment.NewLine
            |> Error

    let bind2Results<'T1, 'T2, 'U, 'E> (fn: 'T1 -> 'T2 -> Result<'U, 'E>) (r1: Result<'T1, 'E>, r2: Result<'T2, 'E>) =
        match r1, r2 with
        | Ok v1, Ok v2 -> fn v1 v2
        | Error e1, _ -> Error e1
        | _, Error e2 -> Error e2

    type PipelineConfiguration =
        { Site: string
          Steps: StepType list }

        static member Deserialize(el: JsonElement) =
            match Json.tryGetStringProperty "site" el, Json.tryGetArrayProperty "steps" el with
            | Some site, Some steps ->
                steps
                |> List.map StepType.Deserialize
                |> collectResults
                |> fun (steps, errors) ->
                    match errors.IsEmpty with
                    | true -> Ok { Site = site; Steps = steps }
                    | false -> Error errors
            | None, _ -> Error [ "Missing `site` property." ]
            | _, None -> Error [ "Missing `steps` property." ]

        member pc.Serialize() =
            use ms = new MemoryStream()
            let mutable options = JsonWriterOptions()
            options.Indented <- true

            use writer = new Utf8JsonWriter(ms, options)

            writer.WriteStartObject()

            Json.writeString writer "site" pc.Site
            Json.writeArray (fun w -> pc.Steps |> List.iter (fun s -> s.WriteToJson w)) "steps" writer


            writer.WriteEndObject()
            writer.Flush()

            ms.ToArray() |> Encoding.UTF8.GetString

    and StepType =
        | CreateDirectories of CreateDirectoriesAction
        | CopyResources of CopyResourcesAction
        | BuildPage of BuildPageAction
        | RunPluginScript of RunPluginScriptAction

        static member Deserialize(el: JsonElement) =
            match Json.tryGetStringProperty "type" el with
            | Some "create-directories" ->
                match Json.tryGetProperty "directories" el with
                | Some dirs ->
                    CreateDirectoriesAction.Deserialize dirs
                    |> Result.map StepType.CreateDirectories
                | None -> Error "create-directories: Missing `directories` property."
            | Some "copy-resources" -> CopyResourcesAction.Deserialize el |> Result.map StepType.CopyResources
            | Some "build-page" -> BuildPageAction.Deserialize el |> Result.map StepType.BuildPage
            | Some "run-plugin-script" -> RunPluginScriptAction.Deserialize el |> Result.map StepType.RunPluginScript
            | Some t -> Error $"Unknown step type `{t}`."
            | None -> Error "Missing `type` property."

        member internal st.WriteToJson(writer: Utf8JsonWriter) =
            writer.WriteStartObject()

            match st with
            | StepType.CreateDirectories createDirectoriesAction ->
                Json.writeString writer "type" "create-directories"
                createDirectoriesAction.WriteToJson writer
            | CopyResources copyResourcesAction ->
                writer.WriteString("type", "copy-resources")
                copyResourcesAction.WriteToJson writer
            | BuildPage buildPageAction ->
                writer.WriteString("type", "build-page")
                buildPageAction.WriteToJson writer
            | RunPluginScript runPluginScriptAction ->
                writer.WriteString("type", "run-plugin-script")
                runPluginScriptAction.WriteToJson writer

            writer.WriteEndObject()

    and CreateDirectoriesAction =
        { Directories: string list }

        static member Deserialize(el: JsonElement) =
            match Json.tryGetStringArray el with
            | Some dirs -> Ok { Directories = dirs }
            | None -> Error "Could not get string array."

        member internal cda.WriteToJson(writer: Utf8JsonWriter) =
            Json.writeArray (fun w -> cda.Directories |> List.iter (fun d -> w.WriteStringValue d)) "directories" writer

    and CopyResourcesAction =
        { Directories: CopyDirectory list
          Files: CopyFile list }

        static member Deserialize(el: JsonElement) =
            match Json.tryGetArrayProperty "directories" el, Json.tryGetArrayProperty "files" el with
            | Some ds, Some fs ->
                (ds |> List.map CopyDirectory.Deserialize, fs |> List.map CopyFile.Deserialize)
                |> collectResults2
                |> Result.bind (fun (ds, fs) -> Ok { Directories = ds; Files = fs })
            | None, _ -> Error "Missing `directories` property."
            | _, None -> Error "Missing `files` property."

        member internal cra.WriteToJson(writer: Utf8JsonWriter) =
            Json.writeArray (fun w -> cra.Directories |> List.iter (fun cd -> cd.WriteToJson w)) "directories" writer
            Json.writeArray (fun w -> cra.Files |> List.iter (fun cd -> cd.WriteToJson w)) "files" writer

    and CopyDirectory =
        { From: string
          To: string }

        static member Deserialize(el: JsonElement) =
            match Json.tryGetStringProperty "from" el, Json.tryGetStringProperty "to" el with
            | Some f, Some t -> Ok { From = f; To = t }
            | None, _ -> Error "Copy directory element missing `from` property."
            | _, None -> Error "Copy directory element missing `to` property."

        member internal cd.WriteToJson(writer: Utf8JsonWriter) =
            writer.WriteStartObject()
            writer.WriteString("from", cd.From)
            writer.WriteString("to", cd.To)
            writer.WriteEndObject()

    and CopyFile =
        { From: string
          To: string }

        static member Deserialize(el: JsonElement) =
            match Json.tryGetStringProperty "from" el, Json.tryGetStringProperty "to" el with
            | Some f, Some t -> Ok { From = f; To = t }
            | None, _ -> Error "Copy file element missing `from` property."
            | _, None -> Error "Copy file element missing `to` property."


        member internal cf.WriteToJson(writer: Utf8JsonWriter) =
            writer.WriteStartObject()
            writer.WriteString("from", cf.From)
            writer.WriteString("to", cf.To)
            writer.WriteEndObject()

    and BuildPageAction =
        { Name: string
          Template: string
          Steps: BuildPageStep list }

        static member Deserialize(el: JsonElement) =
            match
                Json.tryGetStringProperty "name" el,
                Json.tryGetStringProperty "template" el,
                Json.tryGetArrayProperty "steps" el
            with
            | Some name, Some template, Some steps ->
                steps
                |> List.map BuildPageStep.Deserialize
                |> collectResults1
                |> Result.map (fun s ->
                    { Name = name
                      Template = template
                      Steps = s })
            | None, _, _ -> Error "Missing `name` property."
            | _, None, _ -> Error "Missing `template` property."
            | _, _, None -> Error "Missing `steps` property."

        member internal bpa.WriteToJson(writer: Utf8JsonWriter) =
            writer.WriteString("name", bpa.Name)
            writer.WriteString("template", bpa.Template)
            Json.writeArray (fun w -> bpa.Steps |> List.iter (fun s -> s.WriteToJson w)) "steps" writer

    and BuildPageStep =
        | AddPageFragment of AddPageFragmentPageBuildStep
        | CombinePageFragments of CombinePageFragmentsPageBuildStep
        | AddPageData of AddPageDataBuildStep
        //| RunPlugin

        static member Deserialize(el: JsonElement) =
            match Json.tryGetStringProperty "type" el with
            | Some "add-page-fragment" ->
                AddPageFragmentPageBuildStep.Deserialize el
                |> Result.map BuildPageStep.AddPageFragment
            | Some "combine-page-fragments" ->
                CombinePageFragmentsPageBuildStep.Deserialize el
                |> Result.map BuildPageStep.CombinePageFragments
            | Some "add-page-data" -> AddPageDataBuildStep.Deserialize el |> Result.map BuildPageStep.AddPageData
            //| Some "run-plugin" -> Ok()
            | Some t -> Error $"Unknown step type `{t}`."
            | None -> Error "Missing `type` property."

        member internal bps.WriteToJson(writer: Utf8JsonWriter) =
            writer.WriteStartObject()

            match bps with
            | AddPageFragment addPageFragmentPageBuildStep ->
                writer.WriteString("type", "add-page-fragment")
                addPageFragmentPageBuildStep.WriteToJson writer
            | CombinePageFragments combinePageFragmentsPageBuildStep ->
                writer.WriteString("type", "combine-page-fragments")
                combinePageFragmentsPageBuildStep.WriteToJson writer
            | AddPageData addPageDataPageBuildStep ->
                writer.WriteString("type", "add-page-data")
                addPageDataPageBuildStep.WriteToJson writer

            writer.WriteEndObject()

    and AddPageFragmentPageBuildStep =
        { Path: string
          Fragment: FragmentActionData }

        static member Deserialize(el: JsonElement) =
            match Json.tryGetStringProperty "path" el, Json.tryGetProperty "fragment" el with
            | Some path, Some fragmentEl ->
                FragmentActionData.Deserialize fragmentEl
                |> Result.map (fun fd -> { Path = path; Fragment = fd })
            | None, _ -> Error "Missing `path` property."
            | _, None -> Error "Missing `fragment` property."

        member internal apf.WriteToJson(writer: Utf8JsonWriter) =
            writer.WriteString("path", apf.Path)
            writer.WritePropertyName("fragment")
            apf.Fragment.WriteToJson writer

    and CombinePageFragmentsPageBuildStep =
        { OutputName: string
          Template: string
          Fragments: string list }

        static member Deserialize(el: JsonElement) =
            match
                Json.tryGetStringProperty "outputName" el,
                Json.tryGetStringProperty "template" el,
                Json.tryGetProperty "fragments" el |> Option.bind Json.tryGetStringArray
            with
            | Some on, Some t, Some fs ->
                Ok
                    { OutputName = on
                      Template = t
                      Fragments = fs }
            | None, _, _ -> Error "Missing `outputName` property."
            | _, None, _ -> Error "Missing `template` property."
            | _, _, None -> Error "Missing/invalid `fragments` property."

        member internal cpf.WriteToJson(writer: Utf8JsonWriter) =
            writer.WriteString("outputName", cpf.OutputName)
            writer.WriteString("template", cpf.Template)
            Json.writeArray (fun w -> cpf.Fragments |> List.iter writer.WriteStringValue) "fragments" writer

    and AddPageDataBuildStep =
        { Path: string }

        static member Deserialize(el: JsonElement) =
            match Json.tryGetStringProperty "path" el with
            | Some p -> Ok { Path = p }
            | None -> Error "Missing `path` property."

        member internal cpf.WriteToJson(writer: Utf8JsonWriter) = writer.WriteString("path", cpf.Path)

    and RunPluginScriptAction =
        { Name: string
          Script: string
          Function: string
          ReturnType: PluginReturnType }

        static member Deserialize(el: JsonElement) =
            match
                Json.tryGetStringProperty "name" el,
                Json.tryGetStringProperty "script" el,
                Json.tryGetStringProperty "function" el,
                Json.tryGetProperty "returnType" el
                |> Option.map PluginReturnType.TryDeserialize
                |> Option.defaultWith (fun _ -> Error "Missing `returnType` property")
            with
            | Some name, Some script, Some functionName, Ok returnType ->
                Ok
                    { Name = name
                      Script = script
                      Function = functionName
                      ReturnType = returnType }
            | None, _, _, _ -> Error "Missing `name` property."
            | _, None, _, _ -> Error "Missing `script` property."
            | _, _, None, _ -> Error "Missing `function` property."
            | _, _, _, Error e -> Error e

        member internal apf.WriteToJson(writer: Utf8JsonWriter) =
            writer.WriteString("name", apf.Name)
            writer.WriteString("script", apf.Script)
            writer.WriteString("function", apf.Function)
            writer.WritePropertyName("returnType")
            apf.ReturnType.WriteToJson writer

    and [<RequireQualifiedAccess>] PluginReturnType =
        | Fragment of OutputPath: string
        | None

        static member TryDeserialize(json: JsonElement) =
            match Json.tryGetStringProperty "type" json with
            | Option.Some "fragment" ->
                match Json.tryGetStringProperty "outputPath" json with
                | Option.Some op -> Fragment op |> Ok
                | Option.None -> Error "Missing `outputPath` property"
            | Option.Some "none" -> Ok None
            | Option.Some v -> Error $"Unknown plugin output type: `{v}`"
            | Option.None -> Error "Missing `type` property"


        member internal prt.WriteToJson(writer: Utf8JsonWriter) =
            writer.WriteStartObject()

            match prt with
            | Fragment outputPath ->
                writer.WriteString("type", "fragment")
                writer.WriteString("outputPath", outputPath)
            | None -> writer.WriteString("type", "none")

            writer.WriteEndObject()

    and FragmentActionData =
        { Template: string
          DataName: string
          ContentType: FragmentBlobType }

        static member Deserialize(el: JsonElement) =
            match
                Json.tryGetStringProperty "template" el,
                Json.tryGetStringProperty "dataName" el,
                Json.tryGetStringProperty "contentType" el
                |> Option.map FragmentBlobType.TryDeserialize
                |> Option.defaultWith (fun _ -> Error "Fragment element missing `contentType` property.")
            with
            | Some t, Some dn, Ok ct ->
                Ok
                    { Template = t
                      DataName = dn
                      ContentType = ct }
            | None, _, _ -> Error "Fragment element missing `template` property."
            | _, None, _ -> Error "Fragment element missing `dataName` property."
            | _, _, Error e -> Error e

        member internal apf.WriteToJson(writer: Utf8JsonWriter) =
            writer.WriteStartObject()

            writer.WriteString("template", apf.Template)
            writer.WriteString("dataName", apf.DataName)
            writer.WriteString("contentType", apf.ContentType.Serialize())

            writer.WriteEndObject()

    type PipelineContext =
        { Store: StaticStore
          ScriptHost: HostContext
          KnownPaths: Map<string, string> }

        static member Create(store: StaticStore, scriptHost: HostContext, knownPaths: Map<string, string>) =
            { Store = store
              ScriptHost = scriptHost
              KnownPaths = knownPaths }

        member ctx.ExpandPath(path: string) =
            let splitPath = toPathParts path

            [| splitPath
               |> Array.tryHead
               |> Option.map (fun p -> ctx.KnownPaths.TryFind p |> Option.defaultValue p)
               |> Option.defaultValue ""
               yield! splitPath |> Array.tail |]
            |> Path.Combine

    let deserializeStep (el: JsonElement) =
        match Json.tryGetStringProperty "type" el with
        | Some "create-directories" -> Ok()
        | Some "copy-resources" -> Ok()
        | Some "build-page" -> Ok()
        | Some t -> Error $"Unknown step type `{t}`."
        | None -> Error "Missing `type` property."

    let deserializeConfiguration (json: string) =
        let el = (JsonDocument.Parse json).RootElement

        match Json.tryGetStringProperty "site" el, Json.tryGetArrayProperty "steps" el with
        | Some site, Some steps -> Ok()
        | None, _ -> Error "Missing `site` property."
        | _, None -> Error "Missing `steps` property."

    let loadConfiguration (path: string) =
        JsonDocument.Parse(File.ReadAllText path).RootElement
        |> PipelineConfiguration.Deserialize

    let expandPath (knownPaths: Map<string, string>) (path: string) =
        let splitPath = toPathParts path

        [| splitPath
           |> Array.tryHead
           |> Option.map (fun p -> knownPaths.TryFind p |> Option.defaultValue p)
           |> Option.defaultValue ""
           yield! splitPath |> Array.tail |]
        |> Path.Combine

    module Actions =

        let private attempt (fn: unit -> Result<unit, string>) =
            try
                fn ()
            with exn ->
                Error $"Unhandled exception: {exn.Message}"

        let private createRef _ = Guid.NewGuid().ToString("n")

        let createDirectories (ctx: PipelineContext) (action: CreateDirectoriesAction) =
            let fn _ =
                action.Directories
                |> List.iter (fun d ->
                    let path = ctx.ExpandPath d

                    match Directory.Exists d with
                    | true -> ()
                    | false -> Directory.CreateDirectory path |> ignore)
                |> Ok

            attempt fn

        let copyResources (ctx: PipelineContext) (action: CopyResourcesAction) =
            let fn _ =
                action.Directories
                |> List.iter (fun d ->
                    let fromPath = ctx.ExpandPath d.From
                    let toPath = ctx.ExpandPath d.To

                    Directory.EnumerateFiles(fromPath)
                    |> List.ofSeq
                    |> List.iter (fun f -> File.Copy(f, Path.Combine(toPath, Path.GetFileName(f)))))

                action.Files
                |> List.iter (fun f -> File.Copy(ctx.ExpandPath f.From, ctx.ExpandPath f.To))

                Ok()

            attempt fn

        let runPluginScript (site: string) (ctx: PipelineContext) (action: RunPluginScriptAction) =
            let fn _ =
                let scriptPath = ctx.ExpandPath action.Script

                let runCmd = $"""{action.Function} "{ctx.Store.Path}" "{site}" """

                match action.ReturnType with
                | PluginReturnType.Fragment outputPath ->
                    ctx.ScriptHost.Eval<string>(scriptPath, runCmd)
                    |> Result.map (fun fd -> File.WriteAllText(ctx.ExpandPath outputPath, fd))
                | PluginReturnType.None -> ctx.ScriptHost.Eval<unit>(scriptPath, runCmd)

            attempt fn

        let buildPageStep (versionRef: string) (ctx: PipelineContext) (step: BuildPageStep) =
            match step with
            | BuildPageStep.AddPageFragment data ->
                let path = ctx.ExpandPath data.Path
                
                match File.Exists path with
                | true ->
                    ctx.Store.AddPageFragment(
                        versionRef,
                        data.Fragment.Template,
                        data.Fragment.DataName,
                        File.ReadAllBytes path,
                        data.Fragment.ContentType
                    )
                | false ->
                    // TODO handle error.
                    ()
            | CombinePageFragments data ->
                // Get page fragments and render into new fragment.
                match PageRenderer.combineFragments ctx.Store data.Template versionRef data.Fragments with
                | Ok f ->
                    ctx.Store.AddPageFragment(
                        versionRef,
                        FragmentTemplate.Blank.Serialize(),
                        data.OutputName,
                        f |> Encoding.UTF8.GetBytes,
                        FragmentBlobType.Html
                    )
                | Error e ->
                    // TODO handle error?
                    ()
            | AddPageData addPageDataBuildStep ->
                let path = ctx.ExpandPath addPageDataBuildStep.Path
                
                match File.Exists path with
                | true ->
                    ctx.Store.AddPageData(versionRef, File.ReadAllBytes path)
                | false ->
                    // TODO handle error?
                    ()

        let buildPage (site: string) (ctx: PipelineContext) (action: BuildPageAction) =
            let fn _ =
                let versionRef = createRef ()

                match ctx.Store.GetPage(site, action.Name) with
                | Some page ->
                    ctx.Store.AddPageVersion(page.Reference, versionRef, action.Template, false)

                    action.Steps
                    |> List.iter (buildPageStep versionRef ctx)
                    |> fun _ ->
                        match PageRenderer.run ctx.Store site action.Name with
                        | Ok p ->
                            File.WriteAllText(Path.Combine(ctx.ExpandPath "$root/rendered", "index.html"), p)
                            |> Ok
                        | Error e -> Error $"Error: {e}"
                | None -> Error $"Page `{action.Name}` not found for site `{site}`."

            attempt fn


    let run (ctx: PipelineContext) (cfg: PipelineConfiguration) =
        cfg.Steps
        |> List.fold
            (fun r s ->
                r
                |> Result.bind (fun _ ->
                    match s with
                    | StepType.CreateDirectories cda -> Actions.createDirectories ctx cda
                    | StepType.CopyResources cra -> Actions.copyResources ctx cra
                    | StepType.RunPluginScript rpsa -> Actions.runPluginScript cfg.Site ctx rpsa
                    | StepType.BuildPage bpa -> Actions.buildPage cfg.Site ctx bpa))
            (Ok())
