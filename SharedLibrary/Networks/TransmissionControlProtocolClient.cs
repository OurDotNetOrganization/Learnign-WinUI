using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SharedLibrary.Networks
{
    public class TransmissionControlProtocolClient
    {
        public async IAsyncEnumerable<Memory<byte>> ListenAsync(long ipAddress,
            short port = 13,
            bool reuseSocket = false,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, Convert.ToInt32(port));
            using TcpClient client = new();
            try
            {
                await client.ConnectAsync(ipEndPoint);
                while (client.Connected)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        await client.Client.DisconnectAsync(reuseSocket, cancellationToken);
                        yield return Memory<byte>.Empty;
                        yield break;
                    }
                    await using NetworkStream stream = client.GetStream();

                    Memory<byte> buffer = new byte[1_024];
                    int received = 0;
                    try
                    {
                        received = await stream.ReadAsync(buffer, cancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                        stream.Close();
                        await client.Client.DisconnectAsync(reuseSocket, cancellationToken);
                        yield break;
                    }
                    if (cancellationToken.IsCancellationRequested)
                    {
                        stream.Close();
                        await client.Client.DisconnectAsync(reuseSocket, cancellationToken);
                        yield return Memory<byte>.Empty;
                        yield break;
                    }
                    yield return buffer;
                }
                yield return Memory<byte>.Empty;
            }
            finally
            {
                client.Close();
            }
        }
    }
}
