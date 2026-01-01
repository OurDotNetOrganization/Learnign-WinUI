using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharedLibrary.Networking
{
    public class TransmissionControlProtocolListener
    {
        public async IAsyncEnumerable<string> ListenAsync(IPAddress iPAddress, 
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
                    yield break;
                }
                await using NetworkStream stream = handler.GetStream();

                var message = $"📅 {DateTime.Now} 🕛";
                var dateTimeBytes = Encoding.UTF8.GetBytes(message);

                yield return message;

                await stream.WriteAsync(dateTimeBytes, cancellationToken);
                if(cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }
                Console.WriteLine($"Sent message: \"{message}\"");
                // Sample output:
                //     Sent message: "📅 8/22/2022 9:07:17 AM 🕛"
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}
