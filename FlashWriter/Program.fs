open System
open System.Threading

type Color = Black | Red | Green | Blue | Cyan | Magenta | Yellow | White

let toConsoleColor = function
    | Black   -> ConsoleColor.Black
    | Red     -> ConsoleColor.Red
    | Green   -> ConsoleColor.Green
    | Blue    -> ConsoleColor.Blue
    | Cyan    -> ConsoleColor.Cyan
    | Magenta -> ConsoleColor.Magenta
    | Yellow  -> ConsoleColor.Yellow
    | White   -> ConsoleColor.White

let toColor = function
    | 'K' -> Black
    | 'R' -> Red
    | 'G' -> Green
    | 'B' -> Blue
    | 'C' -> Cyan
    | 'M' -> Magenta
    | 'Y' -> Yellow
    | 'W' -> White
    | c -> sprintf "Unknown color code: %c" c |> failwith

let toColors = Seq.map toColor

let show color =
    Console.BackgroundColor <- toConsoleColor color
    Console.Clear()

let flash colors =
    colors |> Seq.iter (fun c -> show c; Thread.Sleep 50)
    show White

let savedBG = Console.BackgroundColor
let savedFG = Console.ForegroundColor
show White
Console.ForegroundColor <- toConsoleColor Black
printfn "Place OzoBot on the screen and press"
let args = Environment.GetCommandLineArgs ()
if args.Length > 1 then
    let prog = args.[1]
    printfn "Press [ENTER] to program:\n%s" prog
    Console.ReadLine() |> ignore
    prog |> toColors |> flash
    show White
    printfn "Done. Press [ENTER]"
    Console.ReadLine() |> ignore
else // no program
    printfn "No program given. Hold Ozobot's power button for 2 seconds and place against white background to calibrate."
    Console.ReadLine() |> ignore
Console.BackgroundColor <- savedBG
Console.ForegroundColor <- savedFG
Console.Clear()