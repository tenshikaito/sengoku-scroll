using Core.Helper;
using Core.Network;
using Game.Command;
using Game.Model;
using Game.UI;
using Game.UI.SceneTitle;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Game.Scene
{
    public class SceneTitleMultiplayerGame : SceneBase
    {
        private UIMultiPlayerGameDialog uiStartGameDialog;

        private UIGameServerDetailDialog uiGameServerDetailDialog;

        private readonly ConcurrentDictionary<string, TestServerData> map = new ConcurrentDictionary<string, TestServerData>();

        public SceneTitleMultiplayerGame(GameSystem gs) : base(gs)
        {
        }

        public override void start()
        {
            on();
        }

        public override void finish()
        {
            uiStartGameDialog?.Close();
        }

        private void on()
        {
            uiStartGameDialog = new UIMultiPlayerGameDialog(gameSystem)
            {
                Visible = true,
                addButtonClicked = onAddButtonClicked,
                removeButtonClicked = onRemoveButtonClicked,
                editButtonClicked = onEditButtonClicked,
                refreshButtonClicked = onRefreshButtonClicked,
                okButtonClicked = onOkButtonClicked,
                cancelButtonClicked = () =>
                {
                    uiStartGameDialog.Close();
                    gameSystem.sceneToTitleMain();
                }
            };

            loadGameServerList();

            testServer();
        }

        private void testServer()
        {
            map.Clear();

            var servers = gameSystem.currentPlayer.servers;

            uiStartGameDialog.setData(servers);

            servers.ForEach(async o =>
            {
                try
                {
                    //await TestServerCommand.send(o, map, uiStartGameDialog, dispatcher);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            });
        }

        private void onAddButtonClicked()
        {
            uiStartGameDialog.Visible = false;

            uiGameServerDetailDialog = new UIGameServerDetailDialog(gameSystem)
            {
                Visible = true,
                okButtonClicked = () =>
                {
                    var (txtName, txtIp, txtPort) = uiGameServerDetailDialog.value;

                    if (!checkServer(ref txtName, ref txtIp, ref txtPort, out var port)) return;

                    var code = Guid.NewGuid().ToString();

                    gameSystem.currentPlayer.servers.Add(new ServerInfo()
                    {
                        code = code,
                        name = txtName,
                        ip = txtIp,
                        port = port
                    });

                    PlayerHelper.savePlayer(gameSystem.players);

                    loadGameServerList();

                    testServer();

                    uiGameServerDetailDialog.Close();

                    uiStartGameDialog.Visible = true;
                },
                cancelButtonClicked = () =>
                {
                    uiGameServerDetailDialog.Close();

                    uiStartGameDialog.Visible = true;
                }
            };
        }

        private void onEditButtonClicked(string code)
        {
            uiStartGameDialog.Visible = false;

            uiGameServerDetailDialog = new UIGameServerDetailDialog(gameSystem)
            {
                Visible = true,
                okButtonClicked = () =>
                {
                    var (txtName, txtIp, txtPort) = uiGameServerDetailDialog.value;

                    if (!checkServer(ref txtName, ref txtIp, ref txtPort, out var port)) return;

                    var si = gameSystem.currentPlayer.servers.SingleOrDefault(o => o.code == code);

                    si.name = txtName;
                    si.ip = txtIp;
                    si.port = port;

                    PlayerHelper.savePlayer(gameSystem.players);

                    loadGameServerList();

                    testServer();

                    uiGameServerDetailDialog.Close();

                    uiStartGameDialog.Visible = true;
                },
                cancelButtonClicked = () =>
                {
                    uiGameServerDetailDialog.Close();

                    uiStartGameDialog.Visible = true;
                }
            };
        }

        private void onRemoveButtonClicked(string code)
        {
            var name = gameSystem.currentPlayer.servers.SingleOrDefault(o => o.code == code).name;

            var dialog = new UIConfirmDialog(gameSystem, "confirm", $"remove {name} ?");

            dialog.okButtonClicked = () =>
            {
                gameSystem.currentPlayer.servers.RemoveAll(o => o.code == code);

                PlayerHelper.savePlayer(gameSystem.players);

                loadGameServerList();

                dialog.Close();
            };

            dialog.ShowDialog(formMain);
        }

        private void onRefreshButtonClicked()
        {
            testServer();
        }

        private void onOkButtonClicked()
        {
            var code = uiStartGameDialog.selectedValue;

            if (code == null) return;

            if(!map.TryGetValue(code, out var ov) || ov == null || ov.ping < 0)
            {
                MessageBox.Show("server disconnected.");

                return;
            }

            var si = gameSystem.currentPlayer.servers.SingleOrDefault(o => o.code == code);

            var s = gameSystem.sceneToWaiting();

            Task.Run(async () =>
            {
                try
                {
                    await JoinGameCommand.execute(si, gameSystem.currentPlayer, s, gameSystem);
                }
                catch (SocketException e)
                when (e.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    MessageBox.Show("server disconnected.");

                    dispatcher.invoke(() => gameSystem.sceneToTitleMultiplayerGame());
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            });
        }

        private static bool checkServer(ref string txtName, ref string txtIp, ref string txtPort, out int port)
        {
            port = 0;

            if (string.IsNullOrWhiteSpace(txtName = txtName.Trim()))
            {
                MessageBox.Show("game_server_name");
                return false;
            }

            if (!IPAddress.TryParse(txtIp = txtIp.Trim(), out _))
            {
                MessageBox.Show("ip");
                return false;
            }

            if (!int.TryParse(txtPort.Trim(), out port))
            {
                MessageBox.Show("port");
                return false;
            }

            return true;
        }

        private void loadGameServerList() => uiStartGameDialog.setData(gameSystem.currentPlayer.servers);
    }
}
