namespace Game.Command
{
    public class TestServerCommand
    {
        //public static async Task send(ServerInfo o, IDictionary<string, TestServerData> map, UIStartGameDialog uiStartGameDialog, Dispatcher dispatcher)
        //{
        //    try
        //    {
        //        var data = await NetworkHelper.fetch(o.ip, o.port, new TestServerData()
        //        {
        //            serverCode = o.code,
        //            timeStamp = DateTime.Now
        //        }.toCommandString(nameof(TestServerCommand)));

        //        var (name, s) = data.fromCommandString();

        //        var c = s.fromJson<TestServerData>();

        //        c.ping = (int)(DateTime.Now - c.timeStamp).TotalMilliseconds;

        //        map[c.serverCode] = c;

        //        dispatcher.invoke(() => uiStartGameDialog.refresh(map));
        //    }
        //    catch (SocketException e)
        //    when (e.SocketErrorCode == SocketError.ConnectionRefused)
        //    {
        //        map[o.code] = null;

        //        dispatcher.invoke(() => uiStartGameDialog.refresh(map));

        //        throw;
        //    }
        //}
    }
}
