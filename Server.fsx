#load "Config.fsx"
#load "Message.fsx"


#r "nuget: Akka.Remote"
#r "nuget: Akka.FSharp"

open Config
open Message
open Akka.FSharp
open System

let args = Environment.GetCommandLineArgs()
printfn "%A" args
let K = args.[2] |> int

let N = 16


let server (mailbox: Actor<_>) =
    let rec loop () =
        actor {
            let! message = mailbox.Receive()

            match message with
            | PingMessage (s) -> printfn "Got a ping! %s" s

            | GiveMeWorkMessage (s) ->
                printfn "Assigning work to "
                mailbox.Sender() <! DispatcherMessage(N, K)
            | FinishMessage (normalString, hashString) ->
                printfn "\nString : %s\nHash : %s\n" normalString hashString
                mailbox.Context.System.Terminate() |> ignore
            | _ -> printfn "Weird Message"

            return! loop ()
        }

    loop ()

let remoteSystem = System.create "server" serverConfig
spawn remoteSystem "server" server
remoteSystem.WhenTerminated.Wait()
