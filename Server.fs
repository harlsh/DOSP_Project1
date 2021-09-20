module Server

open Configuration
open Message
open Akka.FSharp


let server (mailbox: Actor<_>) = 
    let rec loop() = actor {
        let! message = mailbox.Receive()
        match message
        | PingMessage -> printfn "Got a ping!"
        | _ -> printfn "Weird Message"
        
    }
    loop()


let runServer =
    let remoteSystem = System.create "server" serverConfig
    spawn remoteSystem "server" server
    
    