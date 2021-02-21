using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinLibrary.Graphic;

namespace WinLibrary
{
    public abstract class FormGame : Form
    {
        protected StateManager gameRoot;

        protected SpriteRectangle background;

        protected GameGraphic gameGraphic;

        protected abstract IFormGameOption formGameOption { get; }

        public Dispatcher dispatcher { get; } = new Dispatcher();

        public FormGame()
        {
            initSystem();
        }

        private void initSystem()
        {
            gameRoot = new StateManager();

            gameGraphic = new GameGraphic()
            {
                defaultFontName = "MingLiU",
                defaultFontSize = 12
            };
        }

        protected void refreshBuffer()
        {
            background = new SpriteRectangle()
            {
                color = Color.Black,
                isFill = true,
                size = ClientSize
            };

            bufferGraphic = new BufferedGraphicsContext().Allocate(CreateGraphics(), DisplayRectangle);
            gameGraphic.g = bufferGraphic.Graphics;
        }

        protected override void OnLoad(EventArgs e)
        {
            refreshBuffer();
            
            run();
        }

        private BufferedGraphics bufferGraphic;

        private bool isDragging;
        private int dragCount;
        private Point dragStartPoint;
        private Point dragPoint;
        private int dragPointOffsetX => dragPoint.X - dragStartPoint.X;
        private int dragPointOffsetY => dragPoint.Y - dragStartPoint.Y;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            gameRoot.onMouseMoved(e);

            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;

                if (++dragCount > 0)
                {
                    if (dragCount == 1)
                    {
                        dragStartPoint = e.Location;
                    }
                    else
                    {
                        dragPoint = e.Location;
                        gameRoot.onMouseDragging(e, new Point(dragPointOffsetX * 2, dragPointOffsetY * 2));
                        dragCount = 0;
                    }
                }
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (!isDragging) gameRoot.onMouseClicked(e);

            isDragging = false;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            gameRoot.onMousePressed(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            dragCount = 0;

            gameRoot.onMouseReleased(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            gameRoot.onMouseWheelScrolled(e);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            gameRoot.onKeyPressed(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            gameRoot.onKeyPressing(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            gameRoot.onKeyReleased(e);
        }

        private int count;
        private DateTime lastUpdateTime = DateTime.Now;
        private readonly TimeSpan oneSecondTimeSpan = TimeSpan.FromSeconds(1);
        private readonly TimeSpan updateTimeSpan = TimeSpan.FromSeconds(1d / 60);
        private readonly TimeSpan drawTimeSpan = TimeSpan.FromSeconds(1d / 60);
        private  TimeSpan updateTimeCost = TimeSpan.Zero;
        private  TimeSpan drawTimeCost = TimeSpan.Zero;

        protected override void OnPaint(PaintEventArgs e)
        {
            draw();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        public void run()
        {
            Task.Run(() =>
            {
                var now = DateTime.Now;
                var lastUpdateTime = now;
                var lastDrawTime = now;
                var updateAction = new Action(update);

                while (true)
                {
                    try
                    {
                        now = DateTime.Now;

                        if (now - lastUpdateTime >= (updateTimeSpan - updateTimeCost))
                        {
                            BeginInvoke(updateAction);
                            lastUpdateTime = now;
                        }

                        if (now - lastDrawTime >= (drawTimeSpan - drawTimeCost))
                        {
                            Invalidate();
                            lastDrawTime = now;
                        }

                        Thread.Sleep(1);
                    }
                    catch
                    {
                        break;
                    }
                }
            });
        }

        private void update()
        {
            var now = DateTime.Now;

            dispatcher.update();

            gameRoot.onUpdate();

            updateTimeCost = DateTime.Now - now;
        }

        private void draw()
        {
            var now = DateTime.Now;

            gameGraphic.drawRectangle(background);

            gameRoot.onDraw();

            bufferGraphic.Render();

            ++count;

            if (now - lastUpdateTime >= oneSecondTimeSpan)
            {
                Text = $"{formGameOption.title} FPS:{count}";
                count = 0;
                lastUpdateTime = now;
            }

            drawTimeCost = DateTime.Now - now;
        }

        public class Dispatcher
        {
            private readonly Queue<Action> actions = new Queue<Action>();
            private readonly List<Action> list = new List<Action>();

            public void update()
            {
                lock (actions)
                {
                    if (actions.Any())
                    {
                        list.AddRange(actions);

                        actions.Clear();
                    }
                }

                if (list.Any())
                {
                    list.ForEach(o => o());

                    list.Clear();
                }
            }

            public void invoke(Action a)
            {
                lock (actions)
                {
                    actions.Enqueue(a);
                }
            }
        }
    }
}
