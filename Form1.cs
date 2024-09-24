using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FloatingDots
{
    public partial class Form1 : Form
    {
        private class Dot
        {
            public PointF Position { get; set; }
            public PointF Velocity { get; set; }
        }

        private List<Dot> _dots;
        private Timer _timer;
        private Random _random;
        private BufferedGraphicsContext _bufferedGraphicsContext;
        private BufferedGraphics _bufferedGraphics;
        private PointF _mousePosition;

        public Form1()
        {
            InitializeComponent();

            _dots = new List<Dot>();
            _random = new Random();
            _mousePosition = new PointF(-1, -1);

            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black
            };
            this.Controls.Add(panel);

            _timer = new Timer
            {
                Interval = 20
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            for (int i = 0; i < 100; i++) //Adjust the number of dots
            {
                _dots.Add(new Dot
                {
                    Position = new PointF(_random.Next(panel.Width), _random.Next(panel.Height)),
                    Velocity = new PointF(
                        (float)(_random.NextDouble() * 4 - 2), //Random velocity in x direction with a range [-2, 2]
                        (float)(_random.NextDouble() * 4 - 2)
                    )
                });
            }

            panel.Paint += Panel_Paint;

            //Initialize BufferedGraphics
            _bufferedGraphicsContext = BufferedGraphicsManager.Current;
            _bufferedGraphics = _bufferedGraphicsContext.Allocate(panel.CreateGraphics(), panel.ClientRectangle);

            panel.MouseMove += Panel_MouseMove;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Panel panel = this.Controls.OfType<Panel>().FirstOrDefault();

            if (panel != null)
            {
                foreach (var dot in _dots)
                {
                    //Update dot position
                    dot.Position = new PointF(
                        dot.Position.X + dot.Velocity.X,
                        dot.Position.Y + dot.Velocity.Y
                    );

                    //Reflect dot off the panel edges if necessary
                    if (dot.Position.X < 0 || dot.Position.X >= panel.Width)
                    {
                        dot.Velocity = new PointF(-dot.Velocity.X, dot.Velocity.Y);
                        dot.Position = new PointF(
                            MathHelper.Clamp(dot.Position.X, 0, panel.Width - 1),
                            dot.Position.Y
                        );
                    }
                    if (dot.Position.Y < 0 || dot.Position.Y >= panel.Height)
                    {
                        dot.Velocity = new PointF(dot.Velocity.X, -dot.Velocity.Y);
                        dot.Position = new PointF(
                            dot.Position.X,
                            MathHelper.Clamp(dot.Position.Y, 0, panel.Height - 1)
                        );
                    }
                }

                //Request a repaint
                panel.Invalidate();
            }
        }

        private void Panel_Paint(object sender, PaintEventArgs e)
        {
            Panel panel = sender as Panel;
            if (panel == null) return;

            //Use BufferedGraphics for rendering
            Graphics g = _bufferedGraphics.Graphics;
            g.Clear(Color.Black); // Clear the buffer

            using (Pen pen = new Pen(Color.White, 1))
            {
                foreach (var dot in _dots)
                {
                    g.FillEllipse(Brushes.White, dot.Position.X, dot.Position.Y, 3, 3); // Draw dot with size 3x3

                    foreach (var otherDot in _dots)
                    {
                        if (dot != otherDot)
                        {
                            float distance = Distance(dot.Position, otherDot.Position);
                            if (distance < 100) //Draw line if the dot is within 100 units
                            {
                                g.DrawLine(pen, dot.Position, otherDot.Position);
                            }
                        }
                    }
                }

                if (_mousePosition.X >= 0 && _mousePosition.Y >= 0) //Ensure valid mouse position
                {
                    g.FillEllipse(Brushes.Red, _mousePosition.X - 1.5f, _mousePosition.Y - 1.5f, 3, 3); // Draw mouse dot

                    foreach (var dot in _dots)
                    {
                        float distance = Distance(_mousePosition, dot.Position);
                        if (distance < 100)
                        {
                            g.DrawLine(Pens.Red, _mousePosition, dot.Position);
                        }
                    }
                }
            }

            //Render the buffer
            _bufferedGraphics.Render(e.Graphics);
        }

        private void Panel_MouseMove(object sender, MouseEventArgs e)
        {
            _mousePosition = new PointF(e.X, e.Y);
        }

        private float Distance(PointF p1, PointF p2)
        {
            return (float)Math.Sqrt(
                (p2.X - p1.X) * (p2.X - p1.X) +
                (p2.Y - p1.Y) * (p2.Y - p1.Y)
            );
        }
    }

    public static class MathHelper
    {
        public static float Clamp(float value, float min, float max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
