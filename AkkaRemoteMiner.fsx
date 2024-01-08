#time "on"

#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit" 
#r "nuget: Akka.Remote"

open Akka.FSharp
open Akka.Actor
open Akka.Configuration

// checking whether the hash has k leading zeros or not
let checkKLeadingZeros (n:int) (str:string) =
    let mutable found = true
    for i in 0 .. n do
        if not (str.[i..i].Equals("0")) then
            found <- false
    found

// finding the random string that will be the suffix for initial string (Gator Id)
let rec findSha256for (n:int) (str:string) (count:int) (clientSideActor:IActorRef)=
    if count > 0 then
        let newcount = count-1
        for (i:char) in char(32) .. char(126) do
            let currStr = str + i.ToString()
            findSha256for n currStr newcount clientSideActor
    else
        // Calculate sha for current string and check for 0s
        let strToBytes = System.Text.Encoding.ASCII.GetBytes(str)
        let strToBytesSha256 = System.Security.Cryptography.SHA256.Create().ComputeHash(strToBytes)
        let hex = Array.map (fun (x : byte) -> System.String.Format("{0:X2}", x)) strToBytesSha256
        let hash = String.concat System.String.Empty hex
        if checkKLeadingZeros n hash then
            let output = str + "\t" + hash
            clientSideActor <! output

// worker actor
let hashActor_worker (mailbox: Actor<_>) =
    let rec loop() = actor {

        let! message = mailbox.Receive()
        let sender = mailbox.Sender()

        match box message with

        | :? int as x -> 
            printfn ""

        | :? string as x -> 
            let s = x.Split '\n'
            let currString = s.[0]
            let value = s.[1] |> int
            let numberOfZeros = s.[2] |> int
            let str = mailbox.Self.Path.Name
            sender <! str
            findSha256for numberOfZeros currString value sender
            sender <! true

        | :? ActorSelection as a ->
            printfn "%s is now activated" mailbox.Self.Path.Name
            a <! true

        | _ -> ()
            
        return! loop()
    }
    loop()


// establishing the connection with server
let configuration = 
    ConfigurationFactory.ParseString(
        @"akka {
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                deployment {
                    /remoteecho {
                        remote = ""akka.tcp://AkkaRemote@127.0.0.1:5000""
                    }
                }
            }
            remote {
                helios.tcp {
                    port = 0
                    hostname = ""127.0.0.1""
                }
            }
        }")

let system = ActorSystem.Create("AkkaRemote", configuration)

// localhost
let ip = "127.0.0.1"

// number of working actors in client
let numOfActors = 6 
let actorList = [
    for i in 1 .. numOfActors do
        let name = "ClientActor" + i.ToString()
        let temp = 
            spawn system name hashActor_worker
        yield temp
]

let address = "akka.tcp://AkkaRemote@" + ip + ":5000/user/AkkaServer"

let server = system.ActorSelection(address)

for i in 0 .. (actorList.Length - 1) do
    actorList.Item(i) <! server


while true do
    System.Threading.Thread.Sleep(200)

#time "off"

// exit the program and return 0
0