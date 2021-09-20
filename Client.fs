module Client

open Configuration
open Message
open Akka.FSharp


let runClient serverIp serverPort =
    let clientSystem = System.create "client" clientConfig
    
    select ($"akka.tcp://server@{serverIp}:{serverPort}/user/server") clientSystem
    