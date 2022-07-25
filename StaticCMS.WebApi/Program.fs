open System.IO
open Fluff.Core
open StaticCMS
open StaticCMS.DataStore

//let store = StaticStore.Create("C:\\ProjectData\\static_cms\\store.db")

//let template = File.ReadAllBytes "C:\\ProjectData\\static_cms\\templates\\peeps_index.mustache"

//store.AddTemplate("peeps_index", template)

let pageTemplate = File.ReadAllText "C:\\ProjectData\\static_cms\\templates\\peeps_index.mustache" |> Mustache.parse

[ "features",
  Mustache.Value.Scalar
  <| PageFragments.renderMarkdownFragment
      PageFragments.rewriteTitles
      (File.ReadAllText "C:\\ProjectData\\static_cms\\example_fagrments\\peeps_features.md")
  "news",
  Mustache.Value.Scalar
  <| PageFragments.renderJsonFragment
      (File.ReadAllText "C:\\ProjectData\\static_cms\\example_fagrments\\peeps_news_template.mustache")
      (File.ReadAllText "C:\\ProjectData\\static_cms\\example_fagrments\\peeps_news.json")
  "docs",
  Mustache.Value.Scalar
  <| PageFragments.renderJsonFragment
      (File.ReadAllText "C:\\ProjectData\\static_cms\\example_fagrments\\peeps_docs_template.mustache")
      (File.ReadAllText "C:\\ProjectData\\static_cms\\example_fagrments\\peeps_docs.json") ]
|> Map.ofList
|> fun r -> ({ Values = r; Partials = Map.empty }: Mustache.Data)
|> fun d -> Mustache.replace d true pageTemplate
|> fun p -> File.WriteAllText("C:\\ProjectData\\Peeps\\website\\index_generated.html", p)

// For more information see https://aka.ms/fsharp-console-apps
printfn "Hello from F#"
