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

        private string Dasm(string colors)
        {
            try
            {
                var dasm = "";
                var v = 0;
                var skip = 8;
                for (var i = 0; i < colors.Length; i++)
                {
                    var c = colors[i];
                    if (c == 'W') c = colors[i - 1];
                    v *= 7;
                    switch(c)
                    {
                        case 'R': v += 1; break;
                        case 'G': v += 2; break;
                        case 'Y': v += 3; break;
                        case 'B': v += 4; break;
                        case 'M': v += 5; break;
                        case 'C': v += 6; break;
                    }
                    if ((i + 1) % 3 == 0)
                    {
                        switch(v)
                        {
                            //      01 80 0a 97 ... ba 03 97 // true if ... fi
                            //         80 0c 97 ... ba 03 97 // if ... fi
                            // 92 01 = 80 0a 97 ... ba 03 97 // surface red = if ... fi
                            //    7f 00 8c 7f 00 8c 7f 00 8c led // set random light
                            // 20 10 a9 8c 40 move // move random distance (16-32) speed 64

                            // constrain 1 to between x10 and x20
                            // 20 10 01  a9 96 a9 aa 96

                            // random light 10x
                            // 0a 94 00 9d 80 12 97
                            // 7f 00 rand
                            // 7f 00 rand
                            // 7f 00 rand
                            // led
                            // 01 86 ba ee 97 96

                            case 0x8a: dasm += "not "   ; break;
                            case 0x83: dasm += "~ "     ; break;
                            case 0x85: dasm += "+ "     ; break;
                            case 0x86: dasm += "- "     ; break;
                            case 0x87: dasm += "* "     ; break;
                            case 0x88: dasm += "/ "     ; break;
                            case 0x89: dasm += "mod "   ; break;
                            case 0x8c: dasm += "rand "  ; break;
                            case 0x90: dasm += "call "  ; break;
                            case 0x91: dasm += ";   "   ; break;
                            case 0x98: dasm += "turn "  ; break;
                            case 0x9b: dasm += "wait "  ; break;
                            case 0x9c: dasm += ">= "    ; break; // <  is >= not
                            case 0x9d: dasm += "> "     ; break; // <= is >  not
                            case 0x9e: dasm += "move "  ; break;
                            case 0x9f: dasm += "wheels "; break;
                            case 0xa4: dasm += "= "  ; break;
                            case 0xa6: dasm += "poke "  ; break;
                            case 0xa7: dasm += "peek "  ; break;
                            case 0xae: dasm += "end "   ; break;
                            case 0xb8: dasm += "led "   ; break;
                            default:
                                if (skip-- <= 0 && i < colors.Length - 6) // skip first 8 (frame, version, length) and last two bytes (checksum, frame)
                                    dasm += String.Format("{0:x2} ", v); break;
                        }
                        v = 0;
                    }
                }
                return dasm;
            } catch(Exception ex)
            {
                return colors;
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
                    if (txt.Length > 0 && txt[txt.Length - 1] == 'W')
                    {
                        var dasm = Dasm(txt);
                        textBox.Text = dasm;
                        Clipboard.SetText(dasm);
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
