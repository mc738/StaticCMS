namespace StaticCMS.App.Actions

open System.Text.Json

[<AutoOpen>]
module Common =

    type ActionResult<'T> =
        | Success of Message: string * Result: 'T option
        | Skipped of Message: string * Result: 'T option
        | Failed of Message: string

    let serializeJson<'T> (value: 'T) =
        let options = JsonSerializerOptions()

        options.WriteIndented <- true


        JsonSerializer.Serialize(value, options)
