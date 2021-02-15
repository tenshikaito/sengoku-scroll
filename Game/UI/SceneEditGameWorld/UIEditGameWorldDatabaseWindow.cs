using Core;
using Core.Data;
using Core.Helper;
using Core.Model;
using Game.Helper;
using Library.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using WinLibrary.Extension;

namespace Game.UI.SceneEditGameWorld
{
    public partial class UIEditGameWorldDatabaseWindow : UISaveableConfirmDialog
    {
        public MasterData gameWorldMasterData;

        private readonly TabControl tabControl;

        public UIEditGameWorldDatabaseWindow(GameSystem gs, GameWorldSystem gw) : base(gs)
        {
            gameWorldMasterData = gw.masterData.toJson().fromJson<MasterData>();

            MaximizeBox = false;

            this.init(w.scene_edit_game_world.database).setCenter();

            var tc = tabControl = new TabControl() { MinimumSize = new System.Drawing.Size(720, 480) }.init().setAutoSize().addTo(panel);

            new TabPageTerrain(this, gameWorldMasterData.tileMapTerrain, gameWorldMasterData.terrainImage).addTo(tc);

            new TabPageRegion(this, gameWorldMasterData.region).addTo(tc);

            new TabPageCulture(this, gameWorldMasterData.culture).addTo(tc);

            new TabPageReligion(this, gameWorldMasterData.religion).addTo(tc);

            new TabPageRoad(this, gameWorldMasterData.road).addTo(tc);

            //new TabPageStronghold(this, gameWorldMasterData.strongholdType, gameWorldMasterData).addTo(tc);

            //new TabPageDetailTileMapInfo(this, gameWorldMasterData.detailTileMapInfo, gameWorldMasterData.detailTileMapTerrain).addTo(tc);

            new TabPageTerrainImage(this, gameWorldMasterData.terrainImage, gameWorldMasterData.tileMapTerrain).addTo(tc);

            addSaveableConfirmButtons();
        }

        protected override void OnLoad(EventArgs e)
        {
            tabControl.TabPages.Cast<TabPageBase>().ToList().ForEach(o => o.onLoad());
        }

        private class TabPageBase : TabPage
        {
            protected UIEditGameWorldDatabaseWindow window;
            protected ListView listView;

            protected GameWording w => window.w;

            public TabPageBase(UIEditGameWorldDatabaseWindow bw) => window = bw;

            protected (TableLayoutPanel sc,
                TableLayoutPanel pl,
                ListView lv)
                createIndexControl(Action<ListView> btnAddClick, Action<ListView, int> btnDeleteClick)
            {
                var sc = new TableLayoutPanel()
                {
                    RowCount = 2,
                    ColumnCount = 1,
                    Dock = DockStyle.Fill
                }.addRowStyle(50).addRowStyle(50).addTo(this);

                var gb = new GroupBox()
                {
                    Dock = DockStyle.Fill,
                    Text = w.type
                }.addTo(sc);

                var pl = new TableLayoutPanel()
                {
                    RowCount = 1,
                    ColumnCount = 2,
                    Dock = DockStyle.Fill,
                }.addColumnStyle(80).addColumnStyle(20).addTo(gb);

                var lv = listView = new ListView()
                {
                    Dock = DockStyle.Fill,

                }.init().addTo(pl);

                var p2 = new TableLayoutPanel()
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1
                }.addTo(pl);

                new Button().init(w.add, () => btnAddClick(lv)).addTo(p2);
                new Button().init(w.remove, () =>
                {
                    if (lv.FocusedItem == null) return;

                    btnDeleteClick(lv, (int)lv.FocusedItem.Tag);
                }).addTo(p2);

                new Label().addTo(p2);

                return (sc, pl, lv);
            }

