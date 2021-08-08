# AsyncAwait-Server
TCP server written using the async/await pattern for efficiency.

Create a new TCP server with attached parameters (and optional parameters):
```C#
AsyncServer(int clientBufferSize, IPEndPoint ipPort,
    bool noDelay = false,
    bool sharedBufferPool = false,
    bool separatePackets = false)
    
/*
    SharedBufferPool : The server uses an ArrayPool<byte> for creating buffers to avoid fragmentation.
        This parameter decides whether the bufferpool should be a shared resource or be newly allocated.
    SeparatePackets : The server can now handle Nagle's algorithm by reading the first byte of each message
        to figure out the size of messages sent to separate out grouped messages (max message size is 256 bytes when enabled).
        ** If disabled you may receive messages grouped together in a single buffer that you'll need to handle on your own.
*/
```
On the thread that you wish to execute the server on call:
```C#
server.Listen();
```
If you wish to close the server or a client safely you can send a request to shutdown:
```C#
server.TryShutdown();
or
client.TryShutdown();
```

---
The server has several asynchronous events that you can subscribe calling methods to: (server events) `Startup`, `Shutdown`, `Failed` and (client events) `Connected`, `Disconnected`, `DataReceived`:
```C#
public event Func<object, ServerDataEventArgs, CancellationToken, Task>
    Startup, Shutdown, Failed, Connected, Disconnected, DataReceived;

/*
Example Usage:
server.Startup += OnStartup;

public static async Task OnStartup(object server, ServerDataEventArgs args, CancellationToken tkn) {
    Console.WriteLine("Startup Successful");
    await Task.FromResult(0);
}
*/
```
These asynchronous events will have an instance of `ServerDataEventArgs` which will always contain the server object which threw the event (on event call: `Startup`, `Shutdown` or `Failed`) and will contain the client and buffer which threw the event when appropriate (on event call: `Connected`, `Disconnected` or `Received`).

Just FYI when you subscribe a calling method to `Received` you'll receive a buffer with data packet(s) in it. Due to Nagle's algorithm this buffer may contain multiple packets sent from a single client iF `SeparatePackets` is set to `false`. If `SeparatePackets` is set to `true` then the buffer will separate out each message by reading out the first message byte as the size of the message and jumping through the buffer finding subsequent messages.

The buffer will be automatically disposed/returned when the `DataReceived` event returns.

---
When you wish to send data to a specific `AsyncClient` you can do the following. The `bytes` parameter is optional, specify if you want to send a certain number of bytes or exclude if you want to send the entire buffer:
```C#
_ = client.SendAsync(buffer, bytes);
or
_ = client.SendAsync(buffer);
or
await client.SendAsync(buffer, bytes);
or
await client.SendAsync(buffer);
```

---
The server keeps a running thread-safe collection of `AsyncClients` of the type `ConcurrentDictionary<T,T>` (where T is the AsyncClient or inherited class). The collection of clients is not ordered per specification of ConcurrentDictionary. If you wish to get a list of all clients: (this IEnumerator is a shallow copy of the original collection for thread safety):
```
IEnumerator<KeyValuePair<T,T>> clientList = server.Clients.GetEnumerator();
```
