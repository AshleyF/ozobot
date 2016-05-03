using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace FlashReader
{
    public partial class Form : System.Windows.Forms.Form
    {
        private Color Sample()
        {
            var point = PointToScreen(new Point(Width / 2, Height / 2));
            var screen = Screen.FromPoint(point);
            var bmp = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            Graphics.FromImage(bmp).CopyFromScreen(screen.Bounds.X + point.X, screen.Bounds.Y + point.Y, 0, 0, new Size(1, 1), CopyPixelOperation.SourceCopy);

            return bmp.GetPixel(0, 0);
        }

        private char last = '\0';

        private char DetectColor(Color c)
        {
            var r = c.R == 255;
            var g = c.G == 255;
            var b = c.B == 255;

            if (r && g && b) return 'W';
            if (r && g) return 'Y';
            if (r && b) return 'M';
            if (g && b) return 'C';
            if (r) return 'R';
            if (g) return 'G';
            if (b) return 'B';
            if (c.R == 0 && c.G == 0 && c.B == 0) return 'K';
            return '?';
        }

        private void AddColor(Color c)
        {
            var d = DetectColor(c);
            // Console.WriteLine($"Color: {d}");
            if (d != '?' && d != last)
            {
                this.textBox.Text += d;
                last = d;
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            try
            {
                var color = Sample();
                BackColor = Color.FromArgb(255, color);
                var grayScale = (color.R * 0.3) + (color.G * 0.59) + (color.B * 0.11);
                label.ForeColor = grayScale < 128 ? Color.White : Color.Black; // white on black, red or blue
                AddColor(color);
                // Console.WriteLine($"R: {color.R} G: {color.G} B: {color.B}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex}");
            }
        }

        public Form()
        {
            InitializeComponent();
        }

        private void Clear()
        {
            textBox.Clear();
            last = '\0';
        }

        private void textBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Clear();
        }
        private void Form_DoubleClick(object sender, EventArgs e)
        {
            Clear();
        }
    }
}