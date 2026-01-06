using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharedLibrary.Networks
{
    public class TransmissionControlProtocolListener
    {
        public async IAsyncEnumerable<Memory<byte>> ListenAsync(IPAddress iPAddress,
            short port = 13, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var ipEndPoint = new IPEndPoint(iPAddress, Convert.ToInt32(port));
            TcpListener listener = new(ipEndPoint);

            try
            {
                listener.Start();

                using TcpClient handler = await listener.AcceptTcpClientAsync(cancellationToken);
                if(cancellationToken.IsCancellationRequested)
                {
                    handler.Close();
                    listener.Stop();
                    yield break;
                }
                await using NetworkStream stream = handler.GetStream();

                Memory<byte> buffer = new byte[1_024];
                int received = 0;

                try
                {
                    received = await stream.ReadAsync(buffer, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    stream.Close();
                    handler.Close();
                    listener.Stop();
                    yield break;
                }

                string message = $"📅 {DateTime.Now} 🕛";
                if (received > 0)
                {
                    var receivedMessage = Encoding.UTF8.GetString(buffer.Span.Slice(0, received));
                    message = receivedMessage;
                }
                try
                {
                    await stream.WriteAsync(Encoding.UTF8.GetBytes(message), cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    handler.Close();
                    stream.Close();
                    listener.Stop();
                    yield break;
                }
                if(cancellationToken.IsCancellationRequested)
                {
                    handler.Close();
                    stream.Close();
                    listener.Stop();
                    yield break;
                }
                yield return buffer;
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}
