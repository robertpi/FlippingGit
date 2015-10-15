namespace FlippingGit
open System
open System.IO
open System.IO.Compression
open System.Text

module Helpers =
    let private chars = [| yield! [| '0' .. '9' |]; yield! [| 'a' .. 'f' |] |]
    let private charMap = 
         Seq.map2 (fun x y -> x, y) chars (seq { 0uy .. 15uy })
         |> Map.ofSeq

    let inline private charOfLowBits (b: Byte) =
        let lowBits = b &&& 0b00001111uy
        chars.[int lowBits]
     
    let inline private charOfHighBits (b: Byte) =
        let highBits = b >>> 4
        chars.[int highBits]

    let byteArrayToHexChars (byteBuff: Byte[]) =    
        let charBuff: char[] = Array.zeroCreate (byteBuff.Length * 2)
        for x in 0  .. byteBuff.Length - 1 do
            let byte = byteBuff.[x]
            let charBuffIndex = x * 2
            charBuff.[charBuffIndex] <- charOfHighBits byte  
            charBuff.[charBuffIndex + 1] <- charOfLowBits byte
        charBuff

    let stringToByteArray (input: string) =    
        let byteBuff: Byte[] = Array.zeroCreate (input.Length / 2)
        for x in 0  .. byteBuff.Length - 1 do
            let charIndex = x * 2
            let byte1 = charMap.[input.[charIndex]] 
            let byte2 = charMap.[input.[charIndex + 1]] 
            byteBuff.[x] <- (byte1 <<< 4) + byte2 
        byteBuff

    let stringUntil (buffer: Byte[]) untilChar initIndex =
        let sb = new StringBuilder()
        let rec loop x = 
            if x < buffer.Length then 
                let c = char buffer.[x]
                if c <> untilChar then
                    sb.Append(c) |> ignore
                    loop (x + 1)
                else
                    sb.ToString(), x
            else
                failwith "End of array before found character" 
        loop initIndex

    let readStreamUntilNull (stream: Stream) =
        let ra = new ResizeArray<byte>()
        let rec innerLoop index =
            let byteAsInt = stream.ReadByte()
            printfn "read %i" byteAsInt
            match byteAsInt with
            | -1 -> 
                if index = 0 then None
                else failwith "End of file without encountering Null"
            | 0 -> 
                ra.Add(byte 0)
                Some (ra.ToArray())
            | x -> 
                ra.Add(byte x)
                innerLoop (index + 1)
        innerLoop 0

    let readObjectHeader (stream: Stream) =
        let bufferOpt = readStreamUntilNull stream
        match bufferOpt with
        | Some buffer ->
            let tag, offset = stringUntil buffer ' ' 0
            let length, _ = stringUntil buffer '\u0000' offset
            tag, int length
        | _ -> failwith "No header to read"

    let openDeflateStream path =
        let stream =  File.OpenRead(path)
        // Why? http://george.chiramattel.com/blog/2007/09/deflatestream-block-length-does-not-match.html
        stream.Position <- 2L
        new DeflateStream(stream, CompressionMode.Decompress)

    let tagsToMap (stream: Stream) streamLength (tagsToRead: Set<string>)  =
        // maybe better to create a standard size buffer for mem reuse
        let buffer: Byte[] = Array.zeroCreate streamLength
        let read = stream.Read(buffer, 0, buffer.Length)
        let readKeyValuePair offset =
            if buffer.Length >= offset then 
                let label, offset = stringUntil buffer ' ' offset
                printfn "reading %s" label
                if tagsToRead.Contains label then
                    let value, offset = stringUntil buffer '\n' (offset + 1)
                    Some ((label, value), offset + 1)
                else
                    None
            else
                None

        // we assum here we'll read all the buffer at once, may not be true
        let map =
            Seq.unfold readKeyValuePair 0
            |> Map.ofSeq
        let offset = map |> Seq.sumBy (fun kvp -> kvp.Key.Length + kvp.Value.Length + 2)
        let textStart = offset + 1
        let textChars =
            Seq.init (streamLength - textStart) (fun x -> char buffer.[textStart + x])
            |> Seq.toArray
        map, new String(textChars)

