using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncNetworking {
    public class AsyncServer<T> where T : AsyncClient {
        public TcpListener Listener { get; private set; }
        public CancellationTokenSource ShutdownToken { get; private set; }
        public IPEndPoint EndPoint { get; private set; }
        public bool SeparatePackets { get; private set; }
        public int BufferSize { get; private set; }
        public ArrayPool<byte> BufferPool { get; private set; }
        public ConcurrentDictionary<T, byte> Clients { get; private set; }
        public event Func<object, ServerDataEventArgs, CancellationToken, Task> Startup, Shutdown, Failed, Connected, Disconnected, Received;

        public AsyncServer(int clientBufferSize, IPEndPoint ipPort, bool noDelay = false, bool sharedBufferPool = false, bool separatePackets = false) {
            Listener = new TcpListener(EndPoint = ipPort);
            ShutdownToken = null;
            Listener.Server.NoDelay = noDelay;
            BufferPool = (sharedBufferPool) ? ArrayPool<byte>.Shared : ArrayPool<byte>.Create();
            BufferSize = clientBufferSize;
            Clients = new ConcurrentDictionary<T,byte>();
            SeparatePackets = separatePackets;
        }

        public async Task Listen() {
            try {
                await Startup.InvokeAsync(this, ServerDataEventArgs.Empty, ShutdownToken.Token).ConfigureAwait(false);
                Listener.Start();

                if (ShutdownToken != null) ShutdownToken.Dispose();
                ShutdownToken = new CancellationTokenSource();

                using (ShutdownToken.Token.Register(() => Listener.Stop()))
                    while (!ShutdownToken.IsCancellationRequested)
                        _ = Accept((T)Activator.CreateInstance(typeof(T), await Listener.AcceptTcpClientAsync())).ConfigureAwait(false);
            } catch (Exception) {
                await Failed.InvokeAsync(this, ServerDataEventArgs.Empty, ShutdownToken.Token).ConfigureAwait(false);
            }

            await TryShutdown(true).ConfigureAwait(false);
        }

        private async Task Accept(T client) {
            Clients.TryAdd(client, default);
            await Connected.InvokeAsync(this, new ServerDataEventArgs(this, client), client.ShutdownToken.Token).ConfigureAwait(false);
            
            try {
                using (ShutdownToken.Token.Register(() => client.TryShutdown())) {
                    byte[] rcvBuffer = BufferPool.Rent(BufferSize);

                    while (!client.ShutdownToken.IsCancellationRequested) {
                        int bytes = await client.Client.GetStream().ReadAsync(rcvBuffer, 0, rcvBuffer.Length, client.ShutdownToken.Token).ConfigureAwait(false);

                        if (bytes > 0) {
                            if (SeparatePackets) {
                                // Accounts for Naggle's algorithm and generates multiple packets based on first byte (size) of each packet.
                                // NOTE: This requires that the very first byte of EVERY packet start with a size with type a of byte.
                                for (int size = 0, i = 0; i < bytes; i += size) {
                                    size = (byte)(Math.Min(rcvBuffer[i], bytes));
                                    byte[] buffer = BufferPool.Rent(size);
                                    Buffer.BlockCopy(rcvBuffer, i, buffer, 0, size);
                                    Task received = Received.InvokeAsync(this, new ServerDataEventArgs(this, client, buffer), client.ShutdownToken.Token);
                                    _ = received.ContinueWith(_ => { BufferPool.Return(buffer); });
                                }
                            } else {
                                //Due to Nagle's Algorithm multiple packets may be received on each ReadAsync().
                                //Always check each buffer on DataReceived.InvokeAsync() for multiple packets.
                                byte[] buffer = BufferPool.Rent(Math.Min(BufferSize, bytes));
                                Buffer.BlockCopy(rcvBuffer, 0, buffer, 0, bytes);
                                Task received = Received.InvokeAsync(this, new ServerDataEventArgs(this, client, buffer), client.ShutdownToken.Token);
                                _ = received.ContinueWith(_ => { BufferPool.Return(buffer); });
                            }
                        }
                    }

                    BufferPool.Return(rcvBuffer);
                }
            } catch (Exception) { /* Catch exceptions from client ReadAsync() any exceptions thrown means disconnected client. */ }

            client.TryShutdown();
            await Disconnected.InvokeAsync(this, new ServerDataEventArgs(this, client), client.ShutdownToken.Token).ConfigureAwait(false);
            Clients.TryRemove(client, out byte _);
        }

        public async Task TryShutdown(bool shutdownEvent) {
            if (!ShutdownToken.IsCancellationRequested) {
                if (shutdownEvent)
                    await Shutdown.InvokeAsync(this, ServerDataEventArgs.Empty, ShutdownToken.Token).ConfigureAwait(false);
                
                foreach (var client in Clients) client.Key.TryShutdown();
                ShutdownToken.Cancel();
            }
        }

        ~AsyncServer() {
            Task.WaitAll(TryShutdown(false));
            ShutdownToken.Dispose();
            Listener.Server.Dispose();
        }
    }

    public class ServerDataEventArgs : EventArgs {
        public object ServerObject { get; private set; }
        public object ClientObject { get; private set; }
        public byte[] DataBuffer { get; private set; }
        public ServerDataEventArgs(object server, object client = null, byte[] dataBuffer = null) { ServerObject = server; ClientObject = client; DataBuffer = dataBuffer; }
        public static new ServerDataEventArgs Empty { get { return new ServerDataEventArgs(null); } }
    }
}
