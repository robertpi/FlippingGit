namespace FlippingGit
module Crypto =

    open System
    open System.IO
    open System.Text
    open System.Security.Cryptography

    type GitStream(path: string) =
        inherit FileStream(path, FileMode.Open, FileAccess.Read)
        let fileInfo = new FileInfo(path)
        let prefixString = sprintf "blob %i\u0000" fileInfo.Length
        let prefixBytes = Encoding.ASCII.GetBytes prefixString
        let mutable pos = 0

        override this.Read(buffer: Byte[], offset: int, count: int) =
            let readFrom = pos + offset
            if readFrom < prefixBytes.Length - 1 then
                let bytesToTake = min (prefixBytes.Length - readFrom) count
                Array.Copy(prefixBytes, readFrom, buffer, 0, bytesToTake)
                pos <- bytesToTake
                bytesToTake
            else
                base.Read(buffer, offset, count) 

        override this.CanRead = true
        override this.CanWrite = false
        override this.CanSeek = false
        override this.Position = int64 pos + base.Position
        override this.Length = int64 prefixBytes.Length + fileInfo.Length

        static member Create(path: string) = new GitStream(path)

    let private sha1 = SHA1.Create()
    let hashFile path =
        use fileSteam = GitStream.Create(path)
        let byteBuff = sha1.ComputeHash(fileSteam)
        Hash.FromByteArray byteBuff
