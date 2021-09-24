#load "Config.fsx"
#load "Message.fsx"


#r "nuget: Akka.Remote"
#r "nuget: Akka.FSharp"

open Config
open Message

open Akka.FSharp
open System
open System.Security.Cryptography


let args = Environment.GetCommandLineArgs()
printfn "%A" args
let K = args.[2] |> int
let W = Environment.ProcessorCount
let N = 16



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


let serverLocalSystem =
    System.create "server-local" (Configuration.load ())

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







let server (mailbox: Actor<_>) =
    let rec loop () =
        actor {
            let! message = mailbox.Receive()

            match message with
            | PingMessage (s) -> printfn "Got a ping! %s" s

            | GiveMeWorkMessage (s) ->
                printfn "Assigning work to %s" s
                mailbox.Sender() <! DispatcherMessage(N, K)
            | FinishMessage (normalString, hashString) ->
                printfn "\nString : %s\nHash : %s\n" normalString hashString
                mailbox.Context.System.Terminate() |> ignore
            | _ -> printfn "Weird Message"

            return! loop ()
        }

    loop ()

let remoteSystem = System.create "server" serverConfig
let serverRef = spawn remoteSystem "server" server



let masterActor (mailbox: Actor<_>) =
    let rec loop () =
        actor {
            let! message = mailbox.Receive()

            match message with
            | DispatcherMessage (N, K) ->
                printfn "Received work from server"

                let workersList =
                    [ for i in 1 .. W do
                          yield (spawn serverLocalSystem ($"slave{i}") slaveActor) ]

                workersList
                |> Seq.iter (fun worker -> printfn "%s" (worker.Path.ToString()))

                workersList
                |> Seq.iter (fun worker -> worker <! WorkerMessage(N, K))

            | FinishMessage (normalString, hashString) ->
                serverRef
                <! FinishMessage(normalString, hashString)

                mailbox.Context.System.Terminate() |> ignore

            | RepeatMessage (N, K) -> mailbox.Sender() <! WorkerMessage(N, K)

            | GiveMeWorkMessage (S) ->
                printfn "%s requested work from server" S

                serverRef <! GiveMeWorkMessage(S)

            | _ -> printfn "Weird message"

            return! loop ()
        }

    loop ()

let localRef =
    spawn serverLocalSystem "local-system" masterActor

localRef <! GiveMeWorkMessage(localRef.ToString())
serverRef <! PingMessage(localRef.ToString())

remoteSystem.WhenTerminated.Wait()
