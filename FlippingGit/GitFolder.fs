namespace FlippingGit
open System.IO

type GitFolder(location: string) =
    let gitDir = Path.Combine(location, ".git")
    let objectsDir = Path.Combine(gitDir, "objects")
    let headFile = Path.Combine(gitDir, "HEAD")

    let getHeadHash() =
        let headText = File.ReadAllText(headFile)
        match headText.Split(':') with
        | [| "ref"; refFile |] -> 
            let cleanedRefFile = refFile.Trim().Replace('/', Path.DirectorySeparatorChar)
            let filePath = Path.Combine(gitDir, cleanedRefFile)
            File.ReadLines(filePath)
            |> Seq.head
            |> Hash.FromString
        | _ -> failwith "not implemented"

    member x.Log() = 
        let headHash = getHeadHash()
        let walkCommits (hash: Hash, count) =
            printfn "%s %s %s" objectsDir hash.DirectoryName hash.FileName

            let objectPath = Path.Combine(objectsDir, hash.DirectoryName, hash.FileName)
            printfn "objectPath: %s" objectPath
            let gitObject = GitObject.ParseFile objectPath
            match gitObject with
            | Commit (_, commit) -> 
                match commit.Parent with
                | Some (hash) when count < 10 -> 
                    Some(commit, (hash, (count + 1)))
                | Some (_)
                | None -> None
            | _ -> failwithf "Unexpected object type: %A" gitObject
        Seq.unfold walkCommits (headHash, 0)
       
        