#load "Config.fsx"
#load "Message.fsx"

#r "nuget: Akka.Remote"
#r "nuget: Akka.FSharp"


open Config
open Message
open Akka.FSharp

let serverIp = "192.168.14.1"
let serverPort = "9001"
let clientSystem = System.create "client" clientConfig
select ($"akka.tcp://server@{serverIp}:{serverPort}/user/server") clientSystem
serveRef <! PingMessage(10)
Console.ReadLine()
