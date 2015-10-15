
#load "../FlippingGit/GitTypes.fs"
#load "../FlippingGit/GitFolder.fs"
open System.IO
open System.IO.Compression
open System.Text
open FlippingGit

let hash = Hash.FromString "70c379b63ffa0795fdbfbc128e5a2818397b7ef8"
let hash' = Hash.FromByteArray hash.Bytes
hash = hash'

let getFilePath fileName =
    Path.Combine(__SOURCE_DIRECTORY__, "examples", fileName)

let commit = GitObject.ParseFile (getFilePath "commit01")

let tag01 = GitObject.ParseFile (getFilePath "tag01")
let tag02 = GitObject.ParseFile (getFilePath "tag02")


let folder = new GitFolder(@"c:\code\testrepo")
let commits = folder.Log()
for commit in commits do
    printfn "%A" commits

let hexViewFile path = 
    use deflateStream = Helpers.openDeflateStream path
    let buffer = Array.zeroCreate 8
    let rec loop () =
        let read = deflateStream.Read(buffer, 0, buffer.Length)
        for x in buffer do
            printf "%i " x
        printfn " %s" (Encoding.ASCII.GetString buffer)
        if read > 0 then
            loop()
    loop()

hexViewFile (getFilePath "commit01")

let textViewFile path = 
    use deflateStream = Helpers.openDeflateStream path
    use file = new StreamReader(deflateStream, Encoding.ASCII)
    printfn "%s" (file.ReadToEnd())

textViewFile (getFilePath "tag01")

Path.Combine(@"c:\code\testrepo\.git\objects", "62", "6a8f7d7e75a5e554227d3b8fb1ed8c77fd3d6d")
