namespace StaticCMS

open System.IO
open System.IO.Compression

module Themes =
    
    let loadTheme (root: string) (name: string) (sitePath: string) =
        let themeDir = Path.Combine(root, name)
        
        match Directory.Exists themeDir with
        | true ->
            // If found...
            
            // Copy resources to [sitePath]/resources
            // OR
            // Add resources to store for site (if not existing).
            
            // Copy templates/pages to [sitePath]/page_templates
            // OR
            // Add page_templates to store (if not existing).
            
            // Copy templates/fragment to [sitePath]/fragment_templates
            // OR
            // Add fragment_templates to store (if not existing).
            
            // Load themes for plugins
            Ok ()
        | false ->
            Error $"Theme `{name}` not found in `{root}`"
      
    let bundle (root: string) (name: string) (outPath: string) =
        let themeDir = Path.Combine(root, name)
        
        match Directory.Exists themeDir with
        | true ->
            try
                let out = Path.Combine(outPath, $"{name}.zip")
                ZipFile.CreateFromDirectory(themeDir, out)
                Ok $"Theme bundled. Output path: `{out}`"
            with
            | exn -> Error $"Failed to bundle theme. Error: {exn.Message}"
        | false ->
            Error $"Theme `{name}` not found in `{root}`"
    


