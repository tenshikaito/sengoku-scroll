
//namespace Library.Helper
//{
//    public static class NetworkHelper
//    {
//        public const int dataBufferSize = Constant.dataBufferLength;

//        public static readonly Encoding encoding = Encoding.UTF8;

//        private const int dataLengthByteLength = 4;

//        public static string getIp(this TcpClient tc) => tc.Client.RemoteEndPoint.ToString();

//        public static GameClient getGameClient(TcpClient tc = null, int dataBufferSize = dataBufferSize)
//            => new GameClient(tc ?? new TcpClient(), dataBufferSize);

//        public static async Task write(this GameClient gc, string name, BaseData bd, CancellationToken ct = default) => await gc.write(bd.toCommandString(name), ct);

//        public static (bool hasResult, string data) splitDataStream(List<byte> receiveDataBytes)
//        {
//            var list = receiveDataBytes;

//            if (list.Count < dataLengthByteLength) return (false, null);

//            var length = BitConverter.ToInt32(list.Take(dataLengthByteLength).ToArray(), 0);

//            if (list.Count - dataLengthByteLength < length) return (false, null);

//            list.RemoveRange(0, dataLengthByteLength);

//            var data = list.Take(length).ToArray();

//            list.RemoveRange(0, length);

//            var s = encoding.GetString(data);

//            return (true, s);
//        }

//        public static (bool hasResult, string data) splitDataStream(PipeReader r, ReadOnlySequence<byte> buffer)
//        {
//            if (buffer.Length < dataLengthByteLength) return (false, null);

//            var length = BitConverter.ToInt32(buffer.Slice(0, dataLengthByteLength).ToArray());

//            if (buffer.Length - dataLengthByteLength < length) return (false, null);

//            var slice = buffer.Slice(dataLengthByteLength, length);

//            var data = slice.ToArray();

//            r.AdvanceTo(slice.End);

//            var s = encoding.GetString(data);

//            return (true, s);
//        }

//        public static async Task<string> read(NetworkStream ns, Pipe p = null, bool isOnce = true, CancellationToken ct = default)
//        {
//            p = p ?? new Pipe();

//            var w = p.Writer;
//            var r = p.Reader;

//            while (true)
//            {
//                var m = w.GetMemory(dataBufferSize);

//                var length = await ns.ReadAsync(m, ct);

//                if (length == 0) return null;

//                w.Advance(length);

//                await w.FlushAsync(ct);

//                var rr = await r.ReadAsync(ct);
//                var buffer = rr.Buffer;

//                var rrr = splitDataStream(r, buffer);

//                if (rrr.hasResult)
//                {
//                    if (isOnce)
//                    {
//                        await w.CompleteAsync();
//                        await r.CompleteAsync();
//                    }

//                    return rrr.data;
//                }
//                else
//                {
//                    r.AdvanceTo(buffer.Start, buffer.End);
//                }
//            }
//        }

//        public static async Task write(NetworkStream ns, string data, List<byte> sendDataBytes = null, CancellationToken ct = default)
//        {
//            sendDataBytes = sendDataBytes ?? new List<byte>();

//            var r = combineDataStream(sendDataBytes, data);

//            await ns.WriteAsync(r, ct);

//            await ns.FlushAsync();
//        }

//        public static byte[] combineDataStream(List<byte> sendDataBytes, string data)
//        {
//            var buffer = encoding.GetBytes(data);

//            var length = BitConverter.GetBytes(buffer.Length);

//            sendDataBytes.AddRange(length);

//            sendDataBytes.AddRange(buffer);

//            return sendDataBytes.ToArray();
//        }

//        public static async Task<string> fetch(string ip, int port, string data, CancellationToken ct = default)
//        {
//            await connect(ip, port, async (tc, ns) =>
//            {
//                await write(ns, data, null, ct);

//                data = await read(ns, null, true, ct);
//            });

//            return data;
//        }

//        public static async Task connect(string ip, int port, Func<TcpClient, NetworkStream, Task> getConnection)
//        {
//            using (var tc = new TcpClient())
//            {
//                await tc.ConnectAsync(ip, port);

//                using (var ns = tc.GetStream())
//                {
//                    await getConnection(tc, ns);
//                }
//            }
//        }
//    }
//}
