#load "../FlippingGit/GitTypes.fs"
#load "../FlippingGit/Crypto.fs"

open System
open System.Diagnostics
open System.IO
open FlippingGit

let rootDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".tb")
let objectsDir = Path.Combine(rootDir, "objects")

let testDir = @"C:\Users\robert.RFQ-HUB.COM\Copy\Pictures\201409Holiday"

let test1() =
    for file in Directory.GetFiles testDir do
        let hash = Crypto.hashFile file
        let objectDir = Path.Combine(objectsDir, hash.DirectoryName)
        let objectPath = Path.Combine(objectsDir, hash.DirectoryName, hash.FileName)
        Directory.CreateDirectory(objectDir) |> ignore
        use file = File.Open(objectPath, FileMode.Create)
        ()

let main () = 

    let sw = Stopwatch.StartNew()
    test1()
    printfn "Finished in %O" sw.Elapsed

    Console.ReadLine() |> ignore

main()