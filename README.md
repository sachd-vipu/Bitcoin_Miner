# Akka Remote Miner - Hop On Hop Off
This project contains two F# scripts, AkkaRemoteMiner.fsx and AkkaServerMiner.fsx, which implement a distributed system for finding a SHA256 hash with a specified number of leading zeros. The system uses the Akka.NET actor model framework to distribute the computation across multiple actors.

# How it works
The system consists of a server and multiple client actors. The server assigns a portion of the computation to each client actor. Each actor then computes SHA256 hashes for its assigned portion and sends any hashes that meet the criteria back to the server. The clients can dynamically join or leave the network, hence the name hop-on hop off

# Files
AkkaRemoteMiner.fsx: This script contains the client-side actors. Each actor computes SHA256 hashes for a portion of the possible inputs and sends any hashes that meet the criteria back to the server.

AkkaServerMiner.fsx: This script contains the server-side actor. The server assigns a portion of the computation to each client actor and collects the results.

# Running the scripts
To run the scripts, you'll need to have F# and the Akka.NET NuGet packages installed. You can run the scripts using the F# interactive (fsi) command-line tool.

First, start the server script:

Then, in a separate terminal window, start the client script

The output print all the possible bitcoins after mining with k leading zeros, but for the purpose of printing only one answer, coinCount can be updated to 1 in 'AkkaServerMiner.fsx'.
- k is some integer >= 0
- dotnet fsi AkkaServerMiner.fsx k
- dotnet fsi AkkaRemoteMiner.fsx

## What and How?

- The project consists of two files first AkkaServerMiner.fsx and second AkkaRemoteMiner.fsx. Initially a server starts while creating a remote configuration port, where the other actors that is from client can connect too.
- Server then itself stars with two actors at server side (assuming two cores of the physical machine) and server will now have two working actors.
- The echoServer function in the AkkaServerMiner.fsx receives the metadat such as number of zeros required to create a hash, the initial string that is gator ID, and the allocated length.
- Now the boss system is created inside the echoServer who recursively calls a loop to receive output from various actors (including client side as well).
- The message received from these actors is now matched with the type of output that is expected and the action is taken according to the validity of the answer. For example [Answer, ComputationalMetaData, and string message from actors].
- When a signal is received from an actor it sends back a response to the sender actor to start working with a given length again, it meant it starts working with some allocated length.
- Now the actor hashActor_worker calls the findSha256for method along with serverSideActor reference. After this when a hash is computed for many random strings and matched with the requirement of k leading zeros (this is achieved by calling checkKLeadingZeros method). If we get the required answer the correct response is sent back to the server as answer and the answer is printed with a tab and then a hash of that string.

# Configuration
You can configure the number of client actors and the range of characters to consider in the AkkaRemoteMiner.fsx script. In the AkkaServerMiner.fsx script, you can configure the number of leading zeros to look for and the maximum length of the string to hash.

### Input

The input provided (as command line to yourproject1.fsx) will be, the required number of 0’s of the bitcoin.
kais
### Output

Print, on independent entry lines, the input string, and the corresponding SHA256 hash separated by a TAB, for each of the bitcoins you find. Obviously, your SHA256 hash must have the required number of leading 0s (k= 3 means3 0’s in the hash notation).  An extra requirement, to ensure every group finds different coins, is to have the input string prefixed by the gator link ID of one of the team members.

## Ratio of CPU to Real Time to check Parallelism

Note
This is a distributed system, and as such, network latency and other factors can affect performance. The system is designed as a dummy project to tun on a single machine for demonstration purposes, but it could be adapted to run on multiple machines.


