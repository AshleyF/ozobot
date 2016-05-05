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
            const int factor = 2; // for MacBook TODO: API to get this?
            var point = PointToScreen(new Point(pictureBox.Left + pictureBox.Width / 2, pictureBox.Top + pictureBox.Height / 2));
            this.Text = $"Point {point.X}, {point.Y}";
            var screen = Screen.FromPoint(point);
            var bmp = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            Graphics.FromImage(bmp).CopyFromScreen((screen.Bounds.X + point.X) * factor,
                                                   (screen.Bounds.Y + point.Y) * factor,
                                                   0, 0, new Size(1, 1), CopyPixelOperation.SourceCopy);

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

        private void Clear()
        {
            textBox.Clear();
            last = '\0';
        }

        private DateTime lastTime = new DateTime();

        private void AddColor(Color c)
        {
            var d = DetectColor(c);
            // Console.WriteLine($"Color: {d}");
            if (d != '?' && d != last)
            {
                var now = DateTime.Now;
                var ellapsed = (now - lastTime).TotalMilliseconds;
                // Console.WriteLine($"{ellapsed}"); // seems to be ~50ms
                lastTime = now;
                if (last == 'W' && ellapsed > 100) Clear(); // beginning of seqence
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
                var ellapsed = (DateTime.Now - lastTime).TotalMilliseconds;
                if (last == 'W' && ellapsed > 100) // end of sequence
                {
                    var txt = textBox.Text;
                    Console.WriteLine($"Test: {txt}");
                    if (txt.Length > 0 && txt[txt.Length - 1] == 'W')
                    {
                        txt = txt.Substring(0, txt.Length - 1);
                        textBox.Text = txt;
                        Clipboard.SetText(txt);
                    }
                }
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
    }
}