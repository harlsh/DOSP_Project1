module Config
#r "nuget: Akka.Remote"
#r "nuget: Akka.FSharp"

open Akka.FSharp
open Akka.Configuration

let serverConfig = ConfigurationFactory.ParseString(
                        @"akka {
                                   actor {
                                       provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                                       debug : {
                                           receive : on
                                           autoreceive : on
                                           lifecycle : on
                                           event-stream : on
                                           unhandled : on
                                       }
                                   }
                                   remote {
                                       helios.tcp {
                                           port = 9001
                                           hostname = localhost
                                       }
                                   }
                               }")

let clientConfig = ConfigurationFactory.ParseString
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



