namespace StaticCMS

open System
open System.IO
open System.Security.Cryptography
open System.Text
open Freql.Core.Common.Types
open Freql.Sqlite


module DataStore =

    open StaticCMS.Persistence

    module Internal =

        let newReference _ = Guid.NewGuid().ToString("n")

        let createTables (ctx: SqliteContext) =
            [ Records.Site.CreateTableSql()
              Records.PluginType.CreateTableSql()
              Records.ResourceType.CreateTableSql()
              Records.FragmentTemplate.CreateTableSql()
              Records.FragmentBlobType.CreateTableSql()
              Records.Template.CreateTableSql()
              Records.Plugin.CreateTableSql()
              Records.Page.CreateTableSql()
              Records.PageVersion.CreateTableSql()
              Records.RenderedPage.CreateTableSql()
              Records.Resource.CreateTableSql()
              Records.SitePlugin.CreateTableSql()
              Records.PageFragment.CreateTableSql()
              Records.PluginResources.CreateTableSql() ]
            |> List.map ctx.ExecuteSqlNonQuery
            |> ignore

        let seedData (ctx: SqliteContext) =
            [ ({ Name = "png" }: Parameters.NewResourceType)
              ({ Name = "jpg" }: Parameters.NewResourceType)
              ({ Name = "svg" }: Parameters.NewResourceType)
              ({ Name = "css" }: Parameters.NewResourceType)
              ({ Name = "js" }: Parameters.NewResourceType)
              ({ Name = "mustache" }: Parameters.NewResourceType) ]
            |> List.iter (Operations.insertResourceType ctx)

            [ ({ Name = "markdown" }: Parameters.NewFragmentBlobType)
              ({ Name = "json" }: Parameters.NewFragmentBlobType) ]
            |> List.iter (Operations.insertFragmentBlobType ctx)

        let initialize path =
            match File.Exists path with
            | true -> SqliteContext.Open path
            | false ->
                let ctx = SqliteContext.Create path

                createTables ctx
                seedData ctx

                ctx

        let addSite ctx name url rootPath =
            ({ Name = name
               Url = url
               RootPath = rootPath }: Parameters.NewSite)
            |> Operations.insertSite ctx

        let getSiteByName (ctx: SqliteContext) (name: string) =
            Operations.selectSiteRecord ctx [ "WHERE name = @0;" ] [ name ]

        let getAllSites (ctx: SqliteContext) = Operations.selectSiteRecords ctx [] []

        let addPage ctx reference site name nameSlug =
            ({ Reference = reference
               Site = site
               Name = name
               NameSlug = nameSlug }: Parameters.NewPage)
            |> Operations.insertPage ctx

        let getPageByName (ctx: SqliteContext) (site: string) (name: string) =
            Operations.selectPageRecord ctx [ "WHERE site = @0 AND name = @1;" ] [ site; name ]

        let getSitePages (ctx: SqliteContext) (site: string) =
            Operations.selectPageRecords ctx [ "WHERE site = @0;" ] [ site ]

        let addTemplate (ctx: SqliteContext) (name: string) (raw: BlobField) (hash: string) =
            ({ Name = name
               RawBlob = raw
               Hash = hash }: Parameters.NewTemplate)
            |> Operations.insertTemplate ctx

        let getTemplate (ctx: SqliteContext) (name: string) =
            Operations.selectTemplateRecord ctx [ "WHERE name = @0;" ] [ name ]

        let getLatestPageVersion (ctx: SqliteContext) (pageReference: string) =
            Operations.selectPageVersionRecord
                ctx
                [ "WHERE page_reference = @0"
                  "ORDER BY version DESC LIMIT 1" ]
                [ pageReference ]

        let addPageVersion
            (ctx: SqliteContext)
            (pageReference: string)
            (reference: string)
            (template: string)
            (isDraft: bool)
            =

            // Get latest version
            let version =
                getLatestPageVersion ctx pageReference
                |> Option.map (fun pv -> pv.Version + 1)
                |> Option.defaultValue 1

            ({ Reference = reference
               PageReference = pageReference
               Version = version
               IsDraft = isDraft
               Template = template
               CreatedOn = DateTime.UtcNow }: Parameters.NewPageVersion)
            |> Operations.insertPageVersion ctx

        let addPageFragment (ctx: SqliteContext) versionReference template dataName raw hash blobType =
            ({ VersionReference = versionReference
               Template = template
               DataName = dataName
               RawBlob = raw
               Hash = hash
               BlobType = blobType }: Parameters.NewPageFragment)
            |> Operations.insertPageFragment ctx

        let getPageFragments (ctx: SqliteContext) (versionReference: string) =
            Operations.selectPageFragmentRecords ctx [ "WHERE version_reference = @0;" ] [ versionReference ]

        let addFragmentTemplate (ctx: SqliteContext) (name: string) (template: BlobField) (hash: string) =
            ({ Name = name
               Template = template
               Hash = hash }: Parameters.NewFragmentTemplate)
            |> Operations.insertFragmentTemplate ctx

        let getFragmentTemplate (ctx: SqliteContext) (name: string) =
            Operations.selectFragmentTemplateRecord ctx [ "WHERE name = @0;" ] [ name ]

        let addPlugin (ctx: SqliteContext) (name: string) (pluginType: string) =
            ({ Name = name; PluginType = pluginType }: Parameters.NewPlugin)
            |> Operations.insertPlugin ctx

        let addSitePlugin (ctx: SqliteContext) (site: string) (plugin: string) (config: BlobField) =
            ({ Site = site
               Plugin = plugin
               Configuration = config }: Parameters.NewSitePlugin)
            |> Operations.insertSitePlugin ctx

        let getPluginResource (ctx: SqliteContext) (plugin: string) (name: string) =
            Operations.selectPluginResourcesRecord ctx [ "WHERE plugin = @0 AND name = @1" ] [ plugin; name ]

        let getSitePlugin (ctx: SqliteContext) (site: string) (plugin: string) =
            Operations.selectSitePluginRecord ctx [ "WHERE site = @0 AND plugin = @1" ] [ site; plugin ]
            
        let addPluginType (ctx: SqliteContext) (name: string) =
            ({ Name = name }: Parameters.NewPluginType )
            |> Operations.insertPluginType ctx
            

    type StaticStore(ctx: SqliteContext) =

        static member Create(path) = Internal.initialize path |> StaticStore

        member _.AddSite(name, url, rootPath) = Internal.addSite ctx name url rootPath

        member _.GetSite(name) = Internal.getSiteByName ctx name

        member _.AddPage(reference, site, name, nameSlug) =
            Internal.addPage ctx reference site name nameSlug

        member _.GetPage(site, name) = Internal.getPageByName ctx site name

        member _.AddTemplate(name: string, raw: byte array) =
            use ms = new MemoryStream(raw)
            let hash = hashStream (SHA256.Create()) ms
            let bf = BlobField.FromStream ms

            Internal.addTemplate ctx name bf hash

        //member _.AddFragmentTemplate

        member _.GetTemplate(name: string) = Internal.getTemplate ctx name
        //|> Option.map (fun t -> t.RawBlob.ToBytes() |> Encoding.UTF8.GetString)

        //Internal.gett

        member store.GetTemplateString(name: string) =
            store.GetTemplate name
            |> Option.map (fun t -> t.RawBlob.ToBytes() |> Encoding.UTF8.GetString)

        member _.GetPageByName(site: string, name: string) = Internal.getPageByName ctx site name

        member _.GetLatestPageVersion(pageReference: string) =
            Internal.getLatestPageVersion ctx pageReference

        member _.GetPageFragments(versionReference: string) =
            Internal.getPageFragments ctx versionReference

        member _.GetFragmentTemplate(name: string) = Internal.getFragmentTemplate ctx name

        //member store.GetFragmentTemplateString(name: string) =
        //    store.GetFragmentTemplate name
        //    |> Option.map (fun ft -> ft.Template.ToBytes() |> Encoding.UTF8.GetString)

        member _.AddPageVersion(pageReference, reference, template, isDraft) =

            Internal.addPageVersion ctx pageReference reference template isDraft

        member _.AddFragmentTemplate(name: string, raw: byte array) =
            use ms = new MemoryStream(raw)
            let hash = hashStream (SHA256.Create()) ms
            let bf = BlobField.FromStream ms

            Internal.addFragmentTemplate ctx name bf hash

        member _.AddPageFragment(versionReference, template, dataName, raw: byte array, blobType: string) =
            use ms = new MemoryStream(raw)
            let hash = hashStream (SHA256.Create()) ms
            let bf = BlobField.FromStream ms

            Internal.addPageFragment ctx versionReference template dataName bf hash blobType

        member _.AddPluginType(name) = Internal.addPluginType ctx name
        
        member _.AddPlugin(name, pluginType) = Internal.addPlugin ctx name pluginType

        member _.AddSitePlugin(site, plugin, configuration: string) =
            use ms =
                new MemoryStream(configuration |> Encoding.UTF8.GetBytes)

            //let hash = hashStream (SHA256.Create()) ms
            let bf = BlobField.FromStream ms

            Internal.addSitePlugin ctx site plugin bf

        member _.GetSitePluginConfiguration(site, plugin) =
            Internal.getSitePlugin ctx site plugin
            |> Option.map (fun sp ->
                sp.Configuration.ToBytes()
                |> Encoding.UTF8.GetString)


        member _.GetPluginResource(plugin, name) =
            Internal.getPluginResource ctx plugin name
            |> Option.map (fun pr -> pr.RawBlob.ToBytes())



    type StaticStoreReader(ctx: SqliteContext) =

        static member Open(path: string) =
            SqliteContext.Open path |> StaticStoreReader

        member _.GetSitePluginConfiguration(site, plugin) =
            Internal.getSitePlugin ctx site plugin
            |> Option.map (fun sp ->
                sp.Configuration.ToBytes()
                |> Encoding.UTF8.GetString)

        member _.GetPluginResource(plugin, name) =
            Internal.getPluginResource ctx plugin name
            |> Option.map (fun pr -> pr.RawBlob.ToBytes())