# AsyncAwait-Server
TCP server written using the async/await pattern for efficiency.

Create a new TCP server with attached parameters (and optional parameters):
```C#
AsyncServer<AsyncClient> server = AsyncServer(int clientBufferSize, IPAddress address, int port,
    int maxClients = int.MaxValue, bool sharedBufferPool = false);
```
On the thread that you wish to execute the server on call:
```C#
server.Listen();
```
If you wish to close the server or a client safely you can send a request to the server to shutdown:
```C#
server.TryShutdown();
or
client.TryShutdown();
```
If you wish to get a list of all clients: (this IList is a shallow copy of the original list for thread safety)
```
IList<T> clients = server.Clients.TryEnumerator();
```
The server has several asynchronous events that you can subscribe calling methods to: (server events) `Startup`, `Shutdown`, `Failed` and (client events) `Connected`, `Disconnected`, `DataReceived`:
```C#
public event Func<object, ServerEventArgs, CancellationToken, Task> Startup, Shutdown, Failed;
public event Func<object, ClientEventArgs, CancellationToken, Task> Connected, Disconnected, DataReceived;

/*
Example Usage:
server.Startup += OnStartup;

public static async Task OnStartup(object server, ServerEventArgs args, CancellationToken tkn) {
    Console.WriteLine("Startup Successful");
    await Task.FromResult(0);
}
*/
```
These asynchronous events will have two types of arguments `ServerEventArgs` (which will return ServerEventArgs.Empty by default) or `ClientEventArgs` which will contain the client that threw the event and the buffer with the data received.

Just FYI when you subscribe a calling method to `DataReceived` you'll receive a buffer with data packet(s) in it. Due to Nagle's algorithm this buffer may contain multiple packets sent from a single client. This will still happen even if you manage to disable Nagle's algorithm. The buffer will be automatically disposed/returned when the `DataReceived` event returns.

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
Finally if needed, `LockedList<T>` is a simple wrapper to create a basic thread-safe `List<T>`. The server class `AsyncServer` implements this class under the property `Clients` for thread-safe client tracking--you can call `server.Clients.TryEnumerator()` to get a shallow copy of the clients for enumeration.

Only the provided methods are thread-safe:
```
list.TryAdd(T object);
bool itemWasRemoved = list.TryRemove(T object);
int itemIndex = list.TryFindIndex(T object);
IList<T> shallowCopy = list.TryEnumerator();
```
