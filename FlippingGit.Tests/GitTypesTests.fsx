
#load "../FlippingGit/Debug.fs"
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

let blob01 = GitObject.ParseFile (getFilePath "blob01")

let expectedCommit01 =
         {Tree =
           {Bytes =
             [|173uy; 12uy; 245uy; 101uy; 208uy; 231uy; 211uy; 253uy; 192uy; 127uy;
               251uy; 99uy; 148uy; 162uy; 222uy; 160uy; 5uy; 232uy; 5uy; 16uy|];
            String = "ad0cf565d0e7d3fdc07ffb6394a2dea005e80510";};
          Parent =
           Some
             {Bytes =
               [|225uy; 16uy; 99uy; 161uy; 125uy; 237uy; 133uy; 87uy; 169uy; 124uy;
                 170uy; 245uy; 73uy; 55uy; 166uy; 163uy; 59uy; 30uy; 29uy; 125uy|];
              String = "e11063a17ded8557a97caaf54937a6a33b1e1d7d";};
          Author = "Robert Pickering <robert@strangelights.com> 1432818778 +0200";
          Committer =
           "Robert Pickering <robert@strangelights.com> 1432818778 +0200";
          Text = "another commit
";}
let commit01 = GitObject.ParseFile (getFilePath "commit01")
match commit01 with 
| Commit (_, x) -> expectedCommit01 = x
| _ -> false

let expectedTag01 =
    { Object =
       {Bytes =
         [|98uy; 106uy; 143uy; 125uy; 126uy; 117uy; 165uy; 229uy; 84uy; 34uy;
           125uy; 59uy; 143uy; 177uy; 237uy; 140uy; 119uy; 253uy; 61uy; 109uy|];
        String = "626a8f7d7e75a5e554227d3b8fb1ed8c77fd3d6d";};
      Type = "commit";
      Tag = "v1.4";
      Tagger = "Robert Pickering <robert@strangelights.com> 1432847405 +0200";
      Text = "my version 1.4
";}
let tag01 = GitObject.ParseFile (getFilePath "tag01")
match tag01 with
| Tag(size, x) -> 153 = size && x = expectedTag01
| _ -> false

let tag02 = GitObject.ParseFile (getFilePath "tag02")
let tree01 = GitObject.ParseFile (getFilePath "tree01")
let tree02 = GitObject.ParseFile (getFilePath "tree02")


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

hexViewFile (getFilePath "tree01")

let textViewFile path = 
    use deflateStream = Helpers.openDeflateStream path
    use file = new StreamReader(deflateStream, Encoding.ASCII)
    printfn "%s" (file.ReadToEnd())

textViewFile (getFilePath "tag01")

Path.Combine(@"c:\code\testrepo\.git\objects", "62", "6a8f7d7e75a5e554227d3b8fb1ed8c77fd3d6d")
