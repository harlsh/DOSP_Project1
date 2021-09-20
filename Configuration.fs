module Configuration

open Akka.FSharp

let serverConfig = Configuration.parse
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

let clientConfig = Configuration.parse
                        @"akka {
                            actor {
                                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            
                            }
                            remote {
                                helios.tcp {
                                    port = 2552
                                    hostname = localhost
                                }
                            }
                        }"



