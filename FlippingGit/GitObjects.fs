namespace FlippingGit
open System
open System.IO
open System.IO.Compression

module GitObjects =

    let parseObject path = 
        let deflateStream = new DeflateStream(File.OpenRead(path), CompressionMode.Decompress)
        deflateStream.ReadByte() |> ignore
        ()
