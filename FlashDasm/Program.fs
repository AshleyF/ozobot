open System

let dasm (name, code) =
    let removeWhites code =
        Seq.append code ['W'] // always end with W
        |> Seq.append ['W'] |> Seq.pairwise
        |> Seq.map (fun (a, b) -> if b = 'W' then a else b)
        |> String.Concat
    let byteValue =
        let value = function
            | 'K' -> 0 // 000 (BGR)
            | 'R' -> 1 // 001 B
            | 'G' -> 2 // 010 G
            | 'Y' -> 3 // 011 R+G
            | 'B' -> 4 // 100 B
            | 'M' -> 5 // 101 R+B
            | 'C' -> 6 // 110 G+B
            | 'W' -> failwith "White is not used as a direct digit value"
            | c -> sprintf "Unknown color code: %c" c |> failwith
        function
        | [|c2; c1; c0|] ->
            let v2 = value c2
            let v1 = value c1
            let v0 = value c0
            v0 + (v1 * 7) + (v2 * 49)
        | _ -> failwith "Expected sequence of sets of three colors"
    printf "Dasm: %s " name
    code
    |> removeWhites
    |> Seq.chunkBySize 3
    |> Seq.map byteValue
    |> Seq.skip 8 // frame and envelope + first three instructions (prepended by OzoBlockly)
    |> Seq.iter (fun v -> printf "%02X " v)
    printfn ""

Console.ReadLine() |> ignore