module Configuration

open System
open Akka.FSharp
open Akka.Configuration

let configuration = Configuration.parse
                        @"akka {
                        actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                        debug : {
                        receive : on
                        autoreceive : on
                        lifecycle : on
                        event-stream : on
                        unhandled : on
                        }
                        remote.helios.tcp {
                            hostname = localhost
                            port = 9001
                        }
        }"

let server (mailbox: Actor<_>) = 
    let rec loop() = actor {
        let! message = mailbox.Receive()
        printfn "%s" message
    }
    loop()


[<EntryPoint>]
let main argv =
    let remoteSystem = System.create "server" configuration
    let serveRef = spawn remoteSystem "server" server
    Console.ReadLine() |> ignore
    0

