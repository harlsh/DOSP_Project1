#time "on"
#load "Config.fsx"
#load "Message.fsx"

#r "nuget: Akka.Remote"
#r "nuget: Akka.FSharp"


open System
open Akka.FSharp
open System.Security.Cryptography
open System.Diagnostics
open Message


let system =
    System.create "my-system" (Configuration.load ())


let generateRandomString (length: int) =
    let characters =
        Array.concat (
            [ [| 'a' .. 'z' |]
              [| 'A' .. 'Z' |]
              [| '0' .. '9' |] ]
        )

    let sz = Array.length characters in

    "harishrebollavar"
    + String(Array.init length (fun _ -> characters.[Random().Next sz]))

let encodeToSHA256 (str: string) =
    System.Text.Encoding.ASCII.GetBytes(str)
    |> (SHA256.Create()).ComputeHash
    |> Array.map (fun (x: byte) -> System.String.Format("{0:x2}", x))
    |> String.concat String.Empty

let hasKZeros (k: int) (hash: string) =
    let index =
        hash |> Seq.tryFindIndex (fun x -> x <> '0')

    index.IsSome && index.Value = k


let slaveActor (mailbox: Actor<_>) =
    let rec loop () =
        actor {
            let! message = mailbox.Receive()

            match message with
            | WorkerMessage (N, K) ->
                for i = 4 to N + 1 do
                    for j = 1 to N do
                        let S = generateRandomString i
                        let encodedString = S |> encodeToSHA256

                        if encodedString |> hasKZeros K then
                            mailbox.Sender()
                            <! FinishMessage(S, encodedString)

                mailbox.Sender() <! RepeatMessage(N, K)
            | _ -> printfn "Weird Message"

            return! loop ()
        }

    loop ()

let masterActor (W: int) (mailbox: Actor<_>) =
    let rec loop () =
        actor {
            let! message = mailbox.Receive()

            match message with
            | DispatcherMessage (N, K) ->
                let workersList =
                    [ for i in 1 .. W do
                          yield (spawn system ($"slave{i}") slaveActor) ]

                workersList
                |> Seq.iter (fun worker -> printfn "%s" (worker.Path.ToString()))

                workersList
                |> Seq.iter (fun worker -> worker <! WorkerMessage(N, K))

            | FinishMessage (normalString, hashString) ->
                printfn "\nString : %s\nHash : %s\n" normalString hashString
                mailbox.Context.System.Terminate() |> ignore

            | RepeatMessage (N, K) -> mailbox.Sender() <! WorkerMessage(N, K)

            | _ -> printfn "Weird message"

            return! loop ()
        }

    loop ()

let time f =
    let proc = Process.GetCurrentProcess()
    let cpuTimeStamp = proc.TotalProcessorTime
    let timer = new Stopwatch()
    timer.Start()

    try
        f ()
    finally
        let cpuTime =
            (proc.TotalProcessorTime - cpuTimeStamp)
                .TotalMilliseconds

        let absoluteTime = timer.ElapsedMilliseconds |> float
        printfn "CPU time = %dms" (int64 cpuTime)
        printfn "Absolute time = %fms" absoluteTime
        printfn "Ratio = %f" (cpuTime / absoluteTime)

let startCompute N K W =
    let masterRef = spawn system "master" (masterActor W)
    masterRef <! DispatcherMessage(N, K)
    system.WhenTerminated.Wait()

let numberOfZeros =
    Environment.GetCommandLineArgs().[2] |> int

let numberOfWorkers = Environment.ProcessorCount
let stringLength = 16

printfn
    "Number of Zeros = %i\nNumber of Workers = %i\nMax String Length = %i"
    numberOfZeros
    numberOfWorkers
    stringLength

startCompute stringLength numberOfZeros numberOfWorkers

//time (fun () -> startCompute stringLength numberOfZeros numberOfWorkers)
