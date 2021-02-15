using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Library
{
    public class GameClient
    {
        public int id;
        public TcpClient tcpClient;
        public NetworkStream networkStream;
        public Action<GameClient> clientDisconnected;
        public Action<GameClient, string> dataReceived;

        //private Pipe pipe;
        private readonly List<byte> sendDataBytes;

        public bool isRunning { get; private set; }

        //public string ip => tcpClient.getIp();

        public GameClient(TcpClient tc, int dataBufferSize = 10240)
        {
            tcpClient = tc;

            sendDataBytes = new List<byte>(dataBufferSize);
            networkStream = tcpClient.GetStream();
            //pipe = new Pipe();
        }

        public async Task connect(string ip, int port) => await tcpClient.ConnectAsync(ip, port);

        public async Task listen()
        {
            while (isRunning)
            {
                var (hasResult, data) = await read();

                if (hasResult) dataReceived?.Invoke(this, data);
            }
        }

        public async ValueTask<(bool hasResult, string data)> read()
        {
            try
            {
                //var data = await NetworkHelper.read(networkStream, pipe, false);

                return (true, null);
            }
            catch (IOException e)
            {
                Debug.WriteLine(e);

                disconnect();

                clientDisconnected?.Invoke(this);
            }

            return (false, null);
        }

        public async Task write(string data, CancellationToken ct = default)
        {
            sendDataBytes.Clear();

            //await NetworkHelper.write(networkStream, data, sendDataBytes, ct);
        }

        public void disconnect()
        {
            networkStream.Close();
            tcpClient.Close();

        }
    }
}
