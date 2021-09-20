module Config

#r "nuget: Akka.Remote"
#r "nuget: Akka.FSharp"
#r "nuget: Akka.Serialization.Hyperion"

open Akka.FSharp
open Akka.Configuration
open Akka.Serialization



let serverConfig =
    ConfigurationFactory.ParseString(
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
                                       serializers {
                                       hyperion = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
                                        }
                                        serialization-bindings {
                                                               ""System.Object"" = hyperion
                                        }
                                   }
                                   remote {
                                       helios.tcp {
                                           port = 6969
                                           hostname = 10.228.0.158
                                       }
                                   }
                               }"
    )



let clientConfig =
    ConfigurationFactory.ParseString
        @"akka {
                            actor {
                                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                                serializers {
                                hyperion = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
                                }
                                serialization-bindings {
                                                ""System.Object"" = hyperion
                                }

                            }
                            remote {
                                helios.tcp {
                                    port = 4209
                                    hostname = localhost
                                }
                            }
                        }"
