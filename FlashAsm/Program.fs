open System

let color = function
    | 0 -> 'K' // 000 (BGR)
    | 1 -> 'R' // 001 B
    | 2 -> 'G' // 010 G
    | 3 -> 'Y' // 011 R+G
    | 4 -> 'B' // 100 B
    | 5 -> 'M' // 101 R+B
    | 6 -> 'C' // 110 G+B
    | 7 -> failwith "White (7) is not used as a digit value"
    | d -> sprintf "Unknown color digit: %i" d |> failwith

let valuesToColors = Seq.map (fun v -> [(v / 49) % 7; (v / 7) % 7; v % 7]) >> Seq.concat >> Seq.map color

let addWhites = Seq.scan (fun (_, a) t -> a, if t = a then 'W' else t) ('W', 'W') >> Seq.map snd >> Seq.skip 1

let asm (prog: int list) =
    let checksum = Seq.fold (fun s t -> s - byte t) 0uy >> int
    let unknown0 = [304; 320; 302]
    let unknown1 = [001; 003]
    let pay = [199]
    let unknown2 = [045; 036; 147]
    let unknown3 = [000; 030; 147; 000; 174]
    let terminator = 334
    let mid = pay @ unknown2 @ prog @ unknown3
    let length = [206; 000; mid.Length]
    let pre = unknown1 @ length @ mid
    unknown0 @ pre @ [checksum pre; terminator]
    |> valuesToColors
    |> addWhites
    |> String.Concat

[000; 000; 000; 184] |> asm |> printfn "%s"