// in git objects hashes are either stored as byte arrays or string of chars
// it would be nice to make this structure lazy, so if only one is needed the 
// other isn't initalized
type Hash =
    { Bytes: Byte[]
      String: string }
    member this.DirectoryName = this.String.[0 .. 1]
    member this.FileName = this.String.[2 ..]
    static member FromByteArray bytes =
        let chars = Helpers.byteArrayToHexChars bytes
        { Bytes = bytes
          String = new String(chars) }
    static member FromString string =
        let bytes = Helpers.stringToByteArray string
        { Bytes = bytes
          String = string }
        
type TreeLine =
    { Flags: int
      Name: string
      Hash: Hash }
    static member ParseFromStream (stream: Stream) =
        printfn "starting ParseFromStream"
        let bufferOpt = Helpers.readStreamUntilNull stream
        let treeLineOfBuffer buffer =
            printfn "%A - %s" buffer (System.Text.Encoding.ASCII.GetString buffer)
            let flags, offset = Helpers.stringUntil buffer ' ' 0
            let filename, offset = Helpers.stringUntil buffer '\u0000' offset
            let hashBuffer = Array.zeroCreate 20
            let read = stream.Read(hashBuffer, 0, hashBuffer.Length)
            // TODO test read
            let hashStart = offset + 1
            let hashEnd = hashStart + 20
            let treeLine =
                { Flags = int flags
                  Name = filename
                  Hash= Hash.FromByteArray hashBuffer }
            treeLine
        Option.map treeLineOfBuffer bufferOpt

type Commit =
    { Tree: Hash
      Parent: option<Hash>
      Author: string
      Committer: string
      Text: string }
    static member ParseFromStream (stream: Stream) length =
        let tagsToRead = [ "tree"; "parent"; "author"; "committer"] |> Set.ofList
        let map, text = Helpers.tagsToMap stream length tagsToRead
        printfn "%A" map
        { Tree =  map.["tree"] |> Hash.FromString
          Parent = map.TryFind "parent" |> Option.map Hash.FromString
          Author = map.["author"]
          Committer = map.["committer"]
          Text =  text }
type Tag =
    { Object: Hash
      Type: string
      Tag: string
      Tagger: string
      Text: string }
    static member ParseFromStream (stream: Stream) length =
        let tagsToRead = [ "object"; "type"; "tag"; "tagger"] |> Set.ofList
        let map, text = Helpers.tagsToMap stream length tagsToRead
        { Object = map.["object"] |> Hash.FromString
          Type = map.["type"]
          Tag = map.["tag"]
          Tagger = map.["tagger"]
          Text = text }

type GitObject =
    | Blob of int * (unit -> Stream)
    | Tree of int * TreeLine[]
    | Commit of int * Commit
    | Tag of int * Tag

    static member ParseFile path =
        use deflateStream = Helpers.openDeflateStream path
        let tag, length = Helpers.readObjectHeader deflateStream
        printfn "read tag"
        let readStream() = 
            let stream = new DeflateStream(File.OpenRead(path), CompressionMode.Decompress)
            stream:> Stream
        match tag with
        | "blob" -> Blob (length, readStream)
        | "tree" -> 
            let treeLines = 
                Seq.initInfinite (fun i -> TreeLine.ParseFromStream deflateStream)
                |> Seq.takeWhile (fun x -> Option.isSome x)
                |> Seq.map (fun x -> Option.get x)
                |> Seq.toArray
            Tree (length, treeLines)
        | "commit" -> 
            let commit = Commit.ParseFromStream deflateStream length
            Commit (length, commit)
        | "tag" -> 
            let tag = Tag.ParseFromStream deflateStream length
            Tag(length, tag)
        | _ -> 
            failwith (sprintf "unknown tag '%s'" tag)