            public void onLoad()
            {
                listView?.autoResizeColumns();
            }
        }
    }

    public partial class UIEditGameWorldDatabaseWindow
    {
        private class TabPageTerrain : TabPageBase
        {
            private readonly Dictionary<int, Terrain> data;
            private readonly Dictionary<int, TerrainImage> terrainImage;
            private int? currentId;

            private readonly TextBox tbName;
            private readonly ComboBox cbImage;
            private readonly CheckBox cbIsSurface;
            private readonly CheckBox cbIsGrass;
            private readonly CheckBox cbIsForest;
            private readonly CheckBox cbIsWaste;
            private readonly CheckBox cbIsDesert;
            private readonly CheckBox cbIsMarsh;
            private readonly CheckBox cbIsRock;
            private readonly CheckBox cbIsHill;
            private readonly CheckBox cbIsMountain;
            private readonly CheckBox cbIsHabour;
            private readonly CheckBox cbIsWater;
            private readonly CheckBox cbIsFreshWater;
            private readonly CheckBox cbIsDeepWater;

            public TabPageTerrain(UIEditGameWorldDatabaseWindow bw, Dictionary<int, Terrain> data, Dictionary<int, TerrainImage> terrainImage) : base(bw)
            {
                this.data = data;
                this.terrainImage = terrainImage;

                this.init(w.terrain.text).setAutoSizeP();

                bw.tabControl.SelectedIndexChanged += (s, e) =>
                {
                    if (bw.tabControl.SelectedTab == this) refresh();
                };

                var (sc, pl, lv) = createIndexControl(onAddButtonClicked, onDeleteButtonClicked);

                lv.addColumn(w.id)
                    .addColumn(w.name)
                    .addColumn(w.terrain_image.text)
                    .addColumn(w.terrain.is_surface)
                    .addColumn(w.terrain.is_grass)
                    .addColumn(w.terrain.is_forest)
                    .addColumn(w.terrain.is_waste)
                    .addColumn(w.terrain.is_desert)
                    .addColumn(w.terrain.is_marsh)
                    .addColumn(w.terrain.is_rock)
                    .addColumn(w.terrain.is_hill)
                    .addColumn(w.terrain.is_mountain)
                    .addColumn(w.terrain.is_habour)
                    .addColumn(w.terrain.is_water)
                    .addColumn(w.terrain.is_fresh_water)
                    .addColumn(w.terrain.is_deep_water);

                lv.Click += (s, e) =>
                {
                    if (lv.FocusedItem == null) return;

                    onListViewClicked(lv, (int)lv.FocusedItem.Tag);
                };

                var p2 = new TableLayoutPanel()
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 2
                }.addRowStyle(80).addRowStyle(20).addTo(sc);

                var gb = new GroupBox()
                {
                    Dock = DockStyle.Fill,
                    Text = w.scene_edit_game_world.property
                }.addTo(p2);

                var p3 = new TableLayoutPanel()
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    AutoScroll = true
                }.addTo(gb);

                new Label().init(w.name + ":").setAutoSize().setRightCenter().addTo(p3);
                tbName = new TextBox().refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init(w.terrain_image.text + ":").setAutoSize().setRightCenter().addTo(p3);
                cbImage = new ComboBox().initDropDownList<int>().addTo(p3);

                new Label().init(w.terrain.is_surface + ":").setAutoSize().setRightCenter().addTo(p3);
                cbIsSurface = new CheckBox().init().refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init(w.terrain.is_grass + ":").setAutoSize().setRightCenter().addTo(p3);
                cbIsGrass = new CheckBox().init().refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init(w.terrain.is_forest + ":").setAutoSize().setRightCenter().addTo(p3);
                cbIsForest = new CheckBox().init().refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init(w.terrain.is_waste + ":").setAutoSize().setRightCenter().addTo(p3);
                cbIsWaste = new CheckBox().init().refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init(w.terrain.is_desert + ":").setAutoSize().setRightCenter().addTo(p3);
                cbIsDesert = new CheckBox().init().refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init(w.terrain.is_marsh + ":").setAutoSize().setRightCenter().addTo(p3);
                cbIsMarsh = new CheckBox().init().refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init(w.terrain.is_rock + ":").setAutoSize().setRightCenter().addTo(p3);
                cbIsRock = new CheckBox().init().refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init(w.terrain.is_hill + ":").setAutoSize().setRightCenter().addTo(p3);
                cbIsHill = new CheckBox().init().refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init(w.terrain.is_mountain + ":").setAutoSize().setRightCenter().addTo(p3);
                cbIsMountain = new CheckBox().init().refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init(w.terrain.is_habour + ":").setAutoSize().setRightCenter().addTo(p3);
                cbIsHabour = new CheckBox().init().refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init(w.terrain.is_water + ":").setAutoSize().setRightCenter().addTo(p3);
                cbIsWater = new CheckBox().init().refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init(w.terrain.is_fresh_water + ":").setAutoSize().setRightCenter().addTo(p3);
                cbIsFreshWater = new CheckBox().init().refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init(w.terrain.is_deep_water + ":").setAutoSize().setRightCenter().addTo(p3);
                cbIsDeepWater = new CheckBox().init().refreshListViewOnClick(lv, refresh).addTo(p3);

                initData(lv);

                refresh();
            }

            private void initData(ListView lv) => data.Values.ToList().ForEach(o => lv.Items.Add(setRow(new ListViewItem() { Tag = o.id }, o)));

            private void refresh() => cbImage.setDropDownList(terrainImage.Select(o => new KeyValuePair<int, string>(o.Key, o.Value.name)).ToList());

            private void onAddButtonClicked(ListView lv)
            {
                var max = data.getMaxId(-1);

                if (++max > int.MaxValue) return;

                var oo = new Terrain()
                {
                    id = max,
                    name = w.terrain.text,
                };

                data[oo.id] = oo;

                var lvi = new ListViewItem()
                {
                    Tag = oo.id
                };

                lv.Items.Add(setRow(lvi, oo));

                lv.autoResizeColumns();
            }

            private void onDeleteButtonClicked(ListView lv, int id)
            {
                lv.Items.Remove(lv.Items.Cast<ListViewItem>().First(o => (int)o.Tag == id));

                data.Remove(id);

                if (currentId == id) currentId = null;

                lv.autoResizeColumns();
            }

            private ListViewItem setRow(ListViewItem lvi, Terrain o)
            {
                lvi.Text = o.id.ToString();

                return lvi.addColumn(o.name)
                    .addColumn(terrainImage.TryGetValue(o.imageId, out var ti) ? ti.name : w.symbol_none)
                    .addColumn(o.isSurface.getSymbol(w))
                    .addColumn(o.isGrass.getSymbol(w))
                    .addColumn(o.isForest.getSymbol(w))
                    .addColumn(o.isWaste.getSymbol(w))
                    .addColumn(o.isDesert.getSymbol(w))
                    .addColumn(o.isMarsh.getSymbol(w))
                    .addColumn(o.isRock.getSymbol(w))
                    .addColumn(o.isHill.getSymbol(w))
                    .addColumn(o.isMountain.getSymbol(w))
                    .addColumn(o.isHarbour.getSymbol(w))
                    .addColumn(o.isWater.getSymbol(w))
                    .addColumn(o.isFreshWater.getSymbol(w))
                    .addColumn(o.isDeepWater.getSymbol(w));
            }

            private void refresh(ListView lv)
            {
                if (currentId != null)
                {
                    var oo = data[(int)currentId];

                    oo.name = tbName.Text;

                    if (cbImage.SelectedValue != null) oo.imageId = (int)cbImage.SelectedValue;

                    oo.isSurface = cbIsSurface.Checked;
                    oo.isGrass = cbIsGrass.Checked;
                    oo.isForest = cbIsForest.Checked;
                    oo.isWaste = cbIsWaste.Checked;
                    oo.isDesert = cbIsDesert.Checked;
                    oo.isMarsh = cbIsMarsh.Checked;
                    oo.isRock = cbIsRock.Checked;
                    oo.isHill = cbIsHill.Checked;
                    oo.isMountain = cbIsMountain.Checked;
                    oo.isHarbour = cbIsHabour.Checked;
                    oo.isWater = cbIsWater.Checked;
                    oo.isFreshWater = cbIsFreshWater.Checked;
                    oo.isDeepWater = cbIsDeepWater.Checked;

                    var lvi = lv.Items.Cast<ListViewItem>().FirstOrDefault(o => (int)o.Tag == currentId);

                    if (lvi != null)
                    {
                        lvi.SubItems.Clear();
                        setRow(lvi, oo);
                        lv.autoResizeColumns();
                    }
                }
            }

            private void onListViewClicked(ListView lv, int id)
            {
                refresh(lv);

                currentId = id;
                var o = data[id];

                tbName.Text = o.name;
                cbImage.SelectedValue = o.imageId;
                cbIsSurface.Checked = o.isSurface;
                cbIsGrass.Checked = o.isGrass;
                cbIsForest.Checked = o.isForest;
                cbIsWaste.Checked = o.isWaste;
                cbIsDesert.Checked = o.isDesert;
                cbIsMarsh.Checked = o.isMarsh;
                cbIsRock.Checked = o.isRock;
                cbIsHill.Checked = o.isHill;
                cbIsMountain.Checked = o.isMountain;
                cbIsHabour.Checked = o.isHarbour;
                cbIsWater.Checked = o.isWater;
                cbIsFreshWater.Checked = o.isFreshWater;
                cbIsDeepWater.Checked = o.isDeepWater;
            }
        }
    }

    public partial class UIEditGameWorldDatabaseWindow
    {
        private class TabPageRegion : TabPageBase
        {
            private readonly Dictionary<int, Region> data;
            private int? currentId;

            private readonly TextBox tbName;
            private readonly ComboBox cbClimate;

            public TabPageRegion(UIEditGameWorldDatabaseWindow bw, Dictionary<int, Region> data) : base(bw)
            {
                this.data = data;

                this.init(w.region.text).setAutoSizeP();

                var (sc, pl, lv) = createIndexControl(onAddButtonClicked, onDeleteButtonClicked);

                lv.addColumn(w.id)
                    .addColumn(w.name)
                    .addColumn(w.region.climate);

                lv.Click += (s, e) =>
                {
                    if (lv.FocusedItem == null) return;

                    onListViewClicked(lv, (int)lv.FocusedItem.Tag);
                };

                var p2 = new TableLayoutPanel()
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 2
                }.addRowStyle(80).addRowStyle(20).addTo(sc);

                var gb = new GroupBox()
                {
                    Dock = DockStyle.Fill,
                    Text = w.scene_edit_game_world.property
                }.addTo(p2);

                var p3 = new TableLayoutPanel()
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    AutoScroll = true
                }.addTo(gb);

                new Label().init(w.name + ":").setAutoSize().setRightCenter().addTo(p3);
                tbName = new TextBox().refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init(w.region.climate + ":").setAutoSize().setRightCenter().addTo(p3);
                cbClimate = new ComboBox()
                    .initDropDownList(Core.Model.Region.list.Select(o => new KeyValuePair<Climate, string>(o, w.region[o.ToString()])).ToList(), Climate.normal)
                    .refreshListViewOnClick(lv, refresh)
                    .addTo(p3);

                new Label().init("").addTo(p3);

                initData(lv);

                lv.autoResizeColumns();
            }

            private void initData(ListView lv) => data.Values.ToList().ForEach(o => lv.Items.Add(setRow(new ListViewItem() { Tag = o.id }, o)));

            private void onAddButtonClicked(ListView lv)
            {
                var max = data.getMaxId(-1);

                if (++max > int.MaxValue) return;

                var oo = new Region()
                {
                    id = (int)max,
                    name = w.region.text,
                };

                data[oo.id] = oo;

                var lvi = new ListViewItem()
                {
                    Tag = oo.id
                };

                lv.Items.Add(setRow(lvi, oo));

                lv.autoResizeColumns();
            }

            private void onDeleteButtonClicked(ListView lv, int id)
            {
                lv.Items.Remove(lv.Items.Cast<ListViewItem>().First(o => (int)o.Tag == id));

                data.Remove(id);

                if (currentId == id) currentId = null;

                lv.autoResizeColumns();
            }

            private ListViewItem setRow(ListViewItem lvi, Region o)
            {
                lvi.Text = o.id.ToString();

                return lvi.addColumn(o.name)
                    .addColumn(w.region[o.climate.ToString()]);
            }

            private void refresh(ListView lv)
            {
                if (currentId != null)
                {
                    var oo = data[(int)currentId];

                    oo.name = tbName.Text;
                    oo.climate = (Climate)cbClimate.SelectedValue;

                    var lvi = lv.Items.Cast<ListViewItem>().FirstOrDefault(o => (int)o.Tag == currentId);

                    if (lvi != null)
                    {
                        lvi.SubItems.Clear();
                        setRow(lvi, oo);
                        lv.autoResizeColumns();
                    }
                }
            }

            private void onListViewClicked(ListView lv, int id)
            {
                refresh(lv);

                currentId = id;
                var o = data[id];

                tbName.Text = o.name;
                cbClimate.SelectedValue = o.climate;
            }
        }
    }

    public partial class UIEditGameWorldDatabaseWindow
    {
        private class TabPageCulture : TabPageBase
        {
            private readonly Dictionary<int, Culture> data;
            private int? currentId;

            private readonly TextBox tbName;

            public TabPageCulture(UIEditGameWorldDatabaseWindow bw, Dictionary<int, Culture> data) : base(bw)
            {
                this.data = data;

                this.init(w.culture.text).setAutoSizeP();

                var (sc, pl, lv) = createIndexControl(onAddButtonClicked, onDeleteButtonClicked);

                lv.addColumn(w.id)
                    .addColumn(w.name);

                lv.Click += (s, e) =>
                {
                    if (lv.FocusedItem == null) return;

                    onListViewClicked(lv, (int)lv.FocusedItem.Tag);
                };

                var p2 = new TableLayoutPanel()
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 2
                }.addRowStyle(80).addRowStyle(20).addTo(sc);

                var gb = new GroupBox()
                {
                    Dock = DockStyle.Fill,
                    Text = w.scene_edit_game_world.property
                }.addTo(p2);

                var p3 = new TableLayoutPanel()
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 2,
                    AutoScroll = true
                }.addTo(gb);

                new Label().init(w.name + ":").setAutoSize().setRightCenter().addTo(p3);
                tbName = new TextBox().refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init("").addTo(p3);

                initData(lv);

                lv.autoResizeColumns();
            }

            private void initData(ListView lv) => data.Values.ToList().ForEach(o => lv.Items.Add(setRow(new ListViewItem() { Tag = o.id }, o)));

            private void onAddButtonClicked(ListView lv)
            {
                var max = data.getMaxId(-1);

                if (++max > int.MaxValue) return;

                var oo = new Culture()
                {
                    id = max,
                    name = w.culture.text,
                };

                data[oo.id] = oo;

                var lvi = new ListViewItem()
                {
                    Tag = oo.id
                };

                lv.Items.Add(setRow(lvi, oo));

                lv.autoResizeColumns();
            }

            private void onDeleteButtonClicked(ListView lv, int id)
            {
                lv.Items.Remove(lv.Items.Cast<ListViewItem>().First(o => (int)o.Tag == id));

                data.Remove(id);

                if (currentId == id) currentId = null;

                lv.autoResizeColumns();
            }

            private static ListViewItem setRow(ListViewItem lvi, Culture o)
            {
                lvi.Text = o.id.ToString();

                return lvi.addColumn(o.name);
            }

            private void refresh(ListView lv)
            {
                if (currentId != null)
                {
                    var oo = data[(int)currentId];

                    oo.name = tbName.Text;

                    var lvi = lv.Items.Cast<ListViewItem>().FirstOrDefault(o => (int)o.Tag == currentId);

                    if (lvi != null)
                    {
                        lvi.SubItems.Clear();
                        setRow(lvi, oo);
                        lv.autoResizeColumns();
                    }
                }
            }

            private void onListViewClicked(ListView lv, int id)
            {
                refresh(lv);

                currentId = id;
                var t = data[id];

                tbName.Text = t.name;
            }
        }
    }

    public partial class UIEditGameWorldDatabaseWindow
    {
        private class TabPageReligion : TabPageBase
        {
            private readonly Dictionary<int, Religion> data;
            private int? currentId;

            private readonly TextBox tbName;
            private readonly CheckBox cbIsPolytheism;

            public TabPageReligion(UIEditGameWorldDatabaseWindow bw, Dictionary<int, Religion> data) : base(bw)
            {
                this.data = data;

                this.init(w.religion.text).setAutoSizeP();

                var (sc, pl, lv) = createIndexControl(onAddButtonClicked, onDeleteButtonClicked);

                lv.addColumn(w.id)
                    .addColumn(w.name)
                    .addColumn(w.religion.is_polytheism);

                lv.Click += (s, e) =>
                {
                    if (lv.FocusedItem == null) return;

                    onListViewClicked(lv, (int)lv.FocusedItem.Tag);
                };

                var p2 = new TableLayoutPanel()
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 2
                }.addRowStyle(80).addRowStyle(20).addTo(sc);

                var gb = new GroupBox()
                {
                    Dock = DockStyle.Fill,
                    Text = w.scene_edit_game_world.property
                }.addTo(p2);

                var p3 = new TableLayoutPanel()
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 2,
                    AutoScroll = true
                }.addTo(gb);

                new Label().init(w.name + ":").setAutoSize().setRightCenter().addTo(p3);
                tbName = new TextBox().refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init(w.religion.is_polytheism + ":").setAutoSize().setRightCenter().addTo(p3);
                cbIsPolytheism = new CheckBox().init().refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init("").addTo(p3);

                initData(lv);

                lv.autoResizeColumns();
            }

            private void initData(ListView lv) => data.Values.ToList().ForEach(o => lv.Items.Add(setRow(new ListViewItem() { Tag = o.id }, o)));

            private void onAddButtonClicked(ListView lv)
            {
                var max = data.getMaxId(-1);

                if (++max > int.MaxValue) return;

                var oo = new Religion()
                {
                    id = max,
                    name = w.religion.text,
                };

                data[oo.id] = oo;

                var lvi = new ListViewItem()
                {
                    Tag = oo.id
                };

                lv.Items.Add(setRow(lvi, oo));

                lv.autoResizeColumns();
            }

            private void onDeleteButtonClicked(ListView lv, int id)
            {
                lv.Items.Remove(lv.Items.Cast<ListViewItem>().First(o => (int)o.Tag == id));

                data.Remove(id);

                if (currentId == id) currentId = null;

                lv.autoResizeColumns();
            }

            private static ListViewItem setRow(ListViewItem lvi, Religion o)
            {
                lvi.Text = o.id.ToString();

                return lvi.addColumn(o.name);
            }

            private void refresh(ListView lv)
            {
                if (currentId != null)
                {
                    var oo = data[(int)currentId];

                    oo.name = tbName.Text;
                    oo.isPolytheism = cbIsPolytheism.Checked;

                    var lvi = lv.Items.Cast<ListViewItem>().FirstOrDefault(o => (int)o.Tag == currentId);

                    if (lvi != null)
                    {
                        lvi.SubItems.Clear();
                        setRow(lvi, oo);
                        lv.autoResizeColumns();
                    }
                }
            }

            private void onListViewClicked(ListView lv, int id)
            {
                refresh(lv);

                currentId = id;
                var o = data[id];

                tbName.Text = o.name;
                cbIsPolytheism.Checked = o.isPolytheism;
            }
        }
    }

    public partial class UIEditGameWorldDatabaseWindow
    {
        private class TabPageRoad : TabPageBase
        {
            private readonly Dictionary<int, Road> data;
            private int? currentId;

            private readonly TextBox tbName;

            public TabPageRoad(UIEditGameWorldDatabaseWindow bw, Dictionary<int, Road> data) : base(bw)
            {
                this.data = data;

                this.init(w.road.text).setAutoSizeP();

                var (sc, pl, lv) = createIndexControl(onAddButtonClicked, onDeleteButtonClicked);

                lv.addColumn(w.id)
                    .addColumn(w.name);

                lv.Click += (s, e) =>
                {
                    if (lv.FocusedItem == null) return;

                    onListViewClicked(lv, (int)lv.FocusedItem.Tag);
                };

                var p2 = new TableLayoutPanel()
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 2
                }.addRowStyle(80).addRowStyle(20).addTo(sc);

                var gb = new GroupBox()
                {
                    Dock = DockStyle.Fill,
                    Text = w.scene_edit_game_world.property
                }.addTo(p2);

                var p3 = new TableLayoutPanel()
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 2,
                    AutoScroll = true
                }.addTo(gb);

                new Label().init(w.name + ":").setAutoSize().setRightCenter().addTo(p3);
                tbName = new TextBox().refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init("").addTo(p3);

                initData(lv);

                lv.autoResizeColumns();
            }

            private void initData(ListView lv) => data.Values.ToList().ForEach(o => lv.Items.Add(setRow(new ListViewItem() { Tag = o.id }, o)));

            private void onAddButtonClicked(ListView lv)
            {
                var max = data.getMaxId(0);

                if (++max > int.MaxValue) return;

                var oo = new Road()
                {
                    id = (int)max,
                    name = w.road.text,
                };

                data[oo.id] = oo;

                var lvi = new ListViewItem()
                {
                    Tag = oo.id
                };

                lv.Items.Add(setRow(lvi, oo));

                lv.autoResizeColumns();
            }

            private void onDeleteButtonClicked(ListView lv, int id)
            {
                lv.Items.Remove(lv.Items.Cast<ListViewItem>().First(o => (int)o.Tag == id));

                data.Remove(id);

                if (currentId == id) currentId = null;

                lv.autoResizeColumns();
            }

            private static ListViewItem setRow(ListViewItem lvi, Road o)
            {
                lvi.Text = o.id.ToString();

                return lvi.addColumn(o.name);
            }

            private void refresh(ListView lv)
            {
                if (currentId != null)
                {
                    var oo = data[(int)currentId];

                    oo.name = tbName.Text;

                    var lvi = lv.Items.Cast<ListViewItem>().FirstOrDefault(o => (int)o.Tag == currentId);

                    if (lvi != null)
                    {
                        lvi.SubItems.Clear();
                        setRow(lvi, oo);
                        lv.autoResizeColumns();
                    }
                }
            }

            private void onListViewClicked(ListView lv, int id)
            {
                refresh(lv);

                currentId = id;
                var o = data[id];

                tbName.Text = o.name;
            }
        }
    }

    public partial class UIEditGameWorldDatabaseWindow
    {
        //private class TabPageStronghold : TabPageBase
        //{
        //    private MasterData gameData;
        //    private Dictionary<int, Stronghold.Type> data;
        //    private int? currentId;

        //    private TextBox tbName;
        //    private TextBox tbCulture;
        //    private TextBox tbIntroduction;

        //    public TabPageStronghold(
        //        UIEditGameWorldDatabaseWindow bw, Dictionary<int, Stronghold.Type> data, MasterData gameData)
        //        : base(bw)
        //    {
        //        this.gameData = gameData;
        //        this.data = data;

        //        this.init(w.stronghold_type.text).setAutoSizeP();

        //        var (sc, pl, lv) = createIndexControl(onAddButtonClicked, onDeleteButtonClicked);

        //        lv.addColumn(w.id)
        //            .addColumn(w.name)
        //            .addColumn(w.culture.text);

        //        lv.Click += (s, e) =>
        //        {
        //            if (lv.FocusedItem == null) return;

        //            onListViewClicked(lv, (int)lv.FocusedItem.Tag);
        //        };

        //        var p2 = new TableLayoutPanel()
        //        {
        //            Dock = DockStyle.Fill,
        //            ColumnCount = 1,
        //            RowCount = 2
        //        }.addRowStyle(80).addTo(sc);

        //        var gb = new GroupBox()
        //        {
        //            Dock = DockStyle.Fill,
        //            Text = w.scene_edit_game_world.property
        //        }.addTo(p2);

        //        var p3 = new TableLayoutPanel()
        //        {
        //            Dock = DockStyle.Fill,
        //            ColumnCount = 2,
        //            RowCount = 3,
        //            AutoScroll = true
        //        }.addTo(gb);

        //        new Label().init(w.name + ":").setAutoSize().setRightCenter().addTo(p3);
        //        tbName = new TextBox().refreshListViewOnClick(lv, refresh).addTo(p3);

        //        new Label().init(w.culture.text + ":").setAutoSize().setRightCenter().addTo(p3);
        //        tbCulture = new TextBox().refreshListViewOnClick(lv, refresh).addTo(p3);

        //        new Label().init(w.introduction + ":").setAutoSize().setRightCenter().addTo(p3);
        //        tbIntroduction = new TextBox().refreshListViewOnClick(lv, refresh).setMultiLine().addTo(p3);

        //        new Label().init("").addTo(p3);

        //        initData(lv);

        //        lv.autoResizeColumns();
        //    }

        //    private void initData(ListView lv) => data.Values.ToList().ForEach(o => lv.Items.Add(setRow(new ListViewItem() { Tag = o.id }, o)));

        //    private void onAddButtonClicked(ListView lv)
        //    {
        //        var max = data.getMaxId(0);

        //        if (++max > int.MaxValue) return;

        //        var oo = new Stronghold.Type()
        //        {
        //            id = max,
        //            name = w.stronghold_type.text,
        //            introduction = string.Empty
        //        };

        //        data[oo.id] = oo;

        //        var lvi = new ListViewItem()
        //        {
        //            Tag = oo.id
        //        };

        //        lv.Items.Add(setRow(lvi, oo));

        //        lv.autoResizeColumns();
        //    }

        //    private void onDeleteButtonClicked(ListView lv, int id)
        //    {
        //        lv.Items.Remove(lv.Items.Cast<ListViewItem>().First(o => (int)o.Tag == id));

        //        data.Remove(id);

        //        if (currentId == id) currentId = null;

        //        lv.autoResizeColumns();
        //    }

        //    private ListViewItem setRow(ListViewItem lvi, Stronghold.Type o)
        //    {
        //        lvi.Text = o.id.ToString();

        //        return lvi.addColumn(o.name).addColumn(o.culture != null && gameData.culture.TryGetValue(o.culture.Value, out var c) ? $"{c.id}:{c.name}" : w.none);
        //    }

        //    private void refresh(ListView lv)
        //    {
        //        if (currentId != null)
        //        {
        //            var oo = data[(int)currentId];

        //            oo.name = tbName.Text;
        //            oo.culture = int.TryParse(tbCulture.Text, out var c) ? c : (int?)null;
        //            oo.introduction = tbIntroduction.Text;

        //            var lvi = lv.Items.Cast<ListViewItem>().FirstOrDefault(o => (int)o.Tag == currentId);

        //            if (lvi != null)
        //            {
        //                lvi.SubItems.Clear();

        //                setRow(lvi, oo);

        //                lv.autoResizeColumns();
        //            }
        //        }
        //    }

        //    private void onListViewClicked(ListView lv, int id)
        //    {
        //        refresh(lv);

        //        currentId = id;
        //        var o = data[id];

        //        tbName.Text = o.name;
        //        tbCulture.Text = o.culture?.ToString() ?? string.Empty;
        //        tbIntroduction.Text = o.introduction;
        //    }
        //}
    }

    public partial class UIEditGameWorldDatabaseWindow
    {
        private partial class TabPageTerrainImage : TabPageBase
        {
            private readonly Dictionary<int, TerrainImage> data;
            private readonly Dictionary<int, Terrain> terrain;
            private int? currentId;

            private readonly TextBox tbName;
            private readonly TextBox tbAnimationView;
            private readonly TextBox tbAnimationViewSpring;
            private readonly TextBox tbAnimationViewSummer;
            private readonly TextBox tbAnimationViewAutumn;
            private readonly TextBox tbAnimationViewWinter;
            private readonly TextBox tbAnimationViewSnow;
            private readonly TextBox tbAnimationDetail;
            private readonly TextBox tbAnimationDetailSpring;
            private readonly TextBox tbAnimationDetailSummer;
            private readonly TextBox tbAnimationDetailAutumn;
            private readonly TextBox tbAnimationDetailWinter;
            private readonly TextBox tbAnimationDetailSnow;

            public TabPageTerrainImage(
                UIEditGameWorldDatabaseWindow bw, Dictionary<int, TerrainImage> data, Dictionary<int, Terrain> terrain)
                : base(bw)
            {
                this.data = data;
                this.terrain = terrain;

                this.init(w.terrain_image.text).setAutoSizeP();

                var (sc, pl, lv) = createIndexControl(onAddButtonClicked, onDeleteButtonClicked);

                lv.addColumn(w.id)
                    .addColumn(w.name);

                lv.Click += (s, e) =>
                {
                    if (lv.FocusedItem == null) return;

                    onListViewClicked(lv, (int)lv.FocusedItem.Tag);
                };

                var p2 = new TableLayoutPanel()
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 2
                }.addRowStyle(80).addRowStyle(20).addTo(sc);

                var gb = new GroupBox()
                {
                    Dock = DockStyle.Fill,
                    Text = w.scene_edit_game_world.property
                }.addTo(p2);

                var p3 = new TableLayoutPanel()
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 2,
                    AutoScroll = true
                }.addTo(gb);

                var width = 320;
                var height = 48;

                new Label().init(w.name + ":").setAutoSize().setRightCenter().addTo(p3);
                tbName = new TextBox().refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init(w.terrain_image.animation_view + ":").setAutoSize().setRightCenter().addTo(p3);
                tbAnimationView = new TextBox() { Multiline = true, Width = width, Height = height }.refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init(w.terrain_image.animation_view_spring + ":").setAutoSize().setRightCenter().addTo(p3);
                tbAnimationViewSpring = new TextBox() { Multiline = true, Width = width, Height = height }.refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init(w.terrain_image.animation_view_summer + ":").setAutoSize().setRightCenter().addTo(p3);
                tbAnimationViewSummer = new TextBox() { Multiline = true, Width = width, Height = height }.refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init(w.terrain_image.animation_view_autumn + ":").setAutoSize().setRightCenter().addTo(p3);
                tbAnimationViewAutumn = new TextBox() { Multiline = true, Width = width, Height = height }.refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init(w.terrain_image.animation_view_winter + ":").setAutoSize().setRightCenter().addTo(p3);
                tbAnimationViewWinter = new TextBox() { Multiline = true, Width = width, Height = height }.refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init(w.terrain_image.animation_view_snow + ":").setAutoSize().setRightCenter().addTo(p3);
                tbAnimationViewSnow = new TextBox() { Multiline = true, Width = width, Height = height }.refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init(w.terrain_image.animation_detail + ":").setAutoSize().setRightCenter().addTo(p3);
                tbAnimationDetail = new TextBox() { Multiline = true, Width = width, Height = height }.refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init(w.terrain_image.animation_detail_spring + ":").setAutoSize().setRightCenter().addTo(p3);
                tbAnimationDetailSpring = new TextBox() { Multiline = true, Width = width, Height = height }.refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init(w.terrain_image.animation_detail_summer + ":").setAutoSize().setRightCenter().addTo(p3);
                tbAnimationDetailSummer = new TextBox() { Multiline = true, Width = width, Height = height }.refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init(w.terrain_image.animation_detail_autumn + ":").setAutoSize().setRightCenter().addTo(p3);
                tbAnimationDetailAutumn = new TextBox() { Multiline = true, Width = width, Height = height }.refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init(w.terrain_image.animation_detail_winter + ":").setAutoSize().setRightCenter().addTo(p3);
                tbAnimationDetailWinter = new TextBox() { Multiline = true, Width = width, Height = height }.refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init(w.terrain_image.animation_detail_snow + ":").setAutoSize().setRightCenter().addTo(p3);
                tbAnimationDetailSnow = new TextBox() { Multiline = true, Width = width, Height = height }.refreshListViewOnClick(lv, refresh).addTo(p3);

                new Label().init("").addTo(p3);

                initData(lv);

                lv.autoResizeColumns();
            }

            private void initData(ListView lv) => data.Values.ToList().ForEach(o => lv.Items.Add(setRow(new ListViewItem() { Tag = o.id }, o)));

            private void onAddButtonClicked(ListView lv)
            {
                var max = data.getMaxId(0);

                if (++max > int.MaxValue) return;

                var oo = new TerrainImage()
                {
                    id = max,
                    name = w.terrain_image.text,
                    animationView = new List<TileAnimationFrame>(),
                    animationViewSpring = new List<TileAnimationFrame>(),
                    animationViewSummer = new List<TileAnimationFrame>(),
                    animationViewAutumn = new List<TileAnimationFrame>(),
                    animationViewWinter = new List<TileAnimationFrame>(),
                    animationViewSnow = new List<TileAnimationFrame>(),
                    animationDetail = new List<TileAnimationFrame>(),
                    animationDetailSpring = new List<TileAnimationFrame>(),
                    animationDetailSummer = new List<TileAnimationFrame>(),
                    animationDetailAutumn = new List<TileAnimationFrame>(),
                    animationDetailWinter = new List<TileAnimationFrame>(),
                    animationDetailSnow = new List<TileAnimationFrame>()
                };

                data[oo.id] = oo;

                var lvi = new ListViewItem()
                {
                    Tag = oo.id
                };

                lv.Items.Add(setRow(lvi, oo));

                lv.autoResizeColumns();
            }

            private void onDeleteButtonClicked(ListView lv, int id)
            {
                lv.Items.Remove(lv.Items.Cast<ListViewItem>().First(o => (int)o.Tag == id));

                data.Remove(id);

                if (currentId == id) currentId = null;

                lv.autoResizeColumns();
            }

            private static ListViewItem setRow(ListViewItem lvi, TerrainImage o)
            {
                lvi.Text = o.id.ToString();

                lvi.addColumn(o.name);

                return lvi;
            }

            private void refresh(ListView lv)
            {
                if (currentId != null)
                {
                    var oo = data[(int)currentId];

                    oo.name = tbName.Text;

                    var showDialog = new Action<string>(text =>
                    {
                        new UIDialog(window.gameSystem, "alert", text).ShowDialog();
                    });

                    try
                    {
                        oo.animationView = tbAnimationView.Text.toAnimationFrameList();
                    }
                    catch
                    {
                        showDialog(w.terrain_image.animation_view_spring);
                    }

                    try
                    {
                        oo.animationViewSpring = tbAnimationViewSpring.Text.toAnimationFrameList();
                    }
                    catch
                    {
                        showDialog(w.terrain_image.animation_view_spring);
                    }

                    try
                    {
                        oo.animationViewSummer = tbAnimationViewSummer.Text.toAnimationFrameList();
                    }
                    catch
                    {
                        showDialog(w.terrain_image.animation_view_summer);
                    }

                    try
                    {
                        oo.animationViewAutumn = tbAnimationViewAutumn.Text.toAnimationFrameList();
                    }
                    catch
                    {
                        showDialog(w.terrain_image.animation_view_autumn);
                    }

                    try
                    {
                        oo.animationViewWinter = tbAnimationViewWinter.Text.toAnimationFrameList();
                    }
                    catch
                    {
                        showDialog(w.terrain_image.animation_view_winter);
                    }

                    try
                    {
                        oo.animationViewSnow = tbAnimationViewSnow.Text.toAnimationFrameList();
                    }
                    catch
                    {
                        showDialog(w.terrain_image.animation_view_snow);
                    }

                    try
                    {
                        oo.animationDetail = tbAnimationDetail.Text.toAnimationFrameList();
                    }
                    catch
                    {
                        showDialog(w.terrain_image.animation_detail);
                    }

                    try
                    {
                        oo.animationDetailSpring = tbAnimationDetailSpring.Text.toAnimationFrameList();
                    }
                    catch
                    {
                        showDialog(w.terrain_image.animation_detail_spring);
                    }

                    try
                    {
                        oo.animationDetailSummer = tbAnimationDetailSummer.Text.toAnimationFrameList();
                    }
                    catch
                    {
                        showDialog(w.terrain_image.animation_detail_summer);
                    }

                    try
                    {
                        oo.animationDetailAutumn = tbAnimationDetailAutumn.Text.toAnimationFrameList();
                    }
                    catch
                    {
                        showDialog(w.terrain_image.animation_detail_autumn);
                    }

                    try
                    {
                        oo.animationDetailWinter = tbAnimationDetailWinter.Text.toAnimationFrameList();
                    }
                    catch
                    {
                        showDialog(w.terrain_image.animation_detail_winter);
                    }

                    try
                    {
                        oo.animationDetailSnow = tbAnimationDetailSnow.Text.toAnimationFrameList();
                    }
                    catch
                    {
                        showDialog(w.terrain_image.animation_detail_snow);
                    }

                    var lvi = lv.Items.Cast<ListViewItem>().FirstOrDefault(o => (int)o.Tag == currentId);

                    if (lvi != null)
                    {
                        lvi.SubItems.Clear();

                        setRow(lvi, oo);

                        lv.autoResizeColumns();
                    }
                }
            }

            private void onListViewClicked(ListView lv, int id)
            {
                refresh(lv);

                currentId = id;
                var o = data[id];

                tbName.Text = o.name;
                tbAnimationView.Text = o.animationView.toString();
                tbAnimationViewSpring.Text = o.animationViewSpring.toString();
                tbAnimationViewSummer.Text = o.animationViewSummer.toString();
                tbAnimationViewAutumn.Text = o.animationViewAutumn.toString();
                tbAnimationViewWinter.Text = o.animationViewWinter.toString();
                tbAnimationViewSnow.Text = o.animationViewSnow.toString();
                tbAnimationDetail.Text = o.animationDetail.toString();
                tbAnimationDetailSpring.Text = o.animationDetailSpring.toString();
                tbAnimationDetailSummer.Text = o.animationDetailSummer.toString();
                tbAnimationDetailAutumn.Text = o.animationDetailAutumn.toString();
                tbAnimationDetailWinter.Text = o.animationDetailWinter.toString();
                tbAnimationDetailSnow.Text = o.animationDetailSnow.toString();
            }
        }
    }
}
