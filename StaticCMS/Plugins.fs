namespace StaticCMS

open System
open System.IO
open System.Text.Json
open Fluff.Core
open FsToolbox.Core
open Microsoft.FSharp.Core

[<RequireQualifiedAccess>]
module Plugins =

    type Settings =

        { Initialization: PluginInitialize }

        static member TryFromJson(element: JsonElement) =
            match Json.tryGetProperty "initialize" element with
            | Some el ->
                PluginInitialize.TryFromJson el
                |> Result.map (fun pi -> { Initialization = pi })
            | None -> Error "Missing `initialize` property"

        static member Load(path: string) =
            try
                match File.Exists path with
                | true ->
                    (File.ReadAllText path |> JsonDocument.Parse).RootElement
                    |> Settings.TryFromJson
                | false -> Error $"File `{path}` does not exist"
            with ex ->
                Error $"Unhandled exception while loading plugin settings. Error: {ex.Message}"

    and PluginInitialize =
        { Args: InitializeArg list
          Steps: InitializeStep list }

        static member TryFromJson(element: JsonElement) =
            match Json.tryGetArrayProperty "steps" element with
            | Some ap ->
                ap
                |> List.map InitializeStep.TryFromJson
                |> List.fold
                    (fun (acc, errors) r ->
                        match r with
                        | Ok v -> acc @ [ v ], errors
                        | Error e -> acc, errors @ [ e ])
                    ([], [])
                |> fun (s, e) ->
                    match e.IsEmpty with
                    | true -> Ok s
                    | false ->
                        Error(
                            [ "Failed to deserialize the plugin settings. Errors:"; yield! e ]
                            |> String.concat Environment.NewLine
                        )
                |> Result.map (fun s ->
                    { Args =
                        Json.tryGetArrayProperty "args" element
                        |> Option.map (fun ap -> ap |> List.choose InitializeArg.TryOptionFromJson)
                        |> Option.defaultValue []
                      Steps = s })
            | None -> Ok { Args = []; Steps = [] }

    and InitializeArg =
        { Name: string
          DefaultValue: string option
          PromptHint: string option }

        static member TryOptionFromJson(element: JsonElement) =
            match Json.tryGetStringProperty "name" element with
            | Some name ->
                Some
                    { Name = name
                      DefaultValue = None
                      PromptHint = None }
            | None -> None


    and [<RequireQualifiedAccess>] InitializeStep =
        | CreateDirectory of CreateDirectoryInitializeStep
        | CopyFile of CopyFileInitializeStep
        | CopyDirectory of CopyDirectoryInitializeStep
        | CreateFileFromTemplate of CreateFileFromTemplateInitializeStep
        | AddBuildStep of AddBuildStepInitializeStep
        | ExtractArchive of ExtractArchiveInitializeStep

        static member TryFromJson(element: JsonElement) =
            match Json.tryGetStringProperty "type" element with
            | Some "create-directory" ->
                CreateDirectoryInitializeStep.TryFromJson element
                |> Result.map InitializeStep.CreateDirectory
            | Some "copy-file" -> CopyFileInitializeStep.TryFromJson element |> Result.map InitializeStep.CopyFile
            | Some "copy-directory" ->
                CopyDirectoryInitializeStep.TryFromJson element
                |> Result.map InitializeStep.CopyDirectory
            | Some "create-file-from-template" ->
                CreateFileFromTemplateInitializeStep.TryFromJson element
                |> Result.map InitializeStep.CreateFileFromTemplate
            | Some "add-build-step" ->
                AddBuildStepInitializeStep.TryFromJson element
                |> Result.map InitializeStep.AddBuildStep
            | Some "extract-archive" ->
                ExtractArchiveInitializeStep.TryFromJson element
                |> Result.map InitializeStep.ExtractArchive
            | Some t -> Error $"Unknown step type `{t}`"
            | None -> Error "Missing `type` property"

    and CreateDirectoryInitializeStep =
        { Path: string }

        static member TryFromJson(element: JsonElement) =
            match Json.tryGetStringProperty "path" element with
            | Some p -> Ok { Path = p }
            | None -> Error "Missing `path` property"

    and CopyFileInitializeStep =
        { Path: string
          OutputPath: string }

        static member TryFromJson(element: JsonElement) =
            match Json.tryGetStringProperty "path" element, Json.tryGetStringProperty "outputPath" element with
            | Some p, Some o -> Ok { Path = p; OutputPath = o }
            | None, _ -> Error "Missing `path` property"
            | _, None -> Error "Missing `outputPath` property"

    and CopyDirectoryInitializeStep =
        { Path: string
          OutputPath: string
          Recursive: bool }

        static member TryFromJson(element: JsonElement) =
            match Json.tryGetStringProperty "path" element, Json.tryGetStringProperty "outputPath" element with
            | Some p, Some o ->
                Ok
                    { Path = p
                      OutputPath = o
                      Recursive = Json.tryGetBoolProperty "recursive" element |> Option.defaultValue false }
            | None, _ -> Error "Missing `path` property"
            | _, None -> Error "Missing `outputPath` property"

    and CreateFileFromTemplateInitializeStep =
        { Path: string
          OutputPath: string
          EncodingType: TemplateDataEncodingType
          Data: Map<string, string> }

        static member TryFromJson(element: JsonElement) =
            match
                Json.tryGetStringProperty "path" element,
                Json.tryGetStringProperty "outputPath" element,
                Json.tryGetElementsProperty "data" element
            with
            | Some p, Some o, Some d ->
                { Path = p
                  OutputPath = o
                  EncodingType =
                    Json.tryGetStringProperty "encodingType" element
                    |> Option.map TemplateDataEncodingType.FromString
                    |> Option.defaultValue TemplateDataEncodingType.None
                  Data =
                    d
                    |> List.choose (fun p ->
                        match p.Value.ValueKind with
                        | JsonValueKind.String -> p.Value.GetString() |> Some
                        | _ -> None
                        |> Option.map (fun v -> p.Name, v))
                    |> Map.ofList }
                |> Ok
            | None, _, _ -> Error "Missing `path` property"
            | _, None, _ -> Error "Missing `outputPath` property"
            | _, _, None -> Error "Missing `data` property"

    and [<RequireQualifiedAccess>] TemplateDataEncodingType =
        | Json
        | None

        static member FromString(str: string) =
            match str.ToLower() with
            | "json" -> TemplateDataEncodingType.Json
            | "none" -> TemplateDataEncodingType.None
            | _ -> TemplateDataEncodingType.None

    and AddBuildStepInitializeStep =
        { Name: string
          Script: string
          Function: string
          ReturnType: Pipeline.PluginReturnType }

        static member TryFromJson(json: JsonElement) =
            match
                Json.tryGetStringProperty "name" json,
                Json.tryGetStringProperty "script" json,
                Json.tryGetStringProperty "function" json,
                Json.tryGetProperty "returnType" json
                |> Option.map Pipeline.PluginReturnType.TryDeserialize
                |> Option.defaultValue (Error "Missing `returnType` property")
            with
            | Some n, Some s, Some f, Ok rt ->
                { Name = n
                  Script = s
                  Function = f
                  ReturnType = rt }
                |> Ok
            | None, _, _, _ -> Error "Missing `name` property"
            | _, None, _, _ -> Error "Missing `script` property"
            | _, _, None, _ -> Error "Missing `function` property"
            | _, _, _, Error e -> Error e

    and ExtractArchiveInitializeStep =
        { ArchivePath: string
          OutputPath: string
          CompressionType: ArchiveCompressionType }

        static member TryFromJson(json: JsonElement) =
            match
                Json.tryGetStringProperty "archivePath" json,
                Json.tryGetStringProperty "outputPath" json,
                Json.tryGetStringProperty "compressionType" json
                |> Option.bind ArchiveCompressionType.TryFromString
            with
            | Some p, Some o, Some a ->
                { ArchivePath = p
                  OutputPath = o
                  CompressionType = a }
                |> Ok
            | None, _, _ -> Error "Missing `archivePath` property"
            | _, None, _ -> Error "Missing `outputPath` property"
            | _, _, None -> Error "Missing or invalid `compressionType` property"

    and [<RequireQualifiedAccess>] ArchiveCompressionType =
        | Zip

        static member TryFromString(str: string) =
            match str.ToLower() with
            | "zip" -> Some ArchiveCompressionType.Zip
            | _ -> None


    type InitializePluginParameters =
        { Steps: InitializeStep list
          Args: (string * string) list
          SiteName: string
          SiteDisplayName: string
          StaticCMSRoot: string
          SiteRoot: string }

    let createKnownPaths (staticCMSRoot: string) (siteRoot: string) =
        [ "$root", siteRoot
          "$static_cms_root", staticCMSRoot
          "$plugins", Path.Combine(staticCMSRoot, "plugins")
          "$site_plugins", Path.Combine(siteRoot, "plugins") ]
        |> Map.ofList


    let initializePlugin (parameters: InitializePluginParameters) =
        // First find the init.json file
        let knownPaths = createKnownPaths parameters.StaticCMSRoot parameters.SiteRoot

        let args =
            [ "site_name", parameters.SiteName
              "site_display_name", parameters.SiteDisplayName
              yield! parameters.Args ]
            |> Map.ofList

        let replacementHandler (str: string) =
            replaceArgs args str |> expandPath knownPaths

        parameters.Steps
        |> List.fold
            (fun r s ->
                r
                |> Result.bind (fun _ ->
                    match s with
                    | InitializeStep.CreateDirectory createDirectoryInitializeStep ->
                        try
                            Directory.CreateDirectory(replacementHandler createDirectoryInitializeStep.Path)
                            |> ignore
                            |> Ok
                        with ex ->
                            Error $"Unhandled exception while creating directory. Error: {ex.Message}"
                    | InitializeStep.CopyFile copyFileInitializeStep ->
                        try
                            File.Copy(
                                replacementHandler copyFileInitializeStep.Path,
                                replacementHandler copyFileInitializeStep.OutputPath
                            )
                            |> Ok
                        with ex ->
                            Error $"Unhandled exception while copying file. Error: {ex.Message}"
                    | InitializeStep.CopyDirectory copyDirectoryInitializeStep ->
                        try
                            let output = replacementHandler copyDirectoryInitializeStep.OutputPath
                            let path = replacementHandler copyDirectoryInitializeStep.Path

                            match copyDirectoryInitializeStep.Recursive with
                            | true ->
                                let rec handle (source: DirectoryInfo) (target: DirectoryInfo) =
                                    //target.CreateSubdirectory(source.Name) |> ignore

                                    source.GetFiles()
                                    |> Seq.iter (fun fi ->
                                        File.Copy(fi.FullName, Path.Combine(target.FullName, fi.Name)))

                                    source.GetDirectories()
                                    |> Seq.iter (fun di -> handle di (target.CreateSubdirectory(di.Name)))

                                handle (DirectoryInfo(path)) (DirectoryInfo(output)) |> Ok
                            | false ->
                                Directory.EnumerateFiles(path)
                                |> Seq.iter (fun f -> File.Copy(f, Path.Combine(output, Path.GetFileName(f))))
                                |> Ok
                        with ex ->
                            Error $"Unhandled exception while copying directory. Error: {ex.Message}"
                    | InitializeStep.CreateFileFromTemplate fileFromTemplateInitializeStep ->
                        try
                            let template =
                                File.ReadAllText(replacementHandler fileFromTemplateInitializeStep.Path)
                                |> Mustache.parse

                            { Mustache.Data.Empty() with
                                Values =
                                    fileFromTemplateInitializeStep.Data
                                    |> Map.map (fun _ v ->
                                        let value = replacementHandler v

                                        match fileFromTemplateInitializeStep.EncodingType with
                                        | TemplateDataEncodingType.Json -> JsonEncodedText.Encode value |> string
                                        | TemplateDataEncodingType.None -> value
                                        |> Mustache.Value.Scalar) }
                            |> fun d -> Mustache.replace d false template
                            |> fun f ->
                                File.WriteAllText(replacementHandler fileFromTemplateInitializeStep.OutputPath, f)
                            |> Ok
                        with ex ->
                            Error $"Unhandled exception while creating file from template. Error: {ex.Message}"
                    | InitializeStep.AddBuildStep addBuildStepInitializeStep ->
                        let buildCfgPath = Path.Combine(parameters.SiteRoot, "build.json")

                        match Pipeline.loadConfiguration buildCfgPath with
                        | Ok buildCfg ->
                            try
                                let buildStep =
                                    ({ Name = addBuildStepInitializeStep.Name
                                       Script = replacementHandler addBuildStepInitializeStep.Script
                                       Function = addBuildStepInitializeStep.Function
                                       ReturnType = addBuildStepInitializeStep.ReturnType }
                                    : Pipeline.RunPluginScriptAction)

                                { buildCfg with
                                    Steps =
                                        buildCfg.Steps @ [ Pipeline.StepType.RunPluginScript buildStep ]
                                        |> List.map (fun s -> s.GetDefaultOrder(), s)
                                        |> List.sortBy fst
                                        |> List.map snd }
                                    .Serialize()
                                |> fun nbc -> File.WriteAllText(buildCfgPath, nbc) |> Ok
                            with ex ->
                                Error ""
                        | Error es ->
                            [ "Failed to deserialize build configuration. Error(s):"; yield! es ]
                            |> String.concat Environment.NewLine
                            |> Error
                    | InitializeStep.ExtractArchive extractArchiveInitializeStep ->
                        try
                            match extractArchiveInitializeStep.CompressionType with
                            | ArchiveCompressionType.Zip ->
                                Compression.unzip
                                    (replacementHandler extractArchiveInitializeStep.ArchivePath)
                                    (replacementHandler extractArchiveInitializeStep.OutputPath)
                                |> Ok
                        with ex ->
                            Error $"Unhandled exception while extracting archive. Error: {ex.Message}"))
            (Ok())
