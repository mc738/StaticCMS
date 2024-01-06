namespace StaticCMS.App.Common

module Options =

    open System.Text.Json.Serialization
    open FsToolbox.AppEnvironment.Args.Mapping
    
    [<CLIMutable>]
    type GeneralSettings =
        { [<JsonPropertyName("paths")>]
          Paths: NamedPath seq }

    and [<CLIMutable>] NamedPath =
        { [<JsonPropertyName("name")>]
          Name: string
          [<JsonPropertyName("path")>]
          Path: string }

    type Options =
        | [<CommandValue("init-site")>] InitializeSite of InitializeSiteOptions
        | [<CommandValue("add-site")>] AddSite of AddSiteOptions
        | [<CommandValue("import-template")>] ImportTemplate of ImportTemplateOptions
        | [<CommandValue("render-site")>] RenderSite of RenderSiteOptions
        | [<CommandValue("add-page")>] AddPath of AddPageOptions
        | [<CommandValue("import-fragment-template")>] ImportFragmentTemplate of ImportFragmentTemplateOptions
        | [<CommandValue("add-site-plugin")>] AddSitePlugin
        | [<CommandValue("init")>] RunInit

    and InitializeSiteOptions =
        { [<ArgValue("-n", "--name")>]
          Name: string
          [<ArgValue("-u", "--url")>]
          Url: string
          [<ArgValue("-r", "--root")>]
          Root: string option
          [<ArgValue("-s", "--store")>]
          StorePath: string option }

    and AddSiteOptions =
        { [<ArgValue("-n", "--name")>]
          Name: string
          [<ArgValue("-u", "--url")>]
          Url: string
          [<ArgValue("-r", "--root")>]
          Root: string }

    and ImportTemplateOptions =
        { [<ArgValue("-n", "--name")>]
          Name: string
          [<ArgValue("-p", "--path")>]
          Path: string
          [<ArgValue("-s", "--store")>]
          StorePath: string option }

    and RenderSiteOptions =
        { [<ArgValue("-n", "--name")>]
          Name: string
          [<ArgValue("-s", "--store")>]
          StorePath: string option
          [<ArgValue("-c", "--config")>]
          ConfigurationPath: string option
          [<ArgValue("-d", "--draft")>]
          IsDraft: bool option }

    and AddPageOptions =
        { [<ArgValue("-n", "--name")>]
          Name: string
          [<ArgValue("-w", "--site")>]
          Site: string
          [<ArgValue("-c", "--config")>]
          ConfigurationPath: string option
          [<ArgValue("-s", "--store")>]
          StorePath: string option }

    and ImportFragmentTemplateOptions =
        { [<ArgValue("-n", "--name")>]
          Name: string
          [<ArgValue("-p", "--path")>]
          Path: string
          [<ArgValue("-s", "--store")>]
          StorePath: string option }

