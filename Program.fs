module Program

open System
open Akka.FSharp
open System.Security.Cryptography
open System.Diagnostics
open Message

let system = System.create "my-system" (Configuration.load ())
let mutable counter = 0L



let generateRandomString (length: int) =
    let characters = Array.concat (
               [ [| 'a' .. 'z' |]
                 [| 'A' .. 'Z' |]
                 [| '0' .. '9' |] ]
           )
    let sz = Array.length characters in
    "harishrebollavar" + String(Array.init length (fun _ -> characters.[Random().Next sz]))

let encodeToSHA256 (str: string) = 
    System.Text.Encoding.ASCII.GetBytes(str)
    |> (SHA256.Create()).ComputeHash
    |> Array.map (fun (x: byte) -> System.String.Format("{0:x2}", x))
    |> String.concat String.Empty

let hasKZeros (k: int) (hash: string) =
    let index =
        hash |> Seq.tryFindIndex (fun x -> x <> '0')

    index.IsSome && index.Value = k


let slaveActor(mailbox: Actor<_>) = 
    let rec loop() = 
        actor {
            let! message = mailbox.Receive()

            match message with
            | WorkerMessage(N, K) ->
                for i = 2 to N + 1 do
                    let S = generateRandomString i
                    let encodedString = S |> encodeToSHA256
                    if encodedString |> hasKZeros K then
                        mailbox.Sender() <! FinishMessage(S, encodedString)
                
                mailbox.Sender() <! RepeatMessage
            | _ -> printfn "Weird Message"

            return! loop()
        }
    loop()

let masterActor (N: int) (K: int) (mailbox: Actor<_>) = 
    let rec loop() =
        actor {
            let! message = mailbox.Receive()

            match message with
            | DispatcherMessage(N, K, W) ->
                let workersList = [ for i in 1 .. W do yield (spawn system ($"slave{i}") slaveActor) ]

                workersList
                |> Seq.iter (fun worker -> printfn "%s" (worker.Path.ToString()))
                workersList
                |> Seq.iter (fun worker -> worker <! WorkerMessage(N, K))

            | FinishMessage(normalString, hashString) ->
                printfn "\nString : %s\nHash : %s\n" normalString hashString
                mailbox.Context.System.Terminate() |> ignore

            | RepeatMessage -> 
                mailbox.Sender() <! WorkerMessage(N, K)

            | _ -> printfn "Weird message"

            return! loop()
        }
    loop()

let time f = 
    let proc = Process.GetCurrentProcess()
    let cpu_time_stamp = proc.TotalProcessorTime
    let timer = new Stopwatch()
    timer.Start()
    try
        f()
    finally
        let cpu_time = (proc.TotalProcessorTime-cpu_time_stamp).TotalMilliseconds
        let absolute_time = timer.ElapsedMilliseconds |> float
        printfn "CPU time = %dms" (int64 cpu_time)
        printfn "Absolute time = %fms" absolute_time
        printfn "Ratio = %f" (cpu_time/absolute_time)

let startCompute N K W =
    let masterRef = spawn system "master" (masterActor N K)
    masterRef <! DispatcherMessage(N, K, W)
    system.WhenTerminated.Wait()
    

[<EntryPoint>]
let main argv =
    
    let numberOfZeros = argv.[0] |> int
    let numberOfWorkers = argv.[1] |> int
    let stringLength = 16
    printfn "Number of Zeros = %i\nNumber of Workers = %i\nMax String Length = %i" numberOfZeros numberOfWorkers stringLength
    time(fun () -> startCompute stringLength numberOfZeros numberOfWorkers)

    0 // return an integer exit code