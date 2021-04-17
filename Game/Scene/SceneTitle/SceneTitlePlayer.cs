using Core.Helper;
using Game.Model;
using Game.UI;
using Game.UI.SceneTitle;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Scene
{
    public class SceneTitlePlayer : SceneBase
    {
        private UIPlayerDialog uiPlayerDialog;
        private UIPlayerDetailDialog uiPlayerDetailDialog;

        public SceneTitlePlayer(GameSystem gs) : base(gs)
        {
        }

        public override void start()
        {
            uiPlayerDialog = new UIPlayerDialog(gameSystem)
            {
                addButtonClicked = onAddButtonClicked,
                removeButtonClicked = onRemoveButtonClicked,
                editButtonClicked = onEditButtonClicked,
                okButtonClicked = onOkButtonClicked
            };

            uiPlayerDialog.setData(gameSystem.players.Select(o => o.name).ToList());

            uiPlayerDialog.Visible = true;
        }

        public override void finish()
        {
            uiPlayerDialog?.Close();
        }

        private void onAddButtonClicked()
        {
            uiPlayerDialog.Visible = false;

            uiPlayerDetailDialog = new UIPlayerDetailDialog(gameSystem)
            {
                Text = gameSystem.wording.add,
                okButtonClicked = async () =>
                {
                    var name = uiPlayerDetailDialog.name;

                    if (!checkPlayer(ref name)) return;

                    gameSystem.players.Add(new PlayerInfo()
                    {
                        name = name,
                        code = Guid.NewGuid().ToString(),
                        servers = new List<ServerInfo>()
                    });

                    PlayerHelper.savePlayer(gameSystem.players);

                    uiPlayerDialog.setData(gameSystem.players.Select(o => o.name).ToList());

                    uiPlayerDetailDialog.Close();

                    uiPlayerDialog.Visible = true;
                },
                cancelButtonClicked = () =>
                {
                    uiPlayerDetailDialog.Close();

                    uiPlayerDialog.Visible = true;
                }
            };

            uiPlayerDetailDialog.Show();
        }

        private void onRemoveButtonClicked(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            var dialog = new UIConfirmDialog(gameSystem, "confirm", $"remove player {name} ?");

            dialog.okButtonClicked = () =>
            {
                gameSystem.players.RemoveAll(o => o.name == name);

                PlayerHelper.savePlayer(gameSystem.players);

                uiPlayerDialog.setData(gameSystem.players.Select(o => o.name).ToList());

                dialog.Close();
            };

            dialog.ShowDialog(formMain);
        }

        private void onEditButtonClicked(string oldName)
        {
            uiPlayerDialog.Visible = false;

            uiPlayerDetailDialog = new UIPlayerDetailDialog(gameSystem)
            {
                Text = gameSystem.wording.edit,
                okButtonClicked = () =>
                {
                    var newName = uiPlayerDetailDialog.name;

                    if (!checkPlayer(ref newName)) return;

                    gameSystem.players.SingleOrDefault(o => o.name == oldName).name = newName;

                    PlayerHelper.savePlayer(gameSystem.players);

                    uiPlayerDialog.setData(gameSystem.players.Select(o => o.name).ToList());

                    uiPlayerDetailDialog.Close();

                    uiPlayerDialog.Visible = true;
                },
                cancelButtonClicked = () =>
                {
                    uiPlayerDetailDialog.Close();

                    uiPlayerDialog.Visible = true;
                }
            };

            uiPlayerDetailDialog.Show();
        }

        private void onOkButtonClicked()
        {
            var name = uiPlayerDialog.name;

            if (string.IsNullOrWhiteSpace(name))
            {
                new UIDialog(gameSystem, "alert", "select a player").ShowDialog(formMain);

                return;
            }

            var player = gameSystem.players.SingleOrDefault(o => o.name == name);

            if (player == null)
            {
                new UIConfirmDialog(gameSystem, "error", "select a player").ShowDialog(uiPlayerDialog);

                return;
            }

            gameSystem.currentPlayer = player;

            uiPlayerDialog.Visible = false;

            gameSystem.sceneToTitleMain();
        }

        private bool checkPlayer(ref string name)
        {
            var n = name;

            if (string.IsNullOrWhiteSpace(name = name.Trim())) return false;

            if (gameSystem.players.Any(o => o.name == n))
            {
                new UIDialog(gameSystem, "error", "name existed").ShowDialog();

                return false;
            }

            return true;
        }
    }
}
