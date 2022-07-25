namespace StaticCMS

open System
open System.IO
open System.Security.Cryptography
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
              Records.PageFragment.CreateTableSql() ]
            |> List.map ctx.ExecuteSqlNonQuery
            |> ignore

        let seedData (ctx: SqliteContext) =
            [ ({ Name = "png" }: Parameters.NewResourceType)
              ({ Name = "jpg" }: Parameters.NewResourceType)
              ({ Name = "svg" }: Parameters.NewResourceType)
              ({ Name = "css" }: Parameters.NewResourceType)
              ({ Name = "js" }: Parameters.NewResourceType) ]
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

        let getLatestPageVersion (ctx: SqliteContext) (pageReference: string) =
            Operations.selectPageVersionRecord
                ctx
                [ "WHERE page_reference = @0"
                  "ORDER BY version DESC LIMIT 1" ]
                [ pageReference ]

        let addPageVersion
            (ctx: SqliteContext)
            (pageReference: string)
            (template: string)
            (isDraft: bool)
            (raw: BlobField)
            =
            let reference = newReference ()

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
               RawBlob = raw
               CreatedOn = DateTime.UtcNow }: Parameters.NewPageVersion)
            |> Operations.insertPageVersion ctx

            reference
            
        let getPageFragments (ctx: SqliteContext) (versionReference: string) =
            Operations.selectPageFragmentRecords ctx [ "WHERE version_reference = @0;" ] [ versionReference ]
            
        let getFragmentTemplate (ctx: SqliteContext) (name: string) =
            Operations.selectFragmentTemplateRecord ctx [ "WHERE name = @0;" ] [ name ]

    type StaticStore(ctx: SqliteContext) =

        static member Create(path) = Internal.initialize path |> StaticStore

        member _.AddTemplate(name: string, raw: byte array) =
            use ms = new MemoryStream(raw)
            let hash = hashStream (SHA256.Create()) ms
            let bf = BlobField.FromStream ms

            Internal.addTemplate ctx name bf hash

        member _.GetPageByName(site: string, name: string) = Internal.getPageByName ctx site name

        member _.GetLatestPageVersion(pageReference: string) =
            Internal.getLatestPageVersion ctx pageReference
        
        member _.GetPageFragments(versionReference: string) =
            Internal.getPageFragments ctx versionReference
            
        member _.GetFragmentTemplate(name: string) =
            Internal.getFragmentTemplate ctx name
        
        member _.AddPageVersion() =

            ()
