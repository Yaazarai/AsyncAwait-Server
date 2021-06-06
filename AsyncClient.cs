using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace AsyncNetworking {
    public class AsyncClient {
        public CancellationTokenSource ShutdownToken { get; private set; }
        public TcpClient Client { get; private set; }

        public AsyncClient(TcpClient client) {
            ShutdownToken = new CancellationTokenSource();
            Client = client;
        }

        ~AsyncClient() {
            Client.Close();
            ShutdownToken.Dispose();
        }

        public void TryShutdown() {
            if (!ShutdownToken.IsCancellationRequested) {
                Client.Client.Disconnect(true);
                ShutdownToken.Cancel();
            }
        }

        public async Task SendAsync(byte[] buffer, int count = 0) =>
            await Client.GetStream().WriteAsync(buffer, 0, Math.Max(0, Math.Min(count, buffer.Length)));
    }

    public class ClientEventArgs : EventArgs {
        public object ClientObject { get; private set; }
        public int PacketID { get; private set; }
        public byte[] Buffer { get; private set; }
        public ClientEventArgs(object client, byte[] buffer) { ClientObject = client; Buffer = buffer; }
        public static new ClientEventArgs Empty { get { return new ClientEventArgs(null, null); } }
    }
}
