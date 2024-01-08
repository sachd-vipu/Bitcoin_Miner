#time "on"

#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit" 
#r "nuget: Akka.Remote"

open System
open Akka.FSharp
open Akka.Actor
open Akka.Remote

// Information that is needed to compute the finalString and hash
[<Struct>]
type ComputationMetadata = {
        CurrentString: string
        NumOfZeroes: int
        AllocatedLength: int
    }

// finalString and hash of that string
[<Struct>]
type Answer = {
        computedString: string
        computedHash: string
    }

// global variable that will check whether we got an answer or not
// if we got an answer it will be updated to 1 and the program will exit.
let mutable coinCount = 0

// checking whether the hash has k leading zeros or not
let checkKLeadingZeros (n:int) (str:string) =
    let mutable found = true
    for i in 0 .. n do
        if not (str.[i..i].Equals("0")) then
            found <- false
    found

// finding the random string that will be the suffix for initial string (Gator Id)
let rec findSha256for (n:int) (str:string) (count:int) (serverSideActor:IActorRef)=
    if count > 0 then
        let newcount = count-1
        for (i:char) in char(32) .. char(126) do
            let currStr = str + i.ToString()
            findSha256for n currStr newcount serverSideActor
    else
        // Calculate sha for current string and check for 0s
        let strToBytes = System.Text.Encoding.ASCII.GetBytes(str)
        let strToBytesSha256 = System.Security.Cryptography.SHA256.Create().ComputeHash(strToBytes)
        let hex = Array.map (fun (x : byte) -> System.String.Format("{0:X2}", x)) strToBytesSha256
        let hash = String.concat System.String.Empty hex
        if checkKLeadingZeros n hash then
            let output = {computedString = str; computedHash = hash}
            serverSideActor <! output

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
            a <! true

        | _ -> ()
            
        return! loop()
    }
    loop()

// making the connection with remote
let config =
    // Change Server IP here
    Configuration.parse 
        @"akka {
                actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                remote.helios.tcp {
                                    hostname = ""127.0.0.1""
                                    port = 5000
                                   }
                }"

let system = System.create "AkkaRemote" config

// function to convert uppercase to lowercase
let lowercase (str : string) = str.ToLower()

// boss
let echoServer = 
    spawn system "AkkaServer"
        (fun mailbox ->
            let mutable totalLength = 0
            let mutable currentLength = 0
            let mutable numberOfZeros = 0
            let mutable prefixString = ""
            let mutable count = 0;
            let rec loop() = actor {

                let! message = mailbox.Receive()
                let sender = mailbox.Sender()
                //printfn "message received"
                match box message with

                | :? Answer as ans ->
                    if coinCount = 0 then
                        //printfn "Got an Answer!"
                        // converting the hash to lowercase
                        let hash = ans.computedHash |> lowercase
                        let finalString = ans.computedString + "    " + hash
                        printfn "%s" finalString
                        // updating the global variable to 1 so that the program exits
                        // coinCount <- coinCount + 1

                | :? string as message -> 
                    count <- count + 1

                | :? bool as signal ->
                    if signal && currentLength < totalLength then
                        currentLength <- currentLength + 1
                        let record = prefixString + "\n" + currentLength.ToString() + "\n" + numberOfZeros.ToString()
                        sender <! record
                 
                | :? ComputationMetadata as message ->
                    totalLength <- message.AllocatedLength
                    numberOfZeros <- message.NumOfZeroes
                    prefixString <- message.CurrentString
                    //printfn "assigned length %i for %i 0s for gatorID %s" totalLength numberOfZeros prefixString
                    count <- 0

                | _ -> ()
                // return! loop()
                return! loop()
            }
            loop()
        )

// taking the value of k from the command line
// entry point for the program
let  input = fsi.CommandLineArgs
let num0 = input.[1] |> int
if num0 < 1 then num0 = 1
else num0 = num0

// max additional string length
let lengthRange = 15 

// initial string that is gatorlink id
let initialString = "parthgupta"

let tempRecord = {CurrentString=initialString; AllocatedLength=lengthRange; NumOfZeroes = num0-1}
echoServer <! tempRecord

// number of working actors in server
let ServerWorkforceActors = 2 

let serverActorsArr = [
    for i in 1 .. ServerWorkforceActors do
        let name = "ServerSideActor" + i.ToString()
        let actorWork = 
            spawn system name hashActor_worker
        yield actorWork
]

// sending the server actors to the server
let server = select "akka://AkkaRemote/user/AkkaServer" system

for i in 0 .. (serverActorsArr.Length - 1) do
    serverActorsArr.Item(i) <! server

// continue until you get an answer    
while coinCount = 0 do
    System.Threading.Thread.Sleep(2000)

#time "off"

// exit the program and return 0
0