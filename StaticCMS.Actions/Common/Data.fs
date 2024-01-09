module StaticCMS.Actions.Common

module Data =

    open System.IO
    open System.Text
    open System.Text.Json
    open System.Text.Json.Serialization
    open FsToolbox.Core

    [<CLIMutable>]
    type BasicPageData =
        { [<JsonPropertyName("site_name")>]
          SiteName: string
          [<JsonPropertyName("title")>]
          Title: string
          [<JsonPropertyName("styles")>]
          Styles: Style seq
          [<JsonPropertyName("scripts")>]
          Scripts: Script seq
          [<JsonPropertyName("icon_script")>]
          IconScript: IconScript
          AdditionData: Map<string, string> }

        member bpd.Serialize() =
            use ms = new MemoryStream()
            let mutable options = JsonWriterOptions()

            options.Indented <- true

            use writer = new Utf8JsonWriter(ms, options)

            Json.writeObject
                (fun w ->
                    w.WriteString("site_name", bpd.SiteName)
                    w.WriteString("title", bpd.Title)

                    Json.writeArray
                        (fun iw ->
                            bpd.Styles
                            |> Seq.iter (fun s -> Json.writeObject (fun ow -> ow.WriteString("url", s.Url)) iw))
                        "styles"
                        w

                    Json.writeArray
                        (fun iw ->
                            bpd.Scripts
                            |> Seq.iter (fun s -> Json.writeObject (fun ow -> ow.WriteString("url", s.Url)) iw))
                        "scripts"
                        w

                    Json.writePropertyObject (fun ow -> ow.WriteString("url", bpd.IconScript.Url)) "icon_script" w

                    bpd.AdditionData |> Map.iter (fun k v -> w.WriteString(k, v)))
                writer

            writer.Flush()

            ms.ToArray() |> Encoding.UTF8.GetString



    and [<CLIMutable>] Style =
        { [<JsonPropertyName("url")>]
          Url: string }

    and [<CLIMutable>] Script =
        { [<JsonPropertyName("url")>]
          Url: string }

    and [<CLIMutable>] IconScript =
        { [<JsonPropertyName("url")>]
          Url: string }

    [<CLIMutable>]
    type NavBarData =
        { [<JsonPropertyName("items")>]
          Items: NavBarDataItem seq }

    and [<CLIMutable>] NavBarDataItem =
        { [<JsonPropertyName("url")>]
          Url: string
          [<JsonPropertyName("title")>]
          Title: string }

    [<CLIMutable>]
    type FeatureCardsData =
        { [<JsonPropertyName("id")>]
          Id: string
          [<JsonPropertyName("features")>]
          Features: FeatureCardDataItem seq }

    and [<CLIMutable>] FeatureCardDataItem =
        { [<JsonPropertyName("id")>]
          Id: string
          [<JsonPropertyName("icon")>]
          Icon: string
          [<JsonPropertyName("title")>]
          Title: string
          [<JsonPropertyName("details_html")>]
          DetailHtml: string }
