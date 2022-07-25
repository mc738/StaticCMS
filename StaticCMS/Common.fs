namespace StaticCMS

open System
open System.IO
open System.Security.Cryptography

[<AutoOpen>]
module Common =
    
     let hashStream (hasher: SHA256) (stream: Stream) =
        stream.Seek(0L, SeekOrigin.Begin) |> ignore
        let hash = hasher.ComputeHash stream |> Convert.ToHexString
        stream.Seek(0L, SeekOrigin.Begin) |> ignore
        hash
     
     let hashBytes (hasher: SHA256) (bytes: byte array) =
         hasher.ComputeHash bytes |> Convert.ToHexString