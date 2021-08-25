using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncNetworking {
    public class AsyncClient : IDisposable {
        public CancellationTokenSource ShutdownToken { get; private set; }
        public TcpClient Client { get; private set; }
        private bool Disposed { get; set; }

        public AsyncClient(TcpClient client) {
            ShutdownToken = new CancellationTokenSource();
            Client = client;
            Disposed = false;
        }

        ~AsyncClient() => Dispose();

        public void Dispose() {
            if (Disposed) return;
            ShutdownToken.Dispose();
            GC.SuppressFinalize(this);
            Disposed = true;
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
