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
    let head = [304; 320; 302]
    let ver = [001; 003]
    let deny = [] // [199]
    let terminator = 334
    let mid = deny @ prog
    let len = mid.Length
    let length = [219 - len; 000; len] // TODO: why?
    let pre = ver @ length @ mid
    head @ pre @ [checksum pre; terminator]

let encode = valuesToColors >> addWhites >> String.Concat

let led = 0xB8 // "set LED color (Red=127, Green=127, Blue=0)"
let wait = 0x9B // "wait N x 10ms (N)"

let tests () =
    let equal a b = if a <> b then printfn "TEST FAILURE: %A <> %A" a b
    equal ([045; 036; 147; 000; 000; 000; led; 000; 030; 147; 000; 174] |> asm |> encode)
           "CRYCYMCRWKWRKWYBRBKWKWRMKCYKMRYKWKWKWKWKWKYMGKWKWBGYKWKWKYWCKMYCMW"
    equal ([045; 036; 147; 127; 000; 000; led; 100; wait; 000; 127; 000; led; 100; wait; 000; 000; 127; led; 100; wait; 000; 174] |> asm |> encode)
           "CRYCYMCRWKWRKWYBKWKWKWYGKCYKMRYKWGBRKWKWKWYMGWKGYRWKWKGBRKWKYMGWKGYRWKWKWKWGBRYMGWKGYRWKWKYWCBMCWMW"
tests()

//      [ head    ] [ ver ] [ length  ]     [ unknown ] [ code        ] [ unknown3        ] CHK END
// Paid 304 320 302 001 003 207 000 012     045 036 147 000 000 000 184 000 030 147 000 174 038 334
// Not  304 320 302 001 003 206 000 013 199 045 036 147 000 000 000 184 000 030 147 000 174 095 334
// CRYCYMCRWKWRKWYBRYKWKWRCBKYKCYKMRYKWKWKWKWKWKYMGKWKWBGYKWKWKYWCRCBCMW

[045; 036; 147; 127; 000; 000; led; 100; wait; 000; 127; 000; led; 100; wait; 000; 000; 127; led; 100; wait; 000; 174] |> asm |> encode |> printfn "Prog: %s"

Console.ReadLine()