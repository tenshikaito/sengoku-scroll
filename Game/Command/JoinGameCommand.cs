using Game.Model;
using Game.Scene;
using System.Threading.Tasks;

namespace Game.Command
{
    public class JoinGameCommand
    {
        public static async Task execute(ServerInfo si, PlayerInfo pi, SceneWaiting sw, GameSystem gs)
        {
            // 流程：将自身的身份信息发送到服务端、在服务端自动创建好角色后将角色信息返回

            var w = gs.gameWording;

            var dispatcher = gs.formMain.dispatcher;

            dispatcher.invoke(() => sw.setText($"{w.scene_title.connecting}..."));

            //await NetworkHelper.connect(si.ip, si.port, async (tc, ns) =>
            //{
            //    dispatcher.invoke(() => sw.setText($"{w.scene_title.loading}..."));

            //    var gc = NetworkHelper.getGameClient(tc);

            //    await gc.write(new JoinGameRequestData()
            //    {
            //        playerCode = pi.code,
            //        playerName = pi.name
            //    }.toCommandString(nameof(JoinGameCommand)));

            //    var (hasResult, data) = await gc.read();

            //    var r = data.fromJson<JoinGameResponseData>();

            //    var gwd = r.gameWorldData;

            //    var gw = new GameWorld(gwd.name);

            //    gw.camera = new Camera(gs.screenWidth, gs.screenHeight);
            //    gw.currentPlayer = r.player;

            //    gw.init(gwd);

            //    gs.dispatchSceneToGame(gw);
            //});
        }
    }
}
