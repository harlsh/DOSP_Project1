#time "on"
#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"

#r "nuget: Akka.Serialization.Hyperion"

open Akka.FSharp
open Akka.Serialization
open System
open Akka.Actor
open Akka.Configuration
open System.Security.Cryptography

let serverIp = fsi.CommandLineArgs.[1] |> string
let serverPort = fsi.CommandLineArgs.[2] |> string

let addr =
    "akka.tcp://RemoteFSharp@"
    + serverIp
    + ":"
    + serverPort
    + "/user/server"


let mutable count = 0L //to keep track of the workers

let workers =
    System.Environment.ProcessorCount |> int64

let mutable k = 4
let mutable miner = "UFID"

let configuration =
    ConfigurationFactory.ParseString(
        @"akka {
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                serializers {
                hyperion = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
                }
                serialization-bindings {
                            ""System.Object"" = hyperion
                }

                }
            remote {
                helios.tcp {
                    port = 4209
                    hostname = localhost
                }
            }
        }"
    )

let system =
    ActorSystem.Create("ClientFsharp", configuration)

let echoServer =
    spawn system "EchoServer"
    <| fun mailbox ->
        let rec loop () =
            actor {
                let! message = mailbox.Receive()
                let sender = mailbox.Sender()

                match box message with
                | :? string ->
                    printfn "super!"
                    sender <! sprintf "Hello %s remote" message
                    return! loop ()
                | _ -> failwith "unknown message"
            }

        loop ()


type Messages =
    | InitiateWorker
    | RequestJob
    | Job of string
    | Success of string * string
    | InitiateCoordinator

// Random string length
let strLength = 15

// random string generator
let ranStr () =
    let r = Random()

    let chars =
        Array.concat (
            [ [| 'a' .. 'z' |]
              [| 'A' .. 'Z' |]
              [| '0' .. '9' |] ]
        )

    let sz = Array.length chars in
    String(Array.init strLength (fun _ -> chars.[r.Next sz]))

// Get SHA256Encoding
let getSHA256Encoding (str: string) =
    System.Text.Encoding.ASCII.GetBytes(str)
    |> (SHA256.Create()).ComputeHash
    |> Array.map (fun (x: byte) -> System.String.Format("{0:x2}", x))
    |> String.concat String.Empty

// validate if the hash of the string contains the required pattern
let validateHash (hash: string) =
    let index =
        hash |> Seq.tryFindIndex (fun x -> x <> '0')

    index.IsSome && index.Value = k


let worker (mailbox: Actor<_>) =
    let rec loop () =
        actor {
            let! msg = mailbox.Receive()

            match msg with
            | InitiateWorker ->
                printfn "Worker requesting job"
                mailbox.Sender() <! RequestJob
            | Job (str) ->
                let hash = getSHA256Encoding (str)

                if validateHash (hash) then
                    mailbox.Sender() <! Success(str, hash)
                else
                    mailbox.Sender() <! RequestJob
            | _ -> printfn "Worker Received Wrong message"

            return! loop ()
        }

    loop ()

// template for Coordinator, it's responsible for distributing the jobs to workers
let Coordinator (mailbox: Actor<_>) =
    let mutable stringFound: bool = false

    let rec loop () =
        actor {
            let! msg = mailbox.Receive()

            match msg with
            | InitiateCoordinator ->
                printfn "Starting remote resource workers"

                let workersList =
                    [ for i in 1L .. workers do
                          yield (spawn system ("Worker_" + (string i)) worker) ]

                for i in 0L .. (workers - 1L) do
                    workersList.Item(i |> int) <! InitiateWorker
            | RequestJob ->
                if stringFound then
                    mailbox.Context.System.Terminate() |> ignore // terminate the coordinator
                else
                    let randomSting = miner + ranStr () // generate a random string
                    mailbox.Sender() <! Job(randomSting) // send the worker the new string to process
            | Success (str, hash) ->
                printfn "String found, sending reply to remote server"
                stringFound <- true
                let remoteServer = system.ActorSelection(addr)
                remoteServer <! str + "," + hash
                mailbox.Context.System.Terminate() |> ignore


            | _ -> printfn "Coordinator Received Wrong message"

            return! loop ()
        }

    loop ()

let mutable remoteWorkDone = false

let commlink =
    spawn system "client"
    <| fun mailbox ->
        let rec loop () =
            actor {
                let! msg = mailbox.Receive()
                printfn "%s" msg
                let response = msg |> string
                let command = (response).Split ','

                if command.[0].CompareTo("init") = 0 then
                    let remoteServer = system.ActorSelection(addr)
                    remoteServer <! "RemoteRequestJob"
                elif command.[0].CompareTo("Process") = 0 then
                    k <- (int command.[1])
                    miner <- command.[2]

                    let coordinatorRef =
                        spawn system "RemoteCoordinator" Coordinator

                    coordinatorRef <! InitiateCoordinator
                elif response.CompareTo("Completed") = 0 then
                    system.Terminate() |> ignore
                else
                    printfn "-%s-" msg

                return! loop ()
            }

        loop ()


commlink <! "init"


system.WhenTerminated.Wait()
