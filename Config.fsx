module Config

#r "nuget: Akka.Remote"
#r "nuget: Akka.FSharp"
#r "nuget: Akka.Serialization.Hyperion"

open Akka.FSharp
open Akka.Configuration
open Akka.Serialization
open System.Net.NetworkInformation
open System.Net
open System.Net.Sockets

let localIpAddress =
    let networkInterfaces =
        NetworkInterface.GetAllNetworkInterfaces()
        |> Array.filter (fun iface -> iface.OperationalStatus.Equals(OperationalStatus.Up))

    let addresses =
        seq {
            for iface in networkInterfaces do
                for unicastAddr in iface.GetIPProperties().UnicastAddresses do
                    yield unicastAddr.Address
        }

    addresses
    |> Seq.filter (fun addr -> addr.AddressFamily.Equals(AddressFamily.InterNetwork))
    |> Seq.filter (IPAddress.IsLoopback >> not)
    |> Seq.head

let localhost = localIpAddress.ToString()



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
                        hostname = "
        + localhost
        + "
                            }
                        }
               }"
    )



let clientConfig =
    ConfigurationFactory.ParseString(
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
                      port = 8778
                      hostname = "
        + localhost
        + "
                  }
              }
          }"
    )
