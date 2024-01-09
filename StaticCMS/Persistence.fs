namespace StaticCMS.Persistence

open System
open System.Text.Json.Serialization
open Freql.Core.Common
open Freql.Sqlite

/// Module generated on 08/01/2024 18:34:58 (utc) via Freql.Tools.
[<RequireQualifiedAccess>]
module Records =
    /// A record representing a row in the table `fragment_blob_type`.
    type FragmentBlobType =
        { [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Name = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE fragment_blob_type (
	name TEXT NOT NULL,
	CONSTRAINT fragment_blob_type_PK PRIMARY KEY (name)
)
        """
    
        static member SelectSql() = """
        SELECT
              fragment_blob_type.`name`
        FROM fragment_blob_type
        """
    
        static member TableName() = "fragment_blob_type"
    
    /// A record representing a row in the table `fragment_templates`.
    type FragmentTemplate =
        { [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("template")>] Template: BlobField
          [<JsonPropertyName("hash")>] Hash: string }
    
        static member Blank() =
            { Name = String.Empty
              Template = BlobField.Empty()
              Hash = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE fragment_templates (
	name TEXT NOT NULL,
	template BLOB NOT NULL, hash TEXT NOT NULL,
	CONSTRAINT fragment_templates_PK PRIMARY KEY (name)
)
        """
    
        static member SelectSql() = """
        SELECT
              fragment_templates.`name`,
              fragment_templates.`template`,
              fragment_templates.`hash`
        FROM fragment_templates
        """
    
        static member TableName() = "fragment_templates"
    
    /// A record representing a row in the table `page_data`.
    type PageData =
        { [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("versionReference")>] VersionReference: string
          [<JsonPropertyName("rawBlob")>] RawBlob: BlobField
          [<JsonPropertyName("hash")>] Hash: string }
    
        static member Blank() =
            { Reference = String.Empty
              VersionReference = String.Empty
              RawBlob = BlobField.Empty()
              Hash = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE page_data (
	reference TEXT NOT NULL,
	version_reference TEXT NOT NULL,
	raw_blob BLOB NOT NULL,
	hash TEXT NOT NULL,
	CONSTRAINT NewTable_PK PRIMARY KEY (reference),
	CONSTRAINT page_data_FK FOREIGN KEY (version_reference) REFERENCES page_versions(reference)
)
        """
    
        static member SelectSql() = """
        SELECT
              page_data.`reference`,
              page_data.`version_reference`,
              page_data.`raw_blob`,
              page_data.`hash`
        FROM page_data
        """
    
        static member TableName() = "page_data"
    
    /// A record representing a row in the table `page_fragments`.
    type PageFragment =
        { [<JsonPropertyName("versionReference")>] VersionReference: string
          [<JsonPropertyName("template")>] Template: string
          [<JsonPropertyName("dataName")>] DataName: string
          [<JsonPropertyName("rawBlob")>] RawBlob: BlobField
          [<JsonPropertyName("hash")>] Hash: string
          [<JsonPropertyName("blobType")>] BlobType: string }
    
        static member Blank() =
            { VersionReference = String.Empty
              Template = String.Empty
              DataName = String.Empty
              RawBlob = BlobField.Empty()
              Hash = String.Empty
              BlobType = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE page_fragments (
	version_reference TEXT NOT NULL,
	template TEXT NOT NULL,
	data_name TEXT NOT NULL,
	raw_blob BLOB NOT NULL,
	hash TEXT NOT NULL,
	blob_type TEXT NOT NULL,
	CONSTRAINT page_fragments_PK PRIMARY KEY (version_reference,data_name),
	CONSTRAINT page_fragments_FK FOREIGN KEY (version_reference) REFERENCES page_versions(reference),
	CONSTRAINT page_fragments_FK_1 FOREIGN KEY (template) REFERENCES fragment_templates(name),
	CONSTRAINT page_fragments_FK_2 FOREIGN KEY (blob_type) REFERENCES fragment_blob_type(name)
)
        """
    
        static member SelectSql() = """
        SELECT
              page_fragments.`version_reference`,
              page_fragments.`template`,
              page_fragments.`data_name`,
              page_fragments.`raw_blob`,
              page_fragments.`hash`,
              page_fragments.`blob_type`
        FROM page_fragments
        """
    
        static member TableName() = "page_fragments"
    
    /// A record representing a row in the table `page_versions`.
    type PageVersion =
        { [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("pageReference")>] PageReference: string
          [<JsonPropertyName("version")>] Version: int
          [<JsonPropertyName("isDraft")>] IsDraft: bool
          [<JsonPropertyName("template")>] Template: string
          [<JsonPropertyName("createdOn")>] CreatedOn: DateTime }
    
        static member Blank() =
            { Reference = String.Empty
              PageReference = String.Empty
              Version = 0
              IsDraft = true
              Template = String.Empty
              CreatedOn = DateTime.UtcNow }
    
        static member CreateTableSql() = """
        CREATE TABLE page_versions (
	reference TEXT NOT NULL,
	page_reference TEXT NOT NULL,
	version INTEGER NOT NULL,
	is_draft INTEGER NOT NULL,
	template TEXT NOT NULL,
	created_on TEXT NOT NULL,
	CONSTRAINT page_versions_PK PRIMARY KEY (reference),
	CONSTRAINT page_versions_FK FOREIGN KEY (page_reference) REFERENCES pages(reference),
	CONSTRAINT page_versions_FK_1 FOREIGN KEY (template) REFERENCES templates(name)
)
        """
    
        static member SelectSql() = """
        SELECT
              page_versions.`reference`,
              page_versions.`page_reference`,
              page_versions.`version`,
              page_versions.`is_draft`,
              page_versions.`template`,
              page_versions.`created_on`
        FROM page_versions
        """
    
        static member TableName() = "page_versions"
    
    /// A record representing a row in the table `pages`.
    type Page =
        { [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("site")>] Site: string
          [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("nameSlug")>] NameSlug: string }
    
        static member Blank() =
            { Reference = String.Empty
              Site = String.Empty
              Name = String.Empty
              NameSlug = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE pages (
	reference TEXT NOT NULL,
	site TEXT NOT NULL,
	name TEXT NOT NULL,
	name_slug TEXT NOT NULL,
	CONSTRAINT pages_PK PRIMARY KEY (reference),
	CONSTRAINT pages_UN UNIQUE (site,name),
	CONSTRAINT pages_FK FOREIGN KEY (site) REFERENCES sites(name)
)
        """
    
        static member SelectSql() = """
        SELECT
              pages.`reference`,
              pages.`site`,
              pages.`name`,
              pages.`name_slug`
        FROM pages
        """
    
        static member TableName() = "pages"
    
    /// A record representing a row in the table `plugin_resources`.
    type PluginResources =
        { [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("plugin")>] Plugin: string
          [<JsonPropertyName("rawBlob")>] RawBlob: BlobField
          [<JsonPropertyName("hash")>] Hash: string
          [<JsonPropertyName("resourceType")>] ResourceType: string }
    
        static member Blank() =
            { Name = String.Empty
              Plugin = String.Empty
              RawBlob = BlobField.Empty()
              Hash = String.Empty
              ResourceType = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE plugin_resources (
	name TEXT NOT NULL,
	plugin TEXT NOT NULL,
	raw_blob BLOB NOT NULL,
	hash TEXT NOT NULL,
	resource_type TEXT NOT NULL,
	CONSTRAINT plugin_resources_PK PRIMARY KEY (name,plugin),
	CONSTRAINT plugin_resources_FK FOREIGN KEY (plugin) REFERENCES plugins(name),
	CONSTRAINT plugin_resources_FK_1 FOREIGN KEY (resource_type) REFERENCES resource_types(name)
)
        """
    
        static member SelectSql() = """
        SELECT
              plugin_resources.`name`,
              plugin_resources.`plugin`,
              plugin_resources.`raw_blob`,
              plugin_resources.`hash`,
              plugin_resources.`resource_type`
        FROM plugin_resources
        """
    
        static member TableName() = "plugin_resources"
    
    /// A record representing a row in the table `plugin_types`.
    type PluginType =
        { [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Name = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE "plugin_types" (
	name TEXT NOT NULL,
	CONSTRAINT plugin_type_PK PRIMARY KEY (name)
)
        """
    
        static member SelectSql() = """
        SELECT
              plugin_types.`name`
        FROM plugin_types
        """
    
        static member TableName() = "plugin_types"
    
    /// A record representing a row in the table `plugins`.
    type Plugin =
        { [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("pluginType")>] PluginType: string }
    
        static member Blank() =
            { Name = String.Empty
              PluginType = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE plugins (
	name TEXT NOT NULL,
	plugin_type TEXT NOT NULL,
	CONSTRAINT plugins_PK PRIMARY KEY (name),
	CONSTRAINT plugins_FK FOREIGN KEY (plugin_type) REFERENCES plugin_types(name)
)
        """
    
        static member SelectSql() = """
        SELECT
              plugins.`name`,
              plugins.`plugin_type`
        FROM plugins
        """
    
        static member TableName() = "plugins"
    
    /// A record representing a row in the table `rendered_pages`.
    type RenderedPage =
        { [<JsonPropertyName("versionReference")>] VersionReference: string
          [<JsonPropertyName("rawBlob")>] RawBlob: string
          [<JsonPropertyName("hash")>] Hash: string
          [<JsonPropertyName("renderedOn")>] RenderedOn: DateTime
          [<JsonPropertyName("virtualPath")>] VirtualPath: string
          [<JsonPropertyName("fileName")>] FileName: string }
    
        static member Blank() =
            { VersionReference = String.Empty
              RawBlob = String.Empty
              Hash = String.Empty
              RenderedOn = DateTime.UtcNow
              VirtualPath = String.Empty
              FileName = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE rendered_pages (
	version_reference TEXT NOT NULL,
	raw_blob TEXT NOT NULL,
	hash TEXT NOT NULL,
	rendered_on TEXT NOT NULL, virtual_path TEXT NOT NULL, file_name TEXT NOT NULL,
	CONSTRAINT rendered_pages_PK PRIMARY KEY (version_reference),
	CONSTRAINT rendered_pages_FK FOREIGN KEY (version_reference) REFERENCES page_versions(reference)
)
        """
    
        static member SelectSql() = """
        SELECT
              rendered_pages.`version_reference`,
              rendered_pages.`raw_blob`,
              rendered_pages.`hash`,
              rendered_pages.`rendered_on`,
              rendered_pages.`virtual_path`,
              rendered_pages.`file_name`
        FROM rendered_pages
        """
    
        static member TableName() = "rendered_pages"
    
    /// A record representing a row in the table `resource_types`.
    type ResourceType =
        { [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Name = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE resource_types (
	name TEXT NOT NULL,
	CONSTRAINT resource_types_PK PRIMARY KEY (name)
)
        """
    
        static member SelectSql() = """
        SELECT
              resource_types.`name`
        FROM resource_types
        """
    
        static member TableName() = "resource_types"
    
    /// A record representing a row in the table `resources`.
    type Resource =
        { [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("site")>] Site: string
          [<JsonPropertyName("resourceType")>] ResourceType: string
          [<JsonPropertyName("rawBlob")>] RawBlob: BlobField
          [<JsonPropertyName("hash")>] Hash: string
          [<JsonPropertyName("virtualPath")>] VirtualPath: string
          [<JsonPropertyName("fileName")>] FileName: string }
    
        static member Blank() =
            { Name = String.Empty
              Site = String.Empty
              ResourceType = String.Empty
              RawBlob = BlobField.Empty()
              Hash = String.Empty
              VirtualPath = String.Empty
              FileName = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE resources (
	name TEXT NOT NULL,
	site TEXT NOT NULL,
	resource_type TEXT NOT NULL,
	raw_blob BLOB NOT NULL,
	hash TEXT NOT NULL, virtual_path TEXT NOT NULL, file_name TEXT NOT NULL,
	CONSTRAINT resources_PK PRIMARY KEY (name,site),
	CONSTRAINT resources_FK FOREIGN KEY (site) REFERENCES sites(name),
	CONSTRAINT resources_FK_1 FOREIGN KEY (resource_type) REFERENCES resource_types(name)
)
        """
    
        static member SelectSql() = """
        SELECT
              resources.`name`,
              resources.`site`,
              resources.`resource_type`,
              resources.`raw_blob`,
              resources.`hash`,
              resources.`virtual_path`,
              resources.`file_name`
        FROM resources
        """
    
        static member TableName() = "resources"
    
    /// A record representing a row in the table `site_plugins`.
    type SitePlugin =
        { [<JsonPropertyName("site")>] Site: string
          [<JsonPropertyName("plugin")>] Plugin: string
          [<JsonPropertyName("configuration")>] Configuration: BlobField }
    
        static member Blank() =
            { Site = String.Empty
              Plugin = String.Empty
              Configuration = BlobField.Empty() }
    
        static member CreateTableSql() = """
        CREATE TABLE site_plugins (
	site TEXT NOT NULL,
	plugin TEXT NOT NULL,
	configuration BLOB NOT NULL,
	CONSTRAINT site_plugins_PK PRIMARY KEY (site,plugin),
	CONSTRAINT site_plugins_FK FOREIGN KEY (site) REFERENCES sites(name),
	CONSTRAINT site_plugins_FK_1 FOREIGN KEY (plugin) REFERENCES plugins(name)
)
        """
    
        static member SelectSql() = """
        SELECT
              site_plugins.`site`,
              site_plugins.`plugin`,
              site_plugins.`configuration`
        FROM site_plugins
        """
    
        static member TableName() = "site_plugins"
    
    /// A record representing a row in the table `sites`.
    type Site =
        { [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("rootPath")>] RootPath: string
          [<JsonPropertyName("url")>] Url: string
          [<JsonPropertyName("displayName")>] DisplayName: string option }
    
        static member Blank() =
            { Name = String.Empty
              RootPath = String.Empty
              Url = String.Empty
              DisplayName = None }
    
        static member CreateTableSql() = """
        CREATE TABLE sites (
	name TEXT NOT NULL,
	root_path TEXT NOT NULL,
	url TEXT NOT NULL, display_name TEXT,
	CONSTRAINT sites_PK PRIMARY KEY (name)
)
        """
    
        static member SelectSql() = """
        SELECT
              sites.`name`,
              sites.`root_path`,
              sites.`url`,
              sites.`display_name`
        FROM sites
        """
    
        static member TableName() = "sites"
    
    /// A record representing a row in the table `templates`.
    type Template =
        { [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("rawBlob")>] RawBlob: BlobField
          [<JsonPropertyName("hash")>] Hash: string }
    
        static member Blank() =
            { Name = String.Empty
              RawBlob = BlobField.Empty()
              Hash = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE templates (
	name TEXT NOT NULL,
	raw_blob BLOB NOT NULL,
	hash TEXT NOT NULL,
	CONSTRAINT templates_PK PRIMARY KEY (name)
)
        """
    
        static member SelectSql() = """
        SELECT
              templates.`name`,
              templates.`raw_blob`,
              templates.`hash`
        FROM templates
        """
    
        static member TableName() = "templates"
    

/// Module generated on 08/01/2024 18:34:58 (utc) via Freql.Tools.
[<RequireQualifiedAccess>]
module Parameters =
    /// A record representing a new row in the table `fragment_blob_type`.
    type NewFragmentBlobType =
        { [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Name = String.Empty }
    
    
    /// A record representing a new row in the table `fragment_templates`.
    type NewFragmentTemplate =
        { [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("template")>] Template: BlobField
          [<JsonPropertyName("hash")>] Hash: string }
    
        static member Blank() =
            { Name = String.Empty
              Template = BlobField.Empty()
              Hash = String.Empty }
    
    
    /// A record representing a new row in the table `page_data`.
    type NewPageData =
        { [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("versionReference")>] VersionReference: string
          [<JsonPropertyName("rawBlob")>] RawBlob: BlobField
          [<JsonPropertyName("hash")>] Hash: string }
    
        static member Blank() =
            { Reference = String.Empty
              VersionReference = String.Empty
              RawBlob = BlobField.Empty()
              Hash = String.Empty }
    
    
    /// A record representing a new row in the table `page_fragments`.
    type NewPageFragment =
        { [<JsonPropertyName("versionReference")>] VersionReference: string
          [<JsonPropertyName("template")>] Template: string
          [<JsonPropertyName("dataName")>] DataName: string
          [<JsonPropertyName("rawBlob")>] RawBlob: BlobField
          [<JsonPropertyName("hash")>] Hash: string
          [<JsonPropertyName("blobType")>] BlobType: string }
    
        static member Blank() =
            { VersionReference = String.Empty
              Template = String.Empty
              DataName = String.Empty
              RawBlob = BlobField.Empty()
              Hash = String.Empty
              BlobType = String.Empty }
    
    
    /// A record representing a new row in the table `page_versions`.
    type NewPageVersion =
        { [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("pageReference")>] PageReference: string
          [<JsonPropertyName("version")>] Version: int
          [<JsonPropertyName("isDraft")>] IsDraft: bool
          [<JsonPropertyName("template")>] Template: string
          [<JsonPropertyName("createdOn")>] CreatedOn: DateTime }
    
        static member Blank() =
            { Reference = String.Empty
              PageReference = String.Empty
              Version = 0
              IsDraft = true
              Template = String.Empty
              CreatedOn = DateTime.UtcNow }
    
    
    /// A record representing a new row in the table `pages`.
    type NewPage =
        { [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("site")>] Site: string
          [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("nameSlug")>] NameSlug: string }
    
        static member Blank() =
            { Reference = String.Empty
              Site = String.Empty
              Name = String.Empty
              NameSlug = String.Empty }
    
    
    /// A record representing a new row in the table `plugin_resources`.
    type NewPluginResources =
        { [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("plugin")>] Plugin: string
          [<JsonPropertyName("rawBlob")>] RawBlob: BlobField
          [<JsonPropertyName("hash")>] Hash: string
          [<JsonPropertyName("resourceType")>] ResourceType: string }
    
        static member Blank() =
            { Name = String.Empty
              Plugin = String.Empty
              RawBlob = BlobField.Empty()
              Hash = String.Empty
              ResourceType = String.Empty }
    
    
    /// A record representing a new row in the table `plugin_types`.
    type NewPluginType =
        { [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Name = String.Empty }
    
    
    /// A record representing a new row in the table `plugins`.
    type NewPlugin =
        { [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("pluginType")>] PluginType: string }
    
        static member Blank() =
            { Name = String.Empty
              PluginType = String.Empty }
    
    
    /// A record representing a new row in the table `rendered_pages`.
    type NewRenderedPage =
        { [<JsonPropertyName("versionReference")>] VersionReference: string
          [<JsonPropertyName("rawBlob")>] RawBlob: string
          [<JsonPropertyName("hash")>] Hash: string
          [<JsonPropertyName("renderedOn")>] RenderedOn: DateTime
          [<JsonPropertyName("virtualPath")>] VirtualPath: string
          [<JsonPropertyName("fileName")>] FileName: string }
    
        static member Blank() =
            { VersionReference = String.Empty
              RawBlob = String.Empty
              Hash = String.Empty
              RenderedOn = DateTime.UtcNow
              VirtualPath = String.Empty
              FileName = String.Empty }
    
    
    /// A record representing a new row in the table `resource_types`.
    type NewResourceType =
        { [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Name = String.Empty }
    
    
    /// A record representing a new row in the table `resources`.
    type NewResource =
        { [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("site")>] Site: string
          [<JsonPropertyName("resourceType")>] ResourceType: string
          [<JsonPropertyName("rawBlob")>] RawBlob: BlobField
          [<JsonPropertyName("hash")>] Hash: string
          [<JsonPropertyName("virtualPath")>] VirtualPath: string
          [<JsonPropertyName("fileName")>] FileName: string }
    
        static member Blank() =
            { Name = String.Empty
              Site = String.Empty
              ResourceType = String.Empty
              RawBlob = BlobField.Empty()
              Hash = String.Empty
              VirtualPath = String.Empty
              FileName = String.Empty }
    
    
    /// A record representing a new row in the table `site_plugins`.
    type NewSitePlugin =
        { [<JsonPropertyName("site")>] Site: string
          [<JsonPropertyName("plugin")>] Plugin: string
          [<JsonPropertyName("configuration")>] Configuration: BlobField }
    
        static member Blank() =
            { Site = String.Empty
              Plugin = String.Empty
              Configuration = BlobField.Empty() }
    
    
    /// A record representing a new row in the table `sites`.
    type NewSite =
        { [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("rootPath")>] RootPath: string
          [<JsonPropertyName("url")>] Url: string
          [<JsonPropertyName("displayName")>] DisplayName: string option }
    
        static member Blank() =
            { Name = String.Empty
              RootPath = String.Empty
              Url = String.Empty
              DisplayName = None }
    
    
    /// A record representing a new row in the table `templates`.
    type NewTemplate =
        { [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("rawBlob")>] RawBlob: BlobField
          [<JsonPropertyName("hash")>] Hash: string }
    
        static member Blank() =
            { Name = String.Empty
              RawBlob = BlobField.Empty()
              Hash = String.Empty }
    
    
/// Module generated on 08/01/2024 18:34:58 (utc) via Freql.Tools.
[<RequireQualifiedAccess>]
module Operations =

    let buildSql (lines: string list) = lines |> String.concat Environment.NewLine

    /// Select a `Records.FragmentBlobType` from the table `fragment_blob_type`.
    /// Internally this calls `context.SelectSingleAnon<Records.FragmentBlobType>` and uses Records.FragmentBlobType.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectFragmentBlobTypeRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectFragmentBlobTypeRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.FragmentBlobType.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.FragmentBlobType>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.FragmentBlobType>` and uses Records.FragmentBlobType.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectFragmentBlobTypeRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectFragmentBlobTypeRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.FragmentBlobType.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.FragmentBlobType>(sql, parameters)
    
    let insertFragmentBlobType (context: SqliteContext) (parameters: Parameters.NewFragmentBlobType) =
        context.Insert("fragment_blob_type", parameters)
    
    /// Select a `Records.FragmentTemplate` from the table `fragment_templates`.
    /// Internally this calls `context.SelectSingleAnon<Records.FragmentTemplate>` and uses Records.FragmentTemplate.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectFragmentTemplateRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectFragmentTemplateRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.FragmentTemplate.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.FragmentTemplate>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.FragmentTemplate>` and uses Records.FragmentTemplate.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectFragmentTemplateRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectFragmentTemplateRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.FragmentTemplate.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.FragmentTemplate>(sql, parameters)
    
    let insertFragmentTemplate (context: SqliteContext) (parameters: Parameters.NewFragmentTemplate) =
        context.Insert("fragment_templates", parameters)
    
    /// Select a `Records.PageData` from the table `page_data`.
    /// Internally this calls `context.SelectSingleAnon<Records.PageData>` and uses Records.PageData.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPageDataRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectPageDataRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.PageData.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.PageData>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.PageData>` and uses Records.PageData.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPageDataRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectPageDataRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.PageData.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.PageData>(sql, parameters)
    
    let insertPageData (context: SqliteContext) (parameters: Parameters.NewPageData) =
        context.Insert("page_data", parameters)
    
    /// Select a `Records.PageFragment` from the table `page_fragments`.
    /// Internally this calls `context.SelectSingleAnon<Records.PageFragment>` and uses Records.PageFragment.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPageFragmentRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectPageFragmentRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.PageFragment.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.PageFragment>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.PageFragment>` and uses Records.PageFragment.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPageFragmentRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectPageFragmentRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.PageFragment.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.PageFragment>(sql, parameters)
    
    let insertPageFragment (context: SqliteContext) (parameters: Parameters.NewPageFragment) =
        context.Insert("page_fragments", parameters)
    
    /// Select a `Records.PageVersion` from the table `page_versions`.
    /// Internally this calls `context.SelectSingleAnon<Records.PageVersion>` and uses Records.PageVersion.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPageVersionRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectPageVersionRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.PageVersion.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.PageVersion>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.PageVersion>` and uses Records.PageVersion.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPageVersionRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectPageVersionRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.PageVersion.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.PageVersion>(sql, parameters)
    
    let insertPageVersion (context: SqliteContext) (parameters: Parameters.NewPageVersion) =
        context.Insert("page_versions", parameters)
    
    /// Select a `Records.Page` from the table `pages`.
    /// Internally this calls `context.SelectSingleAnon<Records.Page>` and uses Records.Page.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPageRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectPageRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Page.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.Page>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.Page>` and uses Records.Page.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPageRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectPageRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Page.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.Page>(sql, parameters)
    
    let insertPage (context: SqliteContext) (parameters: Parameters.NewPage) =
        context.Insert("pages", parameters)
    
    /// Select a `Records.PluginResources` from the table `plugin_resources`.
    /// Internally this calls `context.SelectSingleAnon<Records.PluginResources>` and uses Records.PluginResources.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPluginResourcesRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectPluginResourcesRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.PluginResources.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.PluginResources>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.PluginResources>` and uses Records.PluginResources.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPluginResourcesRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectPluginResourcesRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.PluginResources.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.PluginResources>(sql, parameters)
    
    let insertPluginResources (context: SqliteContext) (parameters: Parameters.NewPluginResources) =
        context.Insert("plugin_resources", parameters)
    
    /// Select a `Records.PluginType` from the table `plugin_types`.
    /// Internally this calls `context.SelectSingleAnon<Records.PluginType>` and uses Records.PluginType.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPluginTypeRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectPluginTypeRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.PluginType.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.PluginType>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.PluginType>` and uses Records.PluginType.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPluginTypeRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectPluginTypeRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.PluginType.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.PluginType>(sql, parameters)
    
    let insertPluginType (context: SqliteContext) (parameters: Parameters.NewPluginType) =
        context.Insert("plugin_types", parameters)
    
    /// Select a `Records.Plugin` from the table `plugins`.
    /// Internally this calls `context.SelectSingleAnon<Records.Plugin>` and uses Records.Plugin.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPluginRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectPluginRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Plugin.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.Plugin>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.Plugin>` and uses Records.Plugin.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPluginRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectPluginRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Plugin.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.Plugin>(sql, parameters)
    
    let insertPlugin (context: SqliteContext) (parameters: Parameters.NewPlugin) =
        context.Insert("plugins", parameters)
    
    /// Select a `Records.RenderedPage` from the table `rendered_pages`.
    /// Internally this calls `context.SelectSingleAnon<Records.RenderedPage>` and uses Records.RenderedPage.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectRenderedPageRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectRenderedPageRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.RenderedPage.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.RenderedPage>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.RenderedPage>` and uses Records.RenderedPage.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectRenderedPageRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectRenderedPageRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.RenderedPage.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.RenderedPage>(sql, parameters)
    
    let insertRenderedPage (context: SqliteContext) (parameters: Parameters.NewRenderedPage) =
        context.Insert("rendered_pages", parameters)
    
    /// Select a `Records.ResourceType` from the table `resource_types`.
    /// Internally this calls `context.SelectSingleAnon<Records.ResourceType>` and uses Records.ResourceType.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectResourceTypeRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectResourceTypeRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.ResourceType.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.ResourceType>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.ResourceType>` and uses Records.ResourceType.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectResourceTypeRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectResourceTypeRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.ResourceType.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.ResourceType>(sql, parameters)
    
    let insertResourceType (context: SqliteContext) (parameters: Parameters.NewResourceType) =
        context.Insert("resource_types", parameters)
    
    /// Select a `Records.Resource` from the table `resources`.
    /// Internally this calls `context.SelectSingleAnon<Records.Resource>` and uses Records.Resource.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectResourceRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectResourceRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Resource.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.Resource>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.Resource>` and uses Records.Resource.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectResourceRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectResourceRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Resource.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.Resource>(sql, parameters)
    
    let insertResource (context: SqliteContext) (parameters: Parameters.NewResource) =
        context.Insert("resources", parameters)
    
    /// Select a `Records.SitePlugin` from the table `site_plugins`.
    /// Internally this calls `context.SelectSingleAnon<Records.SitePlugin>` and uses Records.SitePlugin.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectSitePluginRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectSitePluginRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.SitePlugin.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.SitePlugin>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.SitePlugin>` and uses Records.SitePlugin.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectSitePluginRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectSitePluginRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.SitePlugin.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.SitePlugin>(sql, parameters)
    
    let insertSitePlugin (context: SqliteContext) (parameters: Parameters.NewSitePlugin) =
        context.Insert("site_plugins", parameters)
    
    /// Select a `Records.Site` from the table `sites`.
    /// Internally this calls `context.SelectSingleAnon<Records.Site>` and uses Records.Site.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectSiteRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectSiteRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Site.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.Site>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.Site>` and uses Records.Site.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectSiteRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectSiteRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Site.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.Site>(sql, parameters)
    
    let insertSite (context: SqliteContext) (parameters: Parameters.NewSite) =
        context.Insert("sites", parameters)
    
    /// Select a `Records.Template` from the table `templates`.
    /// Internally this calls `context.SelectSingleAnon<Records.Template>` and uses Records.Template.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectTemplateRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectTemplateRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Template.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.Template>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.Template>` and uses Records.Template.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectTemplateRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectTemplateRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Template.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.Template>(sql, parameters)
    
    let insertTemplate (context: SqliteContext) (parameters: Parameters.NewTemplate) =
        context.Insert("templates", parameters)
    