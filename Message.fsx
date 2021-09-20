#r "nuget: Akka.Serialization.Hyperion"
open Akka.Serialization


type Message =
    | DispatcherMessage of int * int
    | WorkerMessage of int * int
    | FinishMessage of string * string
    | RepeatMessage of int * int
    | GiveMeWorkMessage of string
    | PingMessage of string
