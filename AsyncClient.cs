using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncNetworking {
    public class AsyncClient {
        public CancellationTokenSource ShutdownToken { get; private set; }
        public TcpClient Client { get; private set; }

        public AsyncClient(TcpClient client) {
            ShutdownToken = new CancellationTokenSource();
            Client = client;
        }

        ~AsyncClient() {
            TryShutdown();
            ShutdownToken?.Dispose();
        }

        public void TryShutdown() {
            if (!ShutdownToken.IsCancellationRequested) {
                Client.Client?.Close();
                ShutdownToken?.Cancel();
            }
        }

        public async Task SendAsync(byte[] buffer, int count = 0) =>
            await Client.GetStream().WriteAsync(buffer, 0, Math.Max(0, Math.Min(count, buffer.Length))).ConfigureAwait(false);
    }
}
