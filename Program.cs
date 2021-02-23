using Microsoft.VisualStudio.Threading;
using Nerdbank.Streams;
using StreamJsonRpc;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace JsonRpcClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Canceling...");
                cts.Cancel();
                e.Cancel = true;
            };

            try
            {
                Console.WriteLine("Press Ctrl+C to end.");
                await MainAsync(cts.Token);
            }
            catch (Exception e)
            {

            }
        }

        static async Task MainAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Connecting to web socket...");
            using (var socket = new ClientWebSocket())
            {
                await socket.ConnectAsync(new Uri("ws://localhost:7919"), cancellationToken);
                Console.WriteLine("Connected to web socket. Establishing JSON-RPC protocol...");

                // to work with 'vscode-jsonrpc' server, need to use this handler
                // refer to: https://github.com/microsoft/vs-streamjsonrpc/blob/main/doc/extensibility.md
                var handler = new HeaderDelimitedMessageHandler(socket.AsStream());
                var jsonRpc = new JsonRpc(handler);
                try
                {
                    jsonRpc.StartListening();
                    Console.WriteLine("JSON-RPC protocol over web socket established.");

                    await jsonRpc.NotifyAsync("testNotification", "Hello from dotnet");

                    object[] param = { 1, 2 };
                    var result = await jsonRpc.InvokeWithCancellationAsync<int>("Add", param, cancellationToken);
                    Console.WriteLine($"JSON-RPC server says 1 + 2 = {result}");

                    await jsonRpc.Completion.WithCancellation(cancellationToken);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client Closing", CancellationToken.None);
                    throw;
                }
            }
        }

    }
}
