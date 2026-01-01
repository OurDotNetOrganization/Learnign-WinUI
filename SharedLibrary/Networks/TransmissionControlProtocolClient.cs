using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace SharedLibrary.Networking
{
    public class TransmissionControlProtocolClient
    {
        public async IAsyncEnumerable<string> ListenAsync(long ipAddress,
            short port = 13,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, Convert.ToInt32(port));

            using TcpClient client = new();
            await client.ConnectAsync(ipEndPoint);

            while (client.Connected)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }
                await using NetworkStream stream = client.GetStream();

                byte[] buffer = new byte[1_024];
                int received = await stream.ReadAsync(buffer, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }
                string message = Encoding.UTF8.GetString(buffer, 0, received);
                Console.WriteLine($"Message received: \"{message}\"");
                // Sample output:
                //     Message received: "📅 8/22/2022 9:07:17 AM 🕛"

                yield return message;
            }
        }
    }
}
