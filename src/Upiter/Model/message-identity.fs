namespace Upiter.Model
    open System
    open System.Security.Cryptography

    module MessageIdentity =
        let generate (request: Guid) (index: Int32) : Guid =
            let swapByteOrderPairs (bytes: byte[]) : byte[] =
                Array.mapi (fun index value -> 
                    match index with
                    | 0 -> Array.get bytes 3
                    | 1 -> Array.get bytes 2
                    | 2 -> Array.get bytes 1
                    | 3 -> Array.get bytes 0
                    | 4 -> Array.get bytes 5
                    | 5 -> Array.get bytes 4
                    | 6 -> Array.get bytes 7
                    | 7 -> Array.get bytes 6
                    | _ -> Array.get bytes index
                ) bytes
            let namespaceBytes = swapByteOrderPairs (Guid("dbe13a48ebe5444183cb92e0da52f1e8").ToByteArray())
            let inputBytes = Array.concat [ request.ToByteArray(); BitConverter.GetBytes(index) ]
            using (SHA1.Create()) (fun algorithm -> 
                algorithm.TransformBlock(namespaceBytes, 0, namespaceBytes.Length, null, 0) |> ignore
                algorithm.TransformFinalBlock(inputBytes, 0, inputBytes.Length) |> ignore
                let result =
                    Array.truncate 16 algorithm.Hash
                    |> Array.mapi (fun index (value: byte) ->
                            match index with
                            | 6 -> (value &&& 0x0Fuy) ||| (5uy <<< 4)
                            | 8 -> (value &&& 0x3Fuy) ||| 0x80uy
                            | _ -> Array.get algorithm.Hash index
                        )
                    |> swapByteOrderPairs
                Guid(result)
            )