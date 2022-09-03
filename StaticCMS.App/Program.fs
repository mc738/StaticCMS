open System
open System.IO
open Fluff.Core
open StaticCMS
open StaticCMS.DataStore
open Wikd.DataStore


module MetadataTest =
    
    let run _ =
        let lines1 = File.ReadAllText "C:\\Users\\44748\\Projects\\Wikd\\Documents\\overview\\stand_alone\\stand_alone_examples.md" |> fun r -> r.Split(Environment.NewLine) |> List.ofArray
        
        let lines2 = File.ReadAllText "C:\\Users\\44748\\Projects\\Wikd\\Documents\\tools.md" |> fun r -> r.Split(Environment.NewLine) |> List.ofArray
        
        let (r1, md1) = FDOM.Core.Parsing.Parser.ParseLinesAndMetadata(lines1)
        let (r2, md2) = FDOM.Core.Parsing.Parser.ParseLinesAndMetadata(lines2)
        
        let bc1 = r1.CreateBlockContent()
        let bc2 = r2.CreateBlockContent()
        
        ()
    
(*
module WikdTest =
        
    let run _ =
        let store = WikdStore.Create("C:\\ProjectData\\static_cms\\plugins\\wikd\\wikd.db")
        
        let template = File.ReadAllText "C:\\ProjectData\\static_cms\\plugins\\wikd\\resources\\wikd_template.mustache" |> Mustache.parse
        
        Wikd.Renderer.run store "C:\\ProjectData\\static_cms\\plugins\\wikd\\example\\wiki" template
        
        ()
*)

MetadataTest.run()

//WikdTest.run ()

let store = StaticStore.Create("C:\\ProjectData\\static_cms\\static_store.db")

let createRef _ = Guid.NewGuid().ToString("n")

let template = File.ReadAllBytes "C:\\ProjectData\\static_cms\\templates\\peeps_index.mustache"

store.AddTemplate("peeps_index", template)

store.AddSite("peeps", "https://peeps.psionic.cloud", "/home/max/sites/peeps")
let pageRef = createRef ()
let versionRef = createRef ()
    
store.AddPage(pageRef, "peeps", "index", "index_generated")
store.AddPageVersion(pageRef, versionRef, "peeps_index", true)
store.AddFragmentTemplate("features", [||])
store.AddFragmentTemplate("news", File.ReadAllBytes "C:\\ProjectData\\static_cms\\example_fragments\\peeps_news_template.mustache")
store.AddFragmentTemplate("docs", File.ReadAllBytes "C:\\ProjectData\\static_cms\\example_fragments\\peeps_docs_template.mustache")
store.AddPageFragment(versionRef, "features", "features", File.ReadAllBytes "C:\\ProjectData\\static_cms\\example_fragments\\peeps_features.md", "markdown")
store.AddPageFragment(versionRef, "news", "news", File.ReadAllBytes "C:\\ProjectData\\static_cms\\example_fragments\\peeps_news.json", "json")
store.AddPageFragment(versionRef, "docs", "docs", File.ReadAllBytes "C:\\ProjectData\\static_cms\\example_fragments\\peeps_docs.json", "json")

match PageRenderer.run store "peeps" "index" with
| Ok p -> File.WriteAllText("C:\\ProjectData\\Peeps\\website\\index_generated.html", p)
| Error e -> printfn $"Error: {e}"


(*
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
*)
//|> fun p -> File.WriteAllText("C:\\ProjectData\\Peeps\\website\\index_generated.html", p)

// For more information see https://aka.ms/fsharp-console-apps
printfn "Hello from F#"
