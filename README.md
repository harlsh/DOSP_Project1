# DOSP_Project1
Hello future UF grad students, this is my first time writing code in F# but I atleast wrote it better than this guy https://github.com/sanjay105/ lul

To run the program with no remote actors
```bash
dotnet fsi Program.fsx <Number of Zeroes you want in the output>
dotnet fsi Program.fsx 5
```

To run the program in a  distributed manner, make sure that all the machines are under one LAN (use UF VPN if you want to connect alot of them),
First run the server
```bash
dotnet fsi Server.fsx <Number of Zeroes you want in the output>
dotnet fsi Server.fsx 5
```
Then run the client 
```bash
dotnet fsi Client.fsx <ip:port> 
```
The server(master) will just keep distributing the workload to the clients(slaves).
