#load "Config.fsx"
#load "Message.fsx"

#r "nuget: Akka.Remote"
#r "nuget: Akka.FSharp"

open Config
open Message
open Akka.FSharp
open System


let server (mailbox: Actor<_>) = 
    let rec loop() = actor {
        let! message = mailbox.Receive()
        match message with
        | PingMessage -> printfn "Got a ping!"
        | _ -> printfn "Weird Message"
        
    }
    loop()


let remoteSystem = System.create "server" serverConfig
let serverRef = spawn remoteSystem "server" server
Console.ReadLine() |> ignore
    
    