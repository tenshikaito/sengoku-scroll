using Core;
using Core.Helper;
using Game.UI;
using Game.UI.SceneTitle;
using Library;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Game.Scene
{
    public class SceneTitleEditGameWorld : SceneBase
    {
        private UIEditGameWorldDialog uiEditGameWorldDialog;

        private UIGameWorldDetailDialog uiGameWorldDetailDialog;

        public SceneTitleEditGameWorld(GameSystem gs) : base(gs)
        {
        }

        public override void start()
        {
            uiEditGameWorldDialog = new UIEditGameWorldDialog(gameSystem)
            {
                Visible = true,
                addButtonClicked = onAddButtonClicked,
                removeButtonClicked = onRemoveButtonClicked,
                editButtonClicked = onEditButtonClicked,
                okButtonClicked = () =>
                {
                    uiEditGameWorldDialog.Close();
                    gameSystem.sceneToTitleMain();
                }
            };

            _ = loadGameWorldMapList();
        }

        public override void finish()
        {
            uiEditGameWorldDialog.Close();
        }

        private void onAddButtonClicked()
        {
            uiEditGameWorldDialog.Visible = false;

            uiGameWorldDetailDialog = new UIGameWorldDetailDialog(gameSystem)
            {
                Visible = true,
                okButtonClicked = async () =>
                {
                    uiGameWorldDetailDialog.Cursor = Cursors.WaitCursor;

                    uiGameWorldDetailDialog.isBtnOkEnabled = uiGameWorldDetailDialog.isBtnCancelEnabled = false;

                    var (name, width, height) = uiGameWorldDetailDialog.value;

                    var r = await onCreateGameWorld(name, width, height);

                    uiGameWorldDetailDialog.isBtnOkEnabled = uiGameWorldDetailDialog.isBtnCancelEnabled = true;

                    uiGameWorldDetailDialog.Cursor = Cursors.Default;

                    if (!r) return;

                    uiGameWorldDetailDialog.Close();

                    await loadGameWorldMapList();

                    uiEditGameWorldDialog.Visible = true;
                },
                cancelButtonClicked = () =>
                {
                    uiGameWorldDetailDialog.Close();
                    uiEditGameWorldDialog.Visible = true;
                }
            };
        }

        private void onEditButtonClicked(string name)
        {
            var dialog = new UIConfirmDialog(gameSystem, "confirm", "edit game world?");

            dialog.okButtonClicked = () =>
            {
                dialog.Close();

                uiEditGameWorldDialog.Close();

                gameSystem.sceneToWaiting();

                Task.Run(async () =>
                {
                    try
                    {
                        var gw = await GameWorldHelper.loadGameWorldData<GameWorldSystem>(name);

                        gw.camera = new Camera(gameSystem.option.screenWidth, gameSystem.option.screenHeight);

                        gameSystem.dispatchSceneToEditGame(gw);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);

                        throw;
                    }
                });
            };

            dialog.ShowDialog(formMain);
        }

        private void onRemoveButtonClicked(string name)
        {
            var dialog = new UIConfirmDialog(gameSystem, "confirm", "delete?");

            dialog.okButtonClicked = () =>
            {
                dialog.Close();

                try
                {
                    GameWorldHelper.delete(name);
                }
                catch (Exception e)
                {
                    MessageBox.Show("create_failed:" + e.Message);
                    return;
                }

                _ = loadGameWorldMapList();
            };

            dialog.ShowDialog(formMain);
        }

        private async ValueTask<bool> onCreateGameWorld(string name, string txtWidth, string txtHeight)
        {
            name = name.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                new UIDialog(gameSystem, "error", "game_world_name").ShowDialog(formMain);
                return false;
            }
            if (!int.TryParse(txtWidth.Trim(), out var width))
            {
                new UIDialog(gameSystem, "error", "width_number").ShowDialog(formMain);
                return false;
            }
            if (!int.TryParse(txtHeight.Trim(), out var height))
            {
                new UIDialog(gameSystem, "error", "height_number").ShowDialog(formMain);
                return false;
            }

            if (width < GameConstant.mapMinSize || height < GameConstant.mapMinSize)
            {
                new UIDialog(gameSystem, "error", $"map size > {GameConstant.mapMinSize}").ShowDialog(formMain);
                return false;
            }

            if (width > GameConstant.mapMaxSize || height > GameConstant.mapMaxSize)
            {
                new UIDialog(gameSystem, "error", $"map size < {GameConstant.mapMaxSize}").ShowDialog(formMain);
                return false;
            }

            if (!await GameWorldHelper.create(name, width, height))
            {
                new UIDialog(gameSystem, "error", "create_failed").ShowDialog(formMain);
                return false;
            }

            return true;
        }

        private async Task loadGameWorldMapList() => uiEditGameWorldDialog.setData(await GameWorldHelper.getInfoList());
    }
}
