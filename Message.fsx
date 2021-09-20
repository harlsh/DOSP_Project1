module Message
type MessageType = 
    | DispatcherMessage of int*int*int
    | WorkerMessage of int*int
    | FinishMessage of string*string
    | RepeatMessage
    | PingMessage
