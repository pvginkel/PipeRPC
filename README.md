
# PipeRpc

Apache License 2.0

[Download from NuGet](https://www.nuget.org/packages/PipeRpc).

## Introduction

PipeRpc is a library to simplify communication with external processes. It was designed as a simple replacement to `AppDomain`'s.

## Example

To setup PipeRpc, you need a server process and a client process. The server process is the process that starts the client application to communicate with.

A simple server implementation looks as follows:

```csharp
// Program.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PipeRpc;

namespace SampleServer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            using (var server = new PipeRpcServer())
            {
                // Start the server with the location and arguments to the RPC client.
                
                server.Start(new ClientStartInfo(
                    typeof(SampleClient.Program).Assembly.Location,
                    null,
                    server.Handle.ToString()
                ));

                // Invoke a method on the client service.

                int result = server.Invoke<int>("Add", 1, 2);

                // Write the results.

                Console.WriteLine($"1 + 2 = {result}");
            }
        }
    }
}
```

This hosts an RPC service and ensures that the RPC client is started automatically. The client looks as follows:

```csharp
// Program.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PipeRpc;

namespace SampleClient
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            // Start the RPC client with the handle to the RPC server
            // and an instance of the service to host.

            using (var client = new PipeRpcClient(PipeRpcHandle.FromString(args[0]), new Service()))
            {
                client.Run();
            }
        }
    }
}
```

```csharp
// Service.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleClient
{
    public class Service
    {
        public int Add(int a, int b)
        {
            return a + b;
        }
    }
}
```

The entry point of the RPC client starts an instance of the `PipeRpcClient` with the handle created by the server. The service itself is just an instance of a class with some public methods. PipeRpc will automatically find these methods and make them available to be called from the server.

## Features

PipeRpc supports the following features:

* Calling methods with complex arguments. Newtonsoft.Json is used to serialize data between the RPC server and client, and supports a wide range of data types;
* Cancelling running request. If a `CancellationToken` is provided as an argument to `Invoke`, and the service takes a `CancellationToken`, cancellation request are automatically marshaled to the RPC client;
* Post back events. Events can be registered on an `PipeRpcServer` using the `On` method. If the service method takes an `IOperationContext` parameter, this can be used to post an event back. This can e.g. be used for progress reporting;
* Running the RPC client in process. During development, the RPC client can be run in process in an `AppDomain` by constructing the `PipeRpcServer` with the value `PipeRpcServerMode.Local`. This greatly simplifies debugging the RPC client.

Switching between local and remote mode can be done as follows:
```csharp
#if DEBUG
var mode = PipeRpcServerMode.Local;
#else
var mode = PipeRpcServerMode.Remote;
#endif
using (var server = new PipeRpcServer(mode))
{
    // ...
```

## Performance

PipeRpc is built to be fast. Below is a run of the benchmark suite part of the project:

```
              Method |   Mode |      Mean |      Error |     StdDev |
-------------------- |------- |----------:|-----------:|-----------:|
 ReturnComplexObject |  Local |  53.83 us |  4.2649 us |  1.1078 us |
 ReturnComplexObject | Remote |  53.40 us |  2.4947 us |  0.6480 us |
        Cancellation |  Local |  50.21 us |  7.2700 us |  1.8884 us |
        Cancellation | Remote |  50.40 us |  2.0542 us |  0.5336 us |
           ReturnInt |  Local |  27.42 us |  0.9550 us |  0.2481 us |
           ReturnInt | Remote |  28.98 us |  1.7416 us |  0.4524 us |
```
