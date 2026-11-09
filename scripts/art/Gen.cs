// Home Before Dark - procedural pixel art (APNG scenes, logo, UI) and audio generator.
// Compiled at runtime by scripts\make-art.ps1 (C# 5, .NET Framework).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace HBD
{
    // ------------------------------------------------------------------ canvas
    public class Canvas
    {
        public int W, H;
        public int[] P;
        public Canvas(int w, int h) { W = w; H = h; P = new int[w * h]; }
        public Canvas Copy() { Canvas c = new Canvas(W, H); Array.Copy(P, c.P, P.Length); return c; }
        public static int Cl(int v) { return v < 0 ? 0 : (v > 255 ? 255 : v); }
        public static int C(int r, int g, int b) { return C(255, r, g, b); }
        public static int C(int a, int r, int g, int b) { return (Cl(a) << 24) | (Cl(r) << 16) | (Cl(g) << 8) | Cl(b); }
        public static int R(int c) { return (c >> 16) & 255; }
        public static int G(int c) { return (c >> 8) & 255; }
        public static int B(int c) { return c & 255; }
        public static int A(int c) { return (c >> 24) & 255; }
        public static int Mix(int a, int b, double t)
        {
            if (t < 0) t = 0; if (t > 1) t = 1;
            return C((int)(R(a) + (R(b) - R(a)) * t), (int)(G(a) + (G(b) - G(a)) * t), (int)(B(a) + (B(b) - B(a)) * t));
        }
        public static int Shade(int c, double f) { return C(A(c), (int)(R(c) * f), (int)(G(c) * f), (int)(B(c) * f)); }
        public bool In(int x, int y) { return x >= 0 && y >= 0 && x < W && y < H; }
        public void Set(int x, int y, int c) { if (In(x, y)) P[y * W + x] = c; }
        public int Get(int x, int y) { return In(x, y) ? P[y * W + x] : 0; }
        public void Blend(int x, int y, int c, double a)
        {
            if (!In(x, y) || a <= 0) return;
            if (a > 1) a = 1;
            int i = y * W + x; int d = P[i]; int da = A(d);
            if (da == 0) { P[i] = C((int)(a * 255), R(c), G(c), B(c)); return; }
            int r = (int)(R(d) * (1 - a) + R(c) * a);
            int g = (int)(G(d) * (1 - a) + G(c) * a);
            int b = (int)(B(d) * (1 - a) + B(c) * a);
            P[i] = C((int)(da + (255 - da) * a), r, g, b);
        }
        public void Rect(int x, int y, int w, int h, int c) { for (int yy = y; yy < y + h; yy++) for (int xx = x; xx < x + w; xx++) Set(xx, yy, c); }
        public void BlendRect(int x, int y, int w, int h, int c, double a) { for (int yy = y; yy < y + h; yy++) for (int xx = x; xx < x + w; xx++) Blend(xx, yy, c, a); }
        public void Glow(double cx, double cy, double r, int c, double a)
        {
            for (int y = (int)(cy - r) - 1; y <= (int)(cy + r) + 1; y++)
                for (int x = (int)(cx - r) - 1; x <= (int)(cx + r) + 1; x++)
                {
                    double d = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / r;
                    if (d < 1) Blend(x, y, c, a * (1 - d) * (1 - d));
                }
        }
        public void Disc(double cx, double cy, double r, int c, double a)
        {
            for (int y = (int)(cy - r) - 1; y <= (int)(cy + r) + 1; y++)
                for (int x = (int)(cx - r) - 1; x <= (int)(cx + r) + 1; x++)
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r) Blend(x, y, c, a);
        }
        public void Over(Canvas t)
        {
            for (int i = 0; i < P.Length; i++)
            {
                int a = A(t.P[i]);
                if (a == 255) P[i] = t.P[i];
                else if (a > 0) Blend(i % W, i / W, t.P[i], a / 255.0);
            }
        }
        public Canvas Scaled(int s)
        {
            if (s == 1) return this;
            Canvas o = new Canvas(W * s, H * s);
            for (int y = 0; y < o.H; y++) for (int x = 0; x < o.W; x++) o.P[y * o.W + x] = P[(y / s) * W + x / s];
            return o;
        }
        public Bitmap ToBitmap()
        {
            Bitmap bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb);
            BitmapData d = bmp.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            if (d.Stride == W * 4) Marshal.Copy(P, 0, d.Scan0, P.Length);
            else for (int y = 0; y < H; y++) Marshal.Copy(P, y * W, new IntPtr(d.Scan0.ToInt64() + (long)y * d.Stride), W);
            bmp.UnlockBits(d);
            return bmp;
        }
        public void Save(string path, int scale)
        {
            using (Bitmap b = Scaled(scale).ToBitmap()) b.Save(path, ImageFormat.Png);
        }
    }

    // ------------------------------------------------------------------ APNG writer
    public static class Apng
    {
        static uint[] table;
        static uint Crc(byte[] type, byte[] data)
        {
            if (table == null)
            {
                table = new uint[256];
                for (uint n = 0; n < 256; n++) { uint c = n; for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1; table[n] = c; }
            }
            uint crc = 0xFFFFFFFFu;
            foreach (byte b in type) crc = table[(crc ^ b) & 0xFF] ^ (crc >> 8);
            foreach (byte b in data) crc = table[(crc ^ b) & 0xFF] ^ (crc >> 8);
            return crc ^ 0xFFFFFFFFu;
        }
        static void BE(Stream s, uint v) { s.WriteByte((byte)(v >> 24)); s.WriteByte((byte)(v >> 16)); s.WriteByte((byte)(v >> 8)); s.WriteByte((byte)v); }
        static void Chunk(Stream s, string type, byte[] data)
        {
            byte[] t = Encoding.ASCII.GetBytes(type);
            BE(s, (uint)data.Length); s.Write(t, 0, 4); s.Write(data, 0, data.Length); BE(s, Crc(t, data));
        }
        static List<KeyValuePair<string, byte[]>> Parse(byte[] png)
        {
            List<KeyValuePair<string, byte[]>> list = new List<KeyValuePair<string, byte[]>>();
            int p = 8;
            while (p + 8 <= png.Length)
            {
                int len = (png[p] << 24) | (png[p + 1] << 16) | (png[p + 2] << 8) | png[p + 3];
                string type = Encoding.ASCII.GetString(png, p + 4, 4);
                byte[] data = new byte[len]; Array.Copy(png, p + 8, data, 0, len);
                list.Add(new KeyValuePair<string, byte[]>(type, data));
                p += 12 + len;
            }
            return list;
        }
        public static void Write(string path, List<Canvas> frames, int delayNum, int delayDen)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                byte[] sig = { 137, 80, 78, 71, 13, 10, 26, 10 };
                fs.Write(sig, 0, 8);
                uint seq = 0; byte[] ihdr0 = null;
                for (int f = 0; f < frames.Count; f++)
                {
                    byte[] png;
                    using (Bitmap b = frames[f].ToBitmap())
                    using (MemoryStream ms = new MemoryStream()) { b.Save(ms, ImageFormat.Png); png = ms.ToArray(); }
                    List<KeyValuePair<string, byte[]>> chunks = Parse(png);
                    byte[] ihdr = null;
                    foreach (KeyValuePair<string, byte[]> c in chunks) if (c.Key == "IHDR") ihdr = c.Value;
                    if (f == 0)
                    {
                        ihdr0 = ihdr; Chunk(fs, "IHDR", ihdr);
                        MemoryStream ac = new MemoryStream(); BE(ac, (uint)frames.Count); BE(ac, 0); Chunk(fs, "acTL", ac.ToArray());
                    }
                    else
                    {
                        for (int i = 0; i < 13; i++) if (ihdr[i] != ihdr0[i]) throw new Exception("IHDR mismatch in frame " + f);
                    }
                    MemoryStream fc = new MemoryStream();
                    BE(fc, seq++); BE(fc, (uint)frames[f].W); BE(fc, (uint)frames[f].H); BE(fc, 0); BE(fc, 0);
                    fc.WriteByte((byte)(delayNum >> 8)); fc.WriteByte((byte)delayNum);
                    fc.WriteByte((byte)(delayDen >> 8)); fc.WriteByte((byte)delayDen);
                    fc.WriteByte(0); fc.WriteByte(0);
                    Chunk(fs, "fcTL", fc.ToArray());
                    foreach (KeyValuePair<string, byte[]> c in chunks)
                    {
                        if (c.Key != "IDAT") continue;
                        if (f == 0) Chunk(fs, "IDAT", c.Value);
                        else { MemoryStream fd = new MemoryStream(); BE(fd, seq++); fd.Write(c.Value, 0, c.Value.Length); Chunk(fs, "fdAT", fd.ToArray()); }
                    }
                }
                Chunk(fs, "IEND", new byte[0]);
            }
        }
    }

    // ------------------------------------------------------------------ scenes
    public enum Season { Winter, Spring, Summer, Autumn, Neutral }

    public class Scene
    {
        public const int W = 480, H = 270;
        Season S; Random R; int N;
        Canvas sky, fg;
        int skyTop, skyMid, skyBot, horizonCol;
        int groundY = 246, cabinX = 350;
        int chimX, chimTop, winX0, winY0;
        List<double[]> stars = new List<double[]>();
        List<double[]> parts = new List<double[]>();
        List<int> partCols = new List<int>();

        int Rate = 1;   // how many base animation loops fit in N frames (keeps motion speed for long cycles)
        public Scene(Season s, int seed, int frames) : this(s, seed, frames, 1) { }
        int seedBase;
        public Scene(Season s, int seed, int frames, int rate) { S = s; seedBase = seed; R = new Random(seed * 7 + (int)s); N = frames; Rate = rate; Build(); }

        static double TAU = Math.PI * 2;
        bool Night { get { return S == Season.Winter || S == Season.Summer || S == Season.Neutral; } }

        int SkyAt(int y)
        {
            double t = y / (double)(H * 0.93);
            if (t < 0.55) return Canvas.Mix(skyTop, skyMid, t / 0.55);
            return Canvas.Mix(skyMid, skyBot, (t - 0.55) / 0.45);
        }

        void Build()
        {
            switch (S)
            {
                case Season.Winter: skyTop = Canvas.C(8, 12, 34); skyMid = Canvas.C(22, 40, 78); skyBot = Canvas.C(70, 110, 146); break;
                case Season.Spring: skyTop = Canvas.C(28, 30, 76); skyMid = Canvas.C(108, 76, 138); skyBot = Canvas.C(236, 158, 136); break;
                case Season.Summer: skyTop = Canvas.C(8, 16, 44); skyMid = Canvas.C(24, 52, 88); skyBot = Canvas.C(58, 106, 110); break;
                case Season.Autumn: skyTop = Canvas.C(40, 26, 58); skyMid = Canvas.C(150, 64, 76); skyBot = Canvas.C(246, 156, 84); break;
                default: skyTop = Canvas.C(10, 14, 38); skyMid = Canvas.C(26, 46, 84); skyBot = Canvas.C(62, 100, 120); break;
            }
            horizonCol = skyBot;
            sky = new Canvas(W, H);
            for (int y = 0; y < H; y++) { int c = SkyAt(y); for (int x = 0; x < W; x++) sky.P[y * W + x] = c; }

            // celestial body (static)
            if (S == Season.Winter)
            {
                for (int r = 26; r >= 10; r -= 2) sky.Disc(390, 48, r, Canvas.C(200, 225, 255), 0.055);
                sky.Disc(390, 48, 9, Canvas.C(235, 242, 255), 1);
                sky.Disc(387, 45, 2.5, Canvas.C(205, 218, 238), 1); sky.Disc(393, 51, 1.5, Canvas.C(212, 224, 242), 1);
            }
            else if (S == Season.Summer || S == Season.Neutral || S == Season.Spring)
            {
                int mx = S == Season.Spring ? 338 : 402, my = S == Season.Spring ? 40 : 46;
                for (int r = 20; r >= 8; r -= 2) sky.Disc(mx, my, r, Canvas.C(255, 240, 200), S == Season.Spring ? 0.03 : 0.045);
                for (int y = my - 8; y <= my + 8; y++) for (int x = mx - 8; x <= mx + 8; x++)
                {
                    bool inA = (x - mx) * (x - mx) + (y - my) * (y - my) <= 64;
                    bool inB = (x - mx - 4) * (x - mx - 4) + (y - my + 2) * (y - my + 2) <= 56;
                    if (inA && !inB) sky.Set(x, y, Canvas.C(250, 242, 214));
                }
            }
            else if (S == Season.Autumn)
            {
                for (int r = 70; r >= 26; r -= 4) sky.Disc(300, 180, r, Canvas.C(255, 190, 110), 0.06);
                sky.Disc(300, 180, 24, Canvas.C(255, 200, 120), 1);
                sky.Disc(300, 180, 20, Canvas.C(255, 222, 160), 1);
            }

            // twinkling stars (dynamic)
            int nStars = S == Season.Autumn ? 40 : (S == Season.Spring ? 90 : 260);
            double maxY = S == Season.Autumn ? 0.25 : (S == Season.Spring ? 0.38 : 0.62);
            for (int i = 0; i < nStars; i++)
                stars.Add(new double[] { R.Next(W), R.Next((int)(H * maxY)), 0.35 + R.NextDouble() * 0.55, 0.1 + R.NextDouble() * 0.35, 1 + R.Next(3), R.NextDouble() * TAU });

            fg = new Canvas(W, H);
            // independent RNG streams so terrain is identical in every season (only colours/snow/leaves change)
            R = new Random(1000 + seedBase); Mountains();
            R = new Random(2000 + seedBase); Forest();
            R = new Random(3000 + seedBase);
            Ground();
            Trees();
            Cabin();
            Particles();
        }

        // ---------------- mountains
        void Mountains()
        {
            int[][] cols; double[] cap; int minThick;
            switch (S)
            {
                case Season.Winter:
                    cols = new int[][] { new int[] { Canvas.C(58, 78, 112), Canvas.C(150, 175, 205) }, new int[] { Canvas.C(38, 54, 84), Canvas.C(200, 218, 238) }, new int[] { Canvas.C(24, 34, 58), Canvas.C(232, 240, 250) } };
                    cap = new double[] { 0.55, 0.55, 0.55 }; minThick = 2; break;
                case Season.Spring:
                    cols = new int[][] { new int[] { Canvas.C(104, 92, 146), Canvas.C(214, 200, 230) }, new int[] { Canvas.C(70, 76, 110), Canvas.C(184, 182, 214) }, new int[] { Canvas.C(42, 62, 66), Canvas.C(150, 160, 180) } };
                    cap = new double[] { 0.38, 0.16, 0 }; minThick = 0; break;
                case Season.Autumn:
                    cols = new int[][] { new int[] { Canvas.C(118, 74, 98), Canvas.C(240, 200, 192) }, new int[] { Canvas.C(84, 50, 64), Canvas.C(214, 172, 172) }, new int[] { Canvas.C(56, 36, 46), Canvas.C(150, 120, 120) } };
                    cap = new double[] { 0.38, 0.14, 0 }; minThick = 0; break;
                default:
                    cols = new int[][] { new int[] { Canvas.C(52, 74, 100), Canvas.C(146, 166, 190) }, new int[] { Canvas.C(34, 58, 72), Canvas.C(112, 132, 152) }, new int[] { Canvas.C(22, 42, 46), Canvas.C(90, 110, 120) } };
                    cap = new double[] { 0.32, 0.1, 0 }; minThick = 0; break;
            }
            double[][] L = { new double[] { 150, 55, 70 }, new double[] { 178, 45, 50 }, new double[] { 208, 35, 38 } };
            for (int li = 0; li < 3; li++)
            {
                double ph1 = R.NextDouble() * 10, ph2 = R.NextDouble() * 10;
                int[] ys = new int[W]; int minY = H;
                for (int x = 0; x < W; x++)
                {
                    double n = (1 - Math.Abs(Math.Sin(x / L[li][2] + ph1))) + (1 - Math.Abs(Math.Sin(x / (L[li][2] / 2.7) + ph2))) * 0.45 + 0.05 * Math.Sin(x / 3.1 + ph1);
                    ys[x] = (int)(L[li][0] - L[li][1] * n / 1.5);
                    if (ys[x] < minY) minY = ys[x];
                }
                int snowLine = minY + (int)(L[li][1] * cap[li]);
                int edge = 0;
                // atmospheric tint: far layers get a bit of horizon colour
                int rock = Canvas.Mix(cols[li][0], horizonCol, li == 0 ? 0.12 : (li == 1 ? 0.05 : 0));
                for (int x = 0; x < W; x++)
                {
                    for (int y = ys[x]; y < H; y++) fg.Set(x, y, rock);
                    edge = Math.Max(-4, Math.Min(4, edge + R.Next(-1, 2)));
                    int thick = cap[li] <= 0 ? 0 : (int)Math.Max(minThick, Math.Min(34, (snowLine - ys[x]) * 0.9 + 3 + edge));
                    if (cap[li] > 0 && minThick == 0 && ys[x] > snowLine) thick = 0;
                    for (int y = ys[x]; y < ys[x] + thick; y++) fg.Set(x, y, cols[li][1]);
                }
            }
        }

        // ---------------- pines
        int PineCol() { switch (S) { case Season.Winter: return Canvas.C(14, 30, 40); case Season.Spring: return Canvas.C(24, 50, 48); case Season.Autumn: return Canvas.C(32, 38, 34); default: return Canvas.C(14, 34, 30); } }
        void Pine(int tx, int tb, int th, int col, bool snow)
        {
            int tiers = Math.Max(2, th / 9);
            fg.Rect(tx + 3, tb - 4, 2, 6, Canvas.C(40, 28, 22));
            for (int t = 0; t < tiers; t++)
            {
                int wdt = (tiers - t) * 3 + 2; int yy = tb - 4 - t * 8;
                for (int row = 0; row < 9; row++)
                {
                    int half = wdt * (9 - row) / 9 + 1;
                    fg.Rect(tx + 4 - half, yy - row, 2 * half, 1, col);
                }
                if (snow) fg.Rect(tx + 4 - wdt / 2, yy - 2, (int)(wdt * 0.7) + 1, 1, Canvas.C(220, 235, 248));
                else fg.Rect(tx + 5 - wdt / 2, yy - 1, (int)(wdt * 0.5) + 1, 1, Canvas.Shade(col, 1.35));
            }
        }
        void Forest()
        {
            int col = PineCol();
            int back = Canvas.Mix(col, horizonCol, 0.28);
            for (int tx = -6; tx < W; tx += R.Next(5, 11)) Pine(tx, 240 + R.Next(0, 3), R.Next(18, 34), back, S == Season.Winter);
            for (int tx = -6; tx < W; tx += R.Next(7, 16))
            {
                if (tx > cabinX - 40 && tx < cabinX + 70) continue;            // clearing for the cabin
                int th = R.Next(26, 58);
                if (tx > 150 && tx < cabinX - 40) th = (int)(th * 0.6);
                Pine(tx, 244 + R.Next(0, 6), th, col, S == Season.Winter);
            }
        }

        // ---------------- ground
        void Ground()
        {
            int top, low;
            switch (S)
            {
                case Season.Winter: top = Canvas.C(214, 228, 244); low = Canvas.C(180, 200, 226); break;
                case Season.Spring: top = Canvas.C(92, 142, 74); low = Canvas.C(66, 112, 60); break;
                case Season.Autumn: top = Canvas.C(132, 98, 52); low = Canvas.C(104, 74, 42); break;
                default: top = Canvas.C(46, 92, 54); low = Canvas.C(34, 74, 44); break;
            }
            if (S == Season.Spring) { top = Canvas.Mix(top, horizonCol, 0.12); }
            for (int x = 0; x < W; x++)
            {
                int gy = 243 + (int)(1.5 * Math.Sin(x / 23.0) + Math.Sin(x / 7.0));
                for (int y = gy; y < H; y++) fg.Set(x, y, y < gy + 4 ? top : low);
            }
            // path from the door
            int doorX = cabinX + 10;
            for (int y = groundY; y < H; y++)
            {
                double t = (y - groundY) / (double)(H - groundY);
                int cx = (int)(doorX - 26 * t * t + 2 * Math.Sin(y / 4.0));
                int half = 3 + (int)(t * 7);
                int pc = S == Season.Winter ? Canvas.C(166, 186, 214) : (S == Season.Autumn ? Canvas.C(146, 112, 70) : Canvas.C(118, 98, 72));
                for (int x = cx - half; x <= cx + half; x++) if (R.Next(10) > 0) fg.Set(x, y, pc);
            }
            // details
            for (int i = 0; i < 260; i++)
            {
                int x = R.Next(W), y = 246 + R.Next(H - 246);
                if (S == Season.Spring)
                {
                    int[] fl = { Canvas.C(250, 200, 220), Canvas.C(255, 255, 240), Canvas.C(250, 220, 110), Canvas.C(190, 160, 240) };
                    fg.Set(x, y, fl[R.Next(fl.Length)]);
                }
                else if (S == Season.Autumn)
                {
                    int[] lf = { Canvas.C(214, 110, 40), Canvas.C(190, 64, 40), Canvas.C(230, 170, 60), Canvas.C(150, 84, 40) };
                    fg.Set(x, y, lf[R.Next(lf.Length)]); if (R.Next(2) == 0) fg.Set(x + 1, y, lf[R.Next(lf.Length)]);
                }
                else if (S == Season.Winter) { fg.Set(x, y, Canvas.C(236, 244, 255)); }
                else { fg.Set(x, y, Canvas.C(26, 60, 38)); fg.Set(x, y - 1, Canvas.C(30, 66, 42)); }
            }
        }

        // ---------------- deciduous trees
        void Trees()
        {
            int[] xs = { cabinX - 24, cabinX + 64, 300 };
            int[] hs = { 30, 24, 20 };
            for (int i = 0; i < xs.Length; i++)
            {
                int tx = xs[i], th = hs[i], tb = groundY + 1;
                int trunk = Canvas.C(70, 48, 34);
                fg.Rect(tx, tb - th, 2, th, trunk);
                fg.Set(tx - 1, tb - th / 2, trunk); fg.Set(tx - 2, tb - th / 2 - 1, trunk);
                fg.Set(tx + 2, tb - th / 2 - 3, trunk); fg.Set(tx + 3, tb - th / 2 - 4, trunk);
                if (S == Season.Winter)
                {
                    for (int b = 0; b < 6; b++)
                    {
                        int by = tb - th + 2 + b * 3, dir = b % 2 == 0 ? -1 : 1;
                        for (int k = 1; k < 6 - b / 2; k++) { fg.Set(tx + dir * k + (dir > 0 ? 1 : 0), by - k / 2, trunk); if (k % 2 == 0) fg.Set(tx + dir * k + (dir > 0 ? 1 : 0), by - k / 2 - 1, Canvas.C(225, 236, 250)); }
                    }
                    fg.Rect(tx, tb - th - 1, 2, 1, Canvas.C(230, 240, 252));
                    continue;
                }
                int[] pal;
                if (S == Season.Spring) pal = new int[] { Canvas.C(214, 120, 160), Canvas.C(236, 160, 190), Canvas.C(250, 206, 222), Canvas.C(255, 236, 242) };
                else if (S == Season.Autumn) pal = new int[] { Canvas.C(170, 60, 40), Canvas.C(206, 92, 44), Canvas.C(230, 140, 52), Canvas.C(244, 196, 84) };
                else pal = new int[] { Canvas.C(26, 62, 40), Canvas.C(36, 82, 50), Canvas.C(52, 104, 60), Canvas.C(76, 128, 72) };
                double cx = tx + 1, cy = tb - th - 2; double rad = 7 + th / 10.0;
                double[][] blobs = { new double[] { 0, 0, rad }, new double[] { -rad * 0.7, 3, rad * 0.7 }, new double[] { rad * 0.7, 2, rad * 0.75 }, new double[] { 0, -rad * 0.6, rad * 0.65 } };
                for (int y = (int)(cy - rad * 2); y <= (int)(cy + rad * 1.5); y++)
                    for (int x = (int)(cx - rad * 2); x <= (int)(cx + rad * 2); x++)
                    {
                        bool inside = false;
                        foreach (double[] bl in blobs) if ((x - cx - bl[0]) * (x - cx - bl[0]) + (y - cy - bl[1]) * (y - cy - bl[1]) <= bl[2] * bl[2]) inside = true;
                        if (!inside) continue;
                        double light = ((cx - x) * 0.6 + (cy - y)) / (rad * 2) + (R.NextDouble() - 0.5) * 0.5;
                        int idx = light > 0.45 ? 3 : (light > 0.1 ? 2 : (light > -0.3 ? 1 : 0));
                        if (!Night && S != Season.Spring) idx = Math.Min(3, idx + 0);
                        fg.Set(x, y, pal[idx]);
                    }
            }
        }

        // ---------------- cabin
        void Cabin()
        {
            int x0 = cabinX, gy = groundY;
            int wallTop = gy - 19, eave = wallTop, peak = wallTop - 19;
            int log1 = Canvas.C(118, 78, 46), log2 = Canvas.C(100, 64, 38), seam = Canvas.C(64, 40, 24);
            for (int y = wallTop; y <= gy; y++)
                for (int x = x0; x < x0 + 44; x++)
                {
                    int row = (y - wallTop) / 3;
                    int c = row % 2 == 0 ? log1 : log2;
                    if ((y - wallTop) % 3 == 2) c = seam;
                    if (R.Next(14) == 0) c = Canvas.Shade(c, 0.88);
                    fg.Set(x, y, c);
                }
            for (int y = wallTop; y <= gy; y += 3) { fg.Set(x0 - 1, y, Canvas.C(150, 104, 64)); fg.Set(x0 + 44, y, Canvas.C(150, 104, 64)); fg.Set(x0 - 1, y + 1, Canvas.C(120, 82, 50)); fg.Set(x0 + 44, y + 1, Canvas.C(120, 82, 50)); }
            // roof (gable end facing us)
            int roof = Canvas.C(72, 44, 38), roofD = Canvas.C(54, 32, 30), barge = Canvas.C(104, 70, 48);
            double cxr = x0 + 21.5;
            for (int y = peak; y <= eave; y++)
            {
                double half = (y - peak) * 26.0 / (eave - peak) + 1;
                for (int x = (int)(cxr - half); x <= (int)(cxr + half); x++)
                {
                    int c = ((y - peak) % 3 == 0) ? roofD : roof;
                    if ((y - peak) % 3 != 0 && ((x + ((y - peak) / 3) * 2) % 5 == 0)) c = roofD;
                    fg.Set(x, y, c);
                }
                fg.Set((int)(cxr - half), y, barge); fg.Set((int)(cxr + half), y, barge);
            }
            // gable window
            fg.Rect(x0 + 20, peak + 8, 4, 4, Canvas.C(60, 38, 22)); fg.Rect(x0 + 21, peak + 9, 2, 2, Canvas.C(255, 206, 110));
            // chimney
            chimX = x0 + 33; chimTop = peak + 2;
            for (int y = chimTop; y < peak + 14; y++) for (int x = chimX; x < chimX + 5; x++) fg.Set(x, y, ((x + y) % 3 == 0) ? Canvas.C(78, 78, 86) : Canvas.C(104, 104, 112));
            fg.Rect(chimX - 1, chimTop - 1, 7, 2, Canvas.C(70, 70, 80));
            // door
            fg.Rect(x0 + 7, gy - 12, 8, 13, Canvas.C(54, 34, 20));
            fg.Rect(x0 + 8, gy - 11, 6, 12, Canvas.C(84, 54, 32));
            for (int y = gy - 11; y <= gy; y += 3) fg.Rect(x0 + 8, y, 6, 1, Canvas.C(70, 44, 26));
            fg.Set(x0 + 13, gy - 5, Canvas.C(232, 196, 96));
            // window
            winX0 = x0 + 24; winY0 = gy - 14;
            fg.Rect(winX0 - 1, winY0 - 1, 12, 10, Canvas.C(60, 38, 22));
            // window glow on the surroundings
            fg.Glow(winX0 + 5, winY0 + 4, 16, Canvas.C(255, 190, 90), 0.22);
            for (int y = gy + 1; y < gy + 9; y++)
                for (int x = winX0 - 8; x < winX0 + 20; x++)
                {
                    double dx = (x - (winX0 + 5)) / 14.0, dy = (y - gy - 1) / 8.0;
                    double d = dx * dx + dy * dy; if (d < 1) fg.Blend(x, y, Canvas.C(255, 196, 110), 0.28 * (1 - d));
                }
            // lantern post
            fg.Rect(x0 - 7, gy - 10, 1, 11, Canvas.C(40, 28, 22));
            fg.Rect(x0 - 8, gy - 13, 3, 3, Canvas.C(255, 214, 120));
            fg.Glow(x0 - 6.5, gy - 11.5, 9, Canvas.C(255, 200, 100), 0.35);
            // winter: snow on roof, icicles, chimney cap
            if (S == Season.Winter)
            {
                for (int x = (int)(cxr - 27); x <= (int)(cxr + 27); x++)
                {
                    int top = -1;
                    for (int y = peak; y <= eave; y++) if (fg.Get(x, y) != 0 && Canvas.A(fg.Get(x, y)) == 255 && (y >= peak)) { double half = (y - peak) * 26.0 / (eave - peak) + 1; if (x >= (int)(cxr - half) && x <= (int)(cxr + half)) { top = y; break; } }
                    if (top < 0) continue;
                    int th = 2 + R.Next(2);
                    for (int y = top - 1; y < top + th; y++) fg.Set(x, y, y == top + th - 1 ? Canvas.C(196, 214, 236) : Canvas.C(236, 244, 255));
                }
                for (int x = (int)(cxr - 26); x <= (int)(cxr + 26); x += 2) if (R.Next(3) == 0) { int l = 1 + R.Next(3); for (int k = 0; k < l; k++) fg.Set(x, eave + 1 + k, Canvas.C(200, 228, 255)); }
                fg.Rect(chimX - 1, chimTop - 2, 7, 1, Canvas.C(236, 244, 255));
                fg.Rect(x0 - 8, gy - 14, 3, 1, Canvas.C(236, 244, 255));
            }
        }

        // ---------------- particles (precomputed, rendered per frame)
        void Particles()
        {
            if (S == Season.Winter)
            {
                for (int i = 0; i < 170; i++) { parts.Add(new double[] { R.Next(W), R.Next(H), 1, 1 + R.NextDouble() * 2, 1 + R.Next(2), R.NextDouble() * TAU, 0.45 + R.NextDouble() * 0.4, 1 }); partCols.Add(Canvas.C(255, 255, 255)); }
                for (int i = 0; i < 45; i++) { parts.Add(new double[] { R.Next(W), R.Next(H), 2, 2 + R.NextDouble() * 3, 1, R.NextDouble() * TAU, 0.8 + R.NextDouble() * 0.2, 2 }); partCols.Add(Canvas.C(255, 255, 255)); }
            }
            else if (S == Season.Spring)
            {
                int[] pc = { Canvas.C(250, 196, 216), Canvas.C(255, 226, 236), Canvas.C(236, 150, 186) };
                for (int i = 0; i < 70; i++) { parts.Add(new double[] { R.Next(W), R.Next(H), 1, 5 + R.NextDouble() * 8, 1 + R.Next(2), R.NextDouble() * TAU, 0.85, 3 }); partCols.Add(pc[R.Next(pc.Length)]); }
            }
            else if (S == Season.Autumn)
            {
                int[] lc = { Canvas.C(222, 114, 40), Canvas.C(196, 66, 40), Canvas.C(238, 178, 62), Canvas.C(156, 86, 42) };
                for (int i = 0; i < 55; i++) { parts.Add(new double[] { R.Next(W), R.Next(H), 1, 6 + R.NextDouble() * 10, 1, R.NextDouble() * TAU, 1, 4 }); partCols.Add(lc[R.Next(lc.Length)]); }
            }
            else if (S == Season.Summer)
            {
                for (int i = 0; i < 34; i++)
                {
                    parts.Add(new double[] { R.Next(150, W), 150 + R.Next(95), 4 + R.Next(10), 3 + R.Next(7), 1 + R.Next(2), R.NextDouble() * TAU, 1 + R.Next(3), 5, R.NextDouble() * TAU, 1 + R.Next(2) });
                    partCols.Add(Canvas.C(255, 236, 140));
                }
            }
        }

        public Canvas Frame(int t)
        {
            double ft = t / (double)N * Rate;
            Canvas c = sky.Copy();
            foreach (double[] s in stars)
            {
                double b = s[2] + s[3] * Math.Sin(TAU * s[4] * ft + s[5]);
                c.Blend((int)s[0], (int)s[1], Canvas.C(255, 255, 255), b);
            }
            if (S == Season.Winter)
            {
                int[] ac = { Canvas.C(90, 255, 170), Canvas.C(70, 220, 255), Canvas.C(170, 110, 255) };
                double[] aa = { 0.07, 0.055, 0.045 };
                for (int band = 0; band < 3; band++)
                {
                    double bas = 55 + band * 18, ph = band * 1.7;
                    for (int x = 0; x < W; x++)
                    {
                        double y = bas + 14 * Math.Sin(x / 55.0 + ph + TAU * ft) + 6 * Math.Sin(x / 17.0 + ph * 2 - TAU * 2 * ft);
                        double len = 18 + 10 * Math.Sin(x / 30.0 + band + TAU * ft);
                        double shimmer = 0.75 + 0.25 * Math.Sin(x / 9.0 - TAU * 3 * ft + band);
                        for (int k = 0; k < 4; k++)
                            for (int yy = (int)(y - k * 3); yy < (int)(y - k * 3 + len - k * 4); yy++) c.Blend(x, yy, ac[band], aa[band] * shimmer);
                    }
                }
            }
            c.Over(fg);
            // window flicker
            double fl = 1 + 0.07 * Math.Sin(TAU * 3 * ft) + 0.04 * Math.Sin(TAU * 7 * ft + 1);
            for (int y = 0; y < 8; y++) for (int x = 0; x < 10; x++)
            {
                bool frame = x == 4 || y == 3;
                int col = frame ? Canvas.C(70, 44, 26) : Canvas.C((int)(255 * fl), (int)(((y < 3 && x < 4) ? 222 : 196) * fl), (int)(((y < 3 && x < 4) ? 140 : 96) * fl));
                c.Set(winX0 + x, winY0 + y, col);
            }
            // chimney smoke
            for (int i = 0; i < 6; i++)
            {
                double age = (ft + i / 6.0) % 1.0;
                double px = chimX + 2 + age * 24 + 2 * Math.Sin(TAU * age * 1.5 + i);
                double py = chimTop - 2 - age * 48;
                double rad = 1.2 + age * 3.8;
                double a = 0.32 * (1 - age) * Math.Min(1, age / 0.12);
                int sc = S == Season.Autumn ? Canvas.C(214, 180, 176) : Canvas.C(170, 176, 192);
                c.Disc(px, py, rad, sc, a);
            }
            // particles
            for (int i = 0; i < parts.Count; i++)
            {
                double[] p = parts[i]; int col = partCols[i];
                int kind = (int)p[7];
                if (kind == 5)
                {
                    double x = p[0] + p[2] * Math.Sin(TAU * p[4] * ft + p[5]);
                    double y = p[1] + p[3] * Math.Sin(TAU * p[9] * ft + p[8]);
                    double v = Math.Sin(TAU * p[6] * ft + p[5] * 2);
                    double b = v > 0 ? Math.Pow(v, 1.5) : 0;
                    if (b <= 0.02) continue;
                    c.Glow(x + 0.5, y + 0.5, 4, Canvas.C(255, 214, 90), 0.45 * b);
                    c.Blend((int)x, (int)y, col, b);
                    continue;
                }
                double yy = (p[1] + p[2] * H * ft) % H;
                double xx = p[0] + p[3] * Math.Sin(TAU * p[4] * ft + p[5]);
                xx = ((xx % W) + W) % W;
                int ix = (int)xx, iy = (int)yy;
                if (kind == 1) c.Blend(ix, iy, col, p[6]);
                else if (kind == 2) { c.Blend(ix, iy, col, p[6]); c.Blend(ix + 1, iy, col, p[6]); c.Blend(ix, iy + 1, col, p[6]); c.Blend(ix + 1, iy + 1, col, p[6] * 0.8); }
                else
                {
                    int phase = (int)(((ft * 6 + p[5]) % 3 + 3) % 3);
                    c.Blend(ix, iy, col, p[6]);
                    if (phase == 0) c.Blend(ix + 1, iy, col, p[6]);
                    else if (phase == 1) c.Blend(ix, iy + 1, col, p[6]);
                    else if (kind == 4) { c.Blend(ix + 1, iy, col, p[6]); c.Blend(ix, iy + 1, Canvas.Shade(col, 0.8), p[6]); }
                }
            }
            return c;
        }

        public List<Canvas> Frames() { List<Canvas> l = new List<Canvas>(); for (int t = 0; t < N; t++) l.Add(Frame(t)); return l; }

        // Full seasonal cycle: each season holds, then cross-fades smoothly into the next one; loops seamlessly.
        public static List<Canvas> Cycle(int framesPerSeason, int fadeFrames, int rate)
        {
            Season[] order = { Season.Winter, Season.Spring, Season.Summer, Season.Autumn };
            int n = framesPerSeason * order.Length;
            Scene[] sc = new Scene[order.Length];
            for (int i = 0; i < order.Length; i++) sc[i] = new Scene(order[i], 7, n, rate);
            List<Canvas> outF = new List<Canvas>();
            for (int t = 0; t < n; t++)
            {
                int seg = t / framesPerSeason, pos = t % framesPerSeason;
                Canvas a = sc[seg].Frame(t);
                int hold = framesPerSeason - fadeFrames;
                if (pos >= hold)
                {
                    double x = (pos - hold + 1) / (double)(fadeFrames + 1);
                    double k = x * x * (3 - 2 * x);  // smoothstep
                    Canvas b = sc[(seg + 1) % order.Length].Frame(t);
                    for (int p = 0; p < a.P.Length; p++) a.P[p] = Canvas.Mix(a.P[p], b.P[p], k);
                }
                outF.Add(a);
            }
            return outF;
        }
    }

    // ------------------------------------------------------------------ logo / UI
    public static class Ui
    {
        static Dictionary<char, string[]> G = new Dictionary<char, string[]>
        {
            { 'H', new[] { "##...##", "##...##", "##...##", "#######", "#######", "##...##", "##...##", "##...##", "##...##" } },
            { 'O', new[] { ".#####.", "##...##", "##...##", "##...##", "##...##", "##...##", "##...##", "##...##", ".#####." } },
            { 'M', new[] { "##...##", "###.###", "#######", "##.#.##", "##...##", "##...##", "##...##", "##...##", "##...##" } },
            { 'E', new[] { "#######", "##.....", "##.....", "######.", "######.", "##.....", "##.....", "##.....", "#######" } },
            { 'B', new[] { "######.", "##...##", "##...##", "######.", "######.", "##...##", "##...##", "##...##", "######." } },
            { 'F', new[] { "#######", "##.....", "##.....", "######.", "######.", "##.....", "##.....", "##.....", "##....." } },
            { 'R', new[] { "######.", "##...##", "##...##", "######.", "#####..", "##.##..", "##..##.", "##..##.", "##...##" } },
            { 'D', new[] { "#####..", "##..##.", "##...##", "##...##", "##...##", "##...##", "##...##", "##..##.", "#####.." } },
            { 'A', new[] { ".#####.", "##...##", "##...##", "##...##", "#######", "#######", "##...##", "##...##", "##...##" } },
            { 'K', new[] { "##...##", "##..##.", "##.##..", "####...", "###....", "####...", "##.##..", "##..##.", "##...##" } }
        };

        static int TextWidth(string s, int scale) { int w = 0; foreach (char ch in s) w += (ch == ' ' ? 4 : 8) * scale; return w - scale; }
        static void Stamp(bool[,] m, string s, int x0, int y0, int scale)
        {
            int x = x0;
            foreach (char ch in s)
            {
                if (ch == ' ') { x += 4 * scale; continue; }
                string[] g = G[ch];
                for (int gy = 0; gy < 9; gy++) for (int gx = 0; gx < 7; gx++)
                    if (g[gy][gx] == '#') for (int sy = 0; sy < scale; sy++) for (int sx = 0; sx < scale; sx++) m[x + gx * scale + sx, y0 + gy * scale + sy] = true;
                x += 8 * scale;
            }
        }

        public static Canvas Logo(bool snow, int seed)
        {
            Random r = new Random(seed);
            string l1 = "HOME", l2 = "BEFORE DARK";
            int w1 = TextWidth(l1, 3), w2 = TextWidth(l2, 1);
            int W = Math.Max(w1, w2) + 10, H = 27 + 4 + 9 + 12;
            bool[,] m = new bool[W, H];
            int y1 = 5, y2 = y1 + 27 + 4;
            Stamp(m, l1, (W - w1) / 2, y1, 3);
            Stamp(m, l2, (W - w2) / 2, y2, 1);
            Canvas c = new Canvas(W, H);
            int outline = Canvas.C(34, 20, 14), ext = Canvas.C(112, 58, 30);
            // extrusion (3D depth) then outline
            bool[,] body = new bool[W, H];
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) if (m[x, y]) { body[x, y] = true; if (x + 1 < W && y + 1 < H) body[x + 1, y + 1] = true; if (x + 2 < W && y + 2 < H && y < y2) body[x + 2, y + 2] = true; }
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
            {
                if (body[x, y]) { if (!m[x, y]) c.Set(x, y, ext); continue; }
                bool near = false;
                for (int dy = -1; dy <= 1 && !near; dy++) for (int dx = -1; dx <= 1; dx++) { int nx = x + dx, ny = y + dy; if (nx >= 0 && ny >= 0 && nx < W && ny < H && body[nx, ny]) { near = true; break; } }
                if (near) c.Set(x, y, outline);
            }
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
            {
                if (!m[x, y]) continue;
                bool big = y < y2;
                double t = big ? (y - y1) / 27.0 : (y - y2) / 9.0;
                int top = big ? Canvas.C(255, 238, 196) : Canvas.C(250, 228, 182);
                int bot = big ? Canvas.C(232, 140, 60) : Canvas.C(224, 146, 72);
                int col = Canvas.Mix(top, bot, t);
                if (y > 0 && !m[x, y - 1]) col = Canvas.C(255, 250, 228);
                else if (x > 0 && !m[x - 1, y]) col = Canvas.Shade(col, 1.06);
                else if (x + 1 < W && !m[x + 1, y]) col = Canvas.Shade(col, 0.86);
                if (big && ((x * 5 + y * 3) % 11 == 0 || r.Next(9) == 0)) col = Canvas.Shade(col, 0.92);  // wood grain
                c.Set(x, y, col);
            }
            if (snow)
            {
                for (int x = 0; x < W; x++) for (int y = 1; y < H; y++)
                {
                    if (!m[x, y] || m[x, y - 1]) continue;
                    if (y >= y2 - 2) continue;
                    int th = y < y2 ? 1 + r.Next(2) : 1;
                    for (int k = 1; k <= th; k++) c.Set(x, y - k, Canvas.C(242, 248, 255));
                    c.Set(x, y, Canvas.C(206, 222, 242));
                    if (r.Next(12) == 0 && y + 2 < H) { c.Set(x, y + 1, Canvas.C(220, 234, 252)); }
                }
            }
            return c;
        }

        // wooden button textures (200x20, nine-slice border 3)
        public static Canvas Button(int kind, int seed)
        {
            Random r = new Random(seed);
            int W = 200, H = 20;
            Canvas c = new Canvas(W, H);
            int baseC, grain, hi, lo, border;
            if (kind == 1) { baseC = Canvas.C(124, 88, 56); grain = Canvas.C(104, 72, 46); hi = Canvas.C(170, 128, 84); lo = Canvas.C(80, 54, 34); border = Canvas.C(248, 206, 120); }
            else if (kind == 2) { baseC = Canvas.C(70, 64, 60); grain = Canvas.C(60, 55, 52); hi = Canvas.C(92, 86, 80); lo = Canvas.C(48, 44, 42); border = Canvas.C(28, 24, 22); }
            else { baseC = Canvas.C(86, 60, 40); grain = Canvas.C(72, 50, 33); hi = Canvas.C(118, 86, 58); lo = Canvas.C(56, 38, 25); border = Canvas.C(26, 17, 11); }
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
            {
                int col = baseC;
                if (y == 6 || y == 13) col = grain;
                else if ((x * 7 + y * 13 + r.Next(3)) % 37 == 0) col = grain;
                c.Set(x, y, col);
            }
            for (int i = 0; i < 6; i++) { int kx = 10 + r.Next(W - 20), ky = 2 + r.Next(3) * 6; c.Set(kx, ky + 1, lo); c.Set(kx + 1, ky + 1, lo); }
            for (int x = 1; x < W - 1; x++) { c.Set(x, 1, hi); c.Set(x, H - 2, lo); }
            for (int y = 1; y < H - 1; y++) { c.Set(1, y, hi); c.Set(W - 2, y, lo); }
            for (int x = 0; x < W; x++) { c.Set(x, 0, border); c.Set(x, H - 1, border); }
            for (int y = 0; y < H; y++) { c.Set(0, y, border); c.Set(W - 1, y, border); }
            if (kind == 1) { c.Set(0, 0, 0); c.Set(W - 1, 0, 0); c.Set(0, H - 1, 0); c.Set(W - 1, H - 1, 0); }
            return c;
        }

        public static Canvas SliderBack(int seed)
        {
            Canvas c = Button(0, seed);
            for (int i = 0; i < c.P.Length; i++) { int x = i % c.W, y = i / c.W; if (x > 0 && y > 0 && x < c.W - 1 && y < c.H - 1) c.P[i] = Canvas.Shade(c.P[i], 0.62); }
            return c;
        }

        public static Canvas SliderHandle(bool hover)
        {
            Canvas c = new Canvas(8, 20);
            int b = hover ? Canvas.C(248, 206, 120) : Canvas.C(26, 17, 11);
            int f = hover ? Canvas.C(150, 108, 70) : Canvas.C(118, 84, 54);
            c.Rect(0, 0, 8, 20, b); c.Rect(1, 1, 6, 18, f);
            c.Rect(1, 1, 6, 1, Canvas.Shade(f, 1.25)); c.Rect(1, 18, 6, 1, Canvas.Shade(f, 0.7));
            for (int y = 6; y < 14; y += 3) c.Rect(3, y, 2, 1, Canvas.Shade(f, 0.75));
            return c;
        }

        public static Canvas Panel()
        {
            Canvas c = new Canvas(120, 4);
            for (int x = 0; x < 120; x++) { double t = x / 119.0; int a = (int)(175 * Math.Pow(1 - t, 1.4)); for (int y = 0; y < 4; y++) c.Set(x, y, Canvas.C(a, 8, 10, 20)); }
            return c;
        }

        public static Canvas Icon()
        {
            Canvas c = new Canvas(16, 16);
            int sky = Canvas.C(20, 30, 64);
            c.Rect(1, 0, 14, 16, sky); c.Rect(0, 1, 16, 14, sky);
            for (int y = 2; y < 9; y++) { int half = (y - 2) + 1; c.Rect(8 - half, y, half * 2, 1, Canvas.C(150, 70, 52)); }
            c.Rect(3, 9, 10, 6, Canvas.C(130, 86, 50)); c.Rect(3, 11, 10, 1, Canvas.C(96, 62, 36)); c.Rect(3, 13, 10, 1, Canvas.C(96, 62, 36));
            c.Rect(9, 10, 3, 3, Canvas.C(255, 210, 100)); c.Rect(5, 11, 2, 4, Canvas.C(70, 44, 26));
            c.Rect(11, 3, 2, 3, Canvas.C(110, 110, 120)); c.Set(13, 1, Canvas.C(190, 196, 210)); c.Set(14, 0, Canvas.C(160, 166, 184));
            c.Rect(0, 15, 16, 1, Canvas.C(46, 90, 56));
            return c;
        }

        // ---------------- title menu (custom screen): own pixel fonts, lantern bullets, shades
        // bold font, 7 rows, 2px stems (same family as the logo)
        static Dictionary<char, string[]> FB = new Dictionary<char, string[]>
        {
            { 'A', new[] { ".####.", "##..##", "##..##", "######", "##..##", "##..##", "##..##" } },
            { 'B', new[] { "#####.", "##..##", "##..##", "#####.", "##..##", "##..##", "#####." } },
            { 'C', new[] { ".####.", "##..##", "##....", "##....", "##....", "##..##", ".####." } },
            { 'D', new[] { "#####.", "##..##", "##..##", "##..##", "##..##", "##..##", "#####." } },
            { 'E', new[] { "######", "##....", "##....", "#####.", "##....", "##....", "######" } },
            { 'F', new[] { "######", "##....", "##....", "#####.", "##....", "##....", "##...." } },
            { 'G', new[] { ".####.", "##..##", "##....", "##.###", "##..##", "##..##", ".#####" } },
            { 'H', new[] { "##..##", "##..##", "##..##", "######", "##..##", "##..##", "##..##" } },
            { 'I', new[] { "####", ".##.", ".##.", ".##.", ".##.", ".##.", "####" } },
            { 'J', new[] { "....##", "....##", "....##", "....##", "##..##", "##..##", ".####." } },
            { 'K', new[] { "##..##", "##.##.", "####..", "###...", "####..", "##.##.", "##..##" } },
            { 'L', new[] { "##....", "##....", "##....", "##....", "##....", "##....", "######" } },
            { 'M', new[] { "##...##", "###.###", "#######", "##.#.##", "##...##", "##...##", "##...##" } },
            { 'N', new[] { "##..##", "###.##", "######", "##.###", "##..##", "##..##", "##..##" } },
            { 'O', new[] { ".####.", "##..##", "##..##", "##..##", "##..##", "##..##", ".####." } },
            { 'P', new[] { "#####.", "##..##", "##..##", "#####.", "##....", "##....", "##...." } },
            { 'Q', new[] { ".####.", "##..##", "##..##", "##..##", "##.###", "##..##", ".###.#" } },
            { 'R', new[] { "#####.", "##..##", "##..##", "#####.", "####..", "##.##.", "##..##" } },
            { 'S', new[] { ".####.", "##..##", "##....", ".####.", "....##", "##..##", ".####." } },
            { 'T', new[] { "######", "..##..", "..##..", "..##..", "..##..", "..##..", "..##.." } },
            { 'U', new[] { "##..##", "##..##", "##..##", "##..##", "##..##", "##..##", ".####." } },
            { 'V', new[] { "##..##", "##..##", "##..##", "##..##", "##..##", ".####.", "..##.." } },
            { 'W', new[] { "##...##", "##...##", "##...##", "##.#.##", "#######", "###.###", "##...##" } },
            { 'X', new[] { "##..##", "##..##", ".####.", "..##..", ".####.", "##..##", "##..##" } },
            { 'Y', new[] { "##..##", "##..##", ".####.", "..##..", "..##..", "..##..", "..##.." } },
            { 'Z', new[] { "######", "....##", "...##.", "..##..", ".##...", "##....", "######" } }
        };
        // small font, 5 rows, 1px; \u0001 = globe icon, \u0002 = accessibility figure
        static Dictionary<char, string[]> FS = new Dictionary<char, string[]>
        {
            { 'A', new[] { ".#.", "#.#", "###", "#.#", "#.#" } }, { 'B', new[] { "##.", "#.#", "##.", "#.#", "##." } },
            { 'C', new[] { ".##", "#..", "#..", "#..", ".##" } }, { 'D', new[] { "##.", "#.#", "#.#", "#.#", "##." } },
            { 'E', new[] { "###", "#..", "##.", "#..", "###" } }, { 'F', new[] { "###", "#..", "##.", "#..", "#.." } },
            { 'G', new[] { ".##", "#..", "#.#", "#.#", ".##" } }, { 'H', new[] { "#.#", "#.#", "###", "#.#", "#.#" } },
            { 'I', new[] { "###", ".#.", ".#.", ".#.", "###" } }, { 'J', new[] { "..#", "..#", "..#", "#.#", ".#." } },
            { 'K', new[] { "#.#", "#.#", "##.", "#.#", "#.#" } }, { 'L', new[] { "#..", "#..", "#..", "#..", "###" } },
            { 'M', new[] { "#...#", "##.##", "#.#.#", "#...#", "#...#" } }, { 'N', new[] { "#...#", "##..#", "#.#.#", "#..##", "#...#" } },
            { 'O', new[] { ".#.", "#.#", "#.#", "#.#", ".#." } }, { 'P', new[] { "##.", "#.#", "##.", "#..", "#.." } },
            { 'Q', new[] { ".#.", "#.#", "#.#", "##.", ".##" } }, { 'R', new[] { "##.", "#.#", "##.", "#.#", "#.#" } },
            { 'S', new[] { ".##", "#..", ".#.", "..#", "##." } }, { 'T', new[] { "###", ".#.", ".#.", ".#.", ".#." } },
            { 'U', new[] { "#.#", "#.#", "#.#", "#.#", "###" } }, { 'V', new[] { "#.#", "#.#", "#.#", "#.#", ".#." } },
            { 'W', new[] { "#...#", "#...#", "#.#.#", "##.##", "#...#" } }, { 'X', new[] { "#.#", "#.#", ".#.", "#.#", "#.#" } },
            { 'Y', new[] { "#.#", "#.#", ".#.", ".#.", ".#." } }, { 'Z', new[] { "###", "..#", ".#.", "#..", "###" } },
            { '0', new[] { "###", "#.#", "#.#", "#.#", "###" } }, { '1', new[] { ".#.", "##.", ".#.", ".#.", "###" } },
            { '2', new[] { "##.", "..#", ".#.", "#..", "###" } }, { '3', new[] { "##.", "..#", ".#.", "..#", "##." } },
            { '4', new[] { "#.#", "#.#", "###", "..#", "..#" } }, { '5', new[] { "###", "#..", "##.", "..#", "##." } },
            { '6', new[] { ".##", "#..", "###", "#.#", "###" } }, { '7', new[] { "###", "..#", ".#.", ".#.", ".#." } },
            { '8', new[] { "###", "#.#", "###", "#.#", "###" } }, { '9', new[] { "###", "#.#", "###", "..#", "##." } },
            { '.', new[] { ".", ".", ".", ".", "#" } }, { '·', new[] { ".", ".", "#", ".", "." } },
            { '-', new[] { "...", "...", "###", "...", "..." } },
            { '\u0001', new[] { ".###.", "#.#.#", "#####", "#.#.#", ".###." } },
            { '\u0002', new[] { "..#..", "#####", "..#..", ".#.#.", "#...#" } }
        };

        public static int TextW(Dictionary<char, string[]> f, string s, int gap, int space)
        {
            int w = 0;
            foreach (char ch in s) w += (ch == ' ' ? space : f[ch][0].Length) + gap;
            return w - gap;
        }

        // text with a vertical gradient fill, 1px dark outline and a soft drop shadow
        static void Text(Canvas c, Dictionary<char, string[]> f, string s, int x0, int y0, int gap, int space, int top, int bot, double alpha)
        {
            int rows = f['A'].Length;
            bool[,] m = new bool[c.W, c.H];
            int x = x0;
            foreach (char ch in s)
            {
                if (ch == ' ') { x += space + gap; continue; }
                string[] g = f[ch];
                for (int gy = 0; gy < rows; gy++) for (int gx = 0; gx < g[gy].Length; gx++)
                    if (g[gy][gx] == '#' && c.In(x + gx, y0 + gy)) m[x + gx, y0 + gy] = true;
                x += g[0].Length + gap;
            }
            if (rows < 7)
            {
                // thin font: a soft diagonal shadow only (an outline would box in the 1px strokes)
                for (int yy = c.H - 1; yy >= 1; yy--) for (int xx = c.W - 1; xx >= 1; xx--)
                    if (m[xx - 1, yy - 1] && !m[xx, yy]) c.Blend(xx, yy, Canvas.C(14, 10, 12), 0.85 * alpha);
                for (int yy = 0; yy < c.H; yy++) for (int xx = 0; xx < c.W; xx++)
                    if (m[xx, yy]) c.Blend(xx, yy, Canvas.Mix(top, bot, (yy - y0) / (double)(rows - 1)), alpha);
                return;
            }
            bool[,] o = new bool[c.W, c.H];
            for (int yy = 0; yy < c.H; yy++) for (int xx = 0; xx < c.W; xx++)
                if (m[xx, yy]) for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) if (c.In(xx + dx, yy + dy)) o[xx + dx, yy + dy] = true;
            int outline = Canvas.C(26, 18, 12);
            for (int yy = c.H - 1; yy >= 1; yy--) for (int xx = 0; xx < c.W; xx++)
                if (o[xx, yy - 1] && !o[xx, yy]) c.Blend(xx, yy, Canvas.C(8, 6, 10), 0.45 * alpha);
            for (int yy = 0; yy < c.H; yy++) for (int xx = 0; xx < c.W; xx++)
            {
                if (m[xx, yy]) c.Blend(xx, yy, Canvas.Mix(top, bot, rows > 1 ? (yy - y0) / (double)(rows - 1) : 0), alpha);
                else if (o[xx, yy]) c.Blend(xx, yy, outline, 0.92 * alpha);
            }
        }

        static void Lantern(Canvas c, int x, int y, bool lit)
        {
            int iron = lit ? Canvas.C(62, 42, 28) : Canvas.C(40, 34, 34);
            int ironHi = lit ? Canvas.C(118, 84, 52) : Canvas.C(76, 66, 62);
            if (lit) { c.Glow(x + 3.5, y + 5, 8.5, Canvas.C(255, 176, 76), 0.75); c.Glow(x + 3.5, y + 5, 4.5, Canvas.C(255, 214, 140), 0.6); }
            c.Rect(x + 2, y, 3, 1, iron); c.Set(x + 1, y + 1, iron); c.Set(x + 5, y + 1, iron);
            c.Rect(x, y + 2, 7, 1, ironHi);
            for (int yy = y + 3; yy <= y + 6; yy++) { c.Set(x, yy, iron); c.Set(x + 6, yy, iron); }
            if (lit)
            {
                c.Rect(x + 1, y + 3, 5, 4, Canvas.C(246, 178, 82));
                c.Rect(x + 2, y + 4, 3, 3, Canvas.C(255, 222, 140));
                c.Set(x + 3, y + 4, Canvas.C(255, 250, 222)); c.Set(x + 3, y + 5, Canvas.C(255, 244, 196));
            }
            else
            {
                c.Rect(x + 1, y + 3, 5, 4, Canvas.C(46, 44, 56));
                c.Set(x + 1, y + 3, Canvas.C(88, 86, 100)); c.Set(x + 2, y + 3, Canvas.C(70, 68, 82));
            }
            c.Rect(x, y + 7, 7, 1, iron); c.Rect(x + 1, y + 8, 5, 1, ironHi); c.Rect(x + 2, y + 9, 3, 1, iron);
        }

        // main menu entry: lantern bullet (lights up on hover) + bold text. Text starts at x=21.
        public static Canvas MenuItem(string text, bool hover)
        {
            int tw = TextW(FB, text, 1, 4);
            Canvas c = new Canvas(21 + tw + 3, 19);
            Lantern(c, 9, 4, hover);
            if (hover) Text(c, FB, text, 21, 6, 1, 4, Canvas.C(255, 246, 214), Canvas.C(244, 164, 80), 1.0);
            else Text(c, FB, text, 21, 6, 1, 4, Canvas.C(243, 230, 202), Canvas.C(200, 178, 144), 1.0);
            return c;
        }

        // small single line (links, tagline). Text starts at x=2.
        public static Canvas SmallText(string text, int col, double alpha)
        {
            Canvas c = new Canvas(TextW(FS, text, 1, 3) + 4, 9);
            Text(c, FS, text, 2, 2, 1, 3, col, col, alpha);
            return c;
        }

        // several small lines, right-aligned (credits / version, bottom-right corner)
        public static Canvas SmallLinesRight(string[] lines, int[] cols, double[] alphas)
        {
            int w = 0; foreach (string l in lines) w = Math.Max(w, TextW(FS, l, 1, 3));
            Canvas c = new Canvas(w + 4, lines.Length * 7 + 3);
            for (int i = 0; i < lines.Length; i++) Text(c, FS, lines[i], 2 + w - TextW(FS, lines[i], 1, 3), 2 + i * 7, 1, 3, cols[i], cols[i], alphas[i]);
            return c;
        }

        // soft dusk shade behind the menu column (stretched vertically) and along the bottom (stretched horizontally)
        public static Canvas ShadeLeft()
        {
            Canvas c = new Canvas(230, 2);
            for (int x = 0; x < c.W; x++) { double t = x / (double)(c.W - 1); int a = (int)(150 * Math.Pow(1 - t, 1.8)); for (int y = 0; y < c.H; y++) c.Set(x, y, Canvas.C(a, 10, 9, 20)); }
            return c;
        }
        public static Canvas ShadeBottom()
        {
            Canvas c = new Canvas(2, 36);
            for (int y = 0; y < c.H; y++) { double t = y / (double)(c.H - 1); int a = (int)(150 * Math.Pow(t, 1.4)); for (int x = 0; x < c.W; x++) c.Set(x, y, Canvas.C(a, 10, 9, 20)); }
            return c;
        }
    }

    // ------------------------------------------------------------------ audio
    public static class Snd
    {
        public const double TAU = Math.PI * 2;
        public static double Hz(int midi) { return 440.0 * Math.Pow(2, (midi - 69) / 12.0); }

        public static void Wav(string path, double[] s, int sr, double peakTarget)
        {
            double peak = 1e-9; foreach (double v in s) peak = Math.Max(peak, Math.Abs(v));
            double g = peakTarget / peak;
            using (FileStream fs = new FileStream(path, FileMode.Create))
            using (BinaryWriter w = new BinaryWriter(fs))
            {
                int dataLen = s.Length * 2;
                w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + dataLen); w.Write(Encoding.ASCII.GetBytes("WAVE"));
                w.Write(Encoding.ASCII.GetBytes("fmt ")); w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(sr); w.Write(sr * 2); w.Write((short)2); w.Write((short)16);
                w.Write(Encoding.ASCII.GetBytes("data")); w.Write(dataLen);
                for (int i = 0; i < s.Length; i++) { double v = Math.Max(-1, Math.Min(1, s[i] * g)); w.Write((short)Math.Round(v * 32767)); }
            }
        }

        // Freeverb-style mono reverb
        public static double[] Reverb(double[] x, int sr, double room, double damp, double wet)
        {
            int[] cd = { 1116, 1188, 1277, 1356, 1422, 1491, 1557, 1617 };
            int[] ad = { 556, 441, 341, 225 };
            double sc = sr / 44100.0;
            double[] y = new double[x.Length];
            double[][] cb = new double[8][]; int[] ci = new int[8]; double[] cf = new double[8];
            for (int k = 0; k < 8; k++) cb[k] = new double[Math.Max(1, (int)(cd[k] * sc))];
            double[][] ab = new double[4][]; int[] ai = new int[4];
            for (int k = 0; k < 4; k++) ab[k] = new double[Math.Max(1, (int)(ad[k] * sc))];
            for (int i = 0; i < x.Length; i++)
            {
                double inp = x[i] * 0.015, o = 0;
                for (int k = 0; k < 8; k++)
                {
                    double b = cb[k][ci[k]];
                    cf[k] = b * (1 - damp) + cf[k] * damp;
                    cb[k][ci[k]] = inp + cf[k] * room;
                    ci[k] = (ci[k] + 1) % cb[k].Length; o += b;
                }
                for (int k = 0; k < 4; k++)
                {
                    double b = ab[k][ai[k]]; double v = -o + b;
                    ab[k][ai[k]] = o + b * 0.5; ai[k] = (ai[k] + 1) % ab[k].Length; o = v;
                }
                y[i] = x[i] + o * wet;
            }
            return y;
        }

        static void Add(double[] buf, int i, double v, bool wrap) { if (wrap) buf[((i % buf.Length) + buf.Length) % buf.Length] += v; else if (i >= 0 && i < buf.Length) buf[i] += v; }

        public static void MusicBox(double[] buf, int sr, double t0, int midi, double vel)
        {
            double f = Hz(midi); int s0 = (int)(t0 * sr); int len = (int)(3.2 * sr);
            for (int i = 0; i < len; i++)
            {
                double t = i / (double)sr;
                double env = (1 - Math.Exp(-t * 300)) * Math.Exp(-t * 1.9);
                double v = Math.Sin(TAU * f * t) + 0.32 * Math.Sin(TAU * 2 * f * t) * Math.Exp(-t * 5) + 0.12 * Math.Sin(TAU * 3.01 * f * t) * Math.Exp(-t * 9) + 0.06 * Math.Sin(TAU * 4.2 * f * t) * Math.Exp(-t * 16);
                Add(buf, s0 + i, v * env * vel, false);
            }
        }

        public static void Pluck(double[] buf, int sr, double t0, int midi, double vel, Random r)
        {
            double f = Hz(midi); int n = Math.Max(2, (int)Math.Round(sr / f));
            double[] line = new double[n];
            for (int i = 0; i < n; i++) line[i] = r.NextDouble() * 2 - 1;
            for (int k = 0; k < 3; k++) for (int i = 0; i < n; i++) line[i] = 0.5 * (line[i] + line[(i + 1) % n]);
            int s0 = (int)(t0 * sr), len = (int)(sr * 2.6);
            double decay = 0.9965;
            for (int i = 0; i < len; i++)
            {
                int j = i % n, jn = (i + 1) % n;
                double v = line[j];
                line[j] = decay * 0.5 * (line[j] + line[jn]);
                double env = Math.Min(1, i / (sr * 0.004));
                Add(buf, s0 + i, v * vel * env, false);
            }
        }

        public static void Pad(double[] buf, int sr, double t0, double dur, int[] notes, double amp)
        {
            int s0 = (int)(t0 * sr), len = (int)((dur + 1.6) * sr);
            foreach (int m in notes)
            {
                double f = Hz(m);
                for (int i = 0; i < len; i++)
                {
                    double t = i / (double)sr;
                    double env = Math.Min(1, t / 1.1) * (t > dur ? Math.Exp(-(t - dur) * 2.6) : 1);
                    double v = Math.Sin(TAU * f * t) + Math.Sin(TAU * f * 1.004 * t + 1) * 0.7 + Math.Sin(TAU * f * 0.996 * t + 2) * 0.7 + 0.12 * Math.Sin(TAU * 2 * f * t);
                    Add(buf, s0 + i, v * env * amp, false);
                }
            }
        }

        public static void Bass(double[] buf, int sr, double t0, int midi, double amp)
        {
            double f = Hz(midi); int s0 = (int)(t0 * sr), len = (int)(2.4 * sr);
            for (int i = 0; i < len; i++)
            {
                double t = i / (double)sr;
                double env = Math.Min(1, t / 0.012) * Math.Exp(-t * 1.25);
                Add(buf, s0 + i, (Math.Sin(TAU * f * t) + 0.22 * Math.Sin(TAU * 2 * f * t)) * env * amp, false);
            }
        }

        public class Chord { public int Root; public int[] Pad; public int[] Tones; public Chord(int root, int[] pad, int[] tones) { Root = root; Pad = pad; Tones = tones; } }

        public static double[] Song(int sr, double bpm, Chord[] prog, int passes, int seed, bool guitar, int[] scalePcs, int lo, int hi)
        {
            Random r = new Random(seed);
            double beat = 60.0 / bpm, bar = beat * 4;
            int bars = prog.Length * passes + 2;
            double total = bars * bar + 5;
            double[] buf = new double[(int)(total * sr)];
            List<int> notes = new List<int>();
            for (int m = lo; m <= hi; m++) foreach (int pc in scalePcs) if (((m % 12) + 12) % 12 == pc) notes.Add(m);
            double[][] rhythms = { new double[] { 0, 1, 2, 3 }, new double[] { 0, 1.5, 2, 3 }, new double[] { 0, 0.5, 1, 2.5 }, new double[] { 0, 2, 2.5, 3.5 }, new double[] { 0, 1, 1.5, 3 }, new double[] { 0, 3 } };
            int prev = notes[notes.Count / 2];
            int[] motif = { r.Next(5), r.Next(5) };
            for (int p = 0; p < passes; p++)
            {
                for (int b = 0; b < prog.Length; b++)
                {
                    Chord ch = prog[b]; double t0 = (p * prog.Length + b) * bar;
                    Pad(buf, sr, t0, bar, ch.Pad, p == 0 ? 0.018 : 0.022);
                    Bass(buf, sr, t0, ch.Root, 0.20);
                    Bass(buf, sr, t0 + 2 * beat, ch.Root + (b % 2 == 0 ? 7 : 12), 0.09);
                    if (guitar)
                    {
                        int[] arp = { ch.Pad[0] + 12, ch.Pad[1] + 12, ch.Pad[2] + 12, ch.Pad[Math.Min(3, ch.Pad.Length - 1)] + 12 };
                        int[] order = { 0, 1, 2, 3, 2, 1, 3, 1 };
                        for (int k = 0; k < 8; k++) Pluck(buf, sr, t0 + k * beat / 2, arp[order[k]], (k == 0 ? 0.26 : 0.17) * (p == 0 ? 0.8 : 1), r);
                    }
                    if (p == 0 && !guitar)
                    {
                        int tone = Nearest(notes, ch.Tones, prev + 5); MusicBox(buf, sr, t0, tone, 0.28); prev = tone;
                        continue;
                    }
                    if (p == 0) continue;
                    double[] rh = (b % 4 < 2) ? rhythms[motif[b % 2]] : rhythms[r.Next(rhythms.Length)];
                    if (b == prog.Length - 1) rh = new double[] { 0, 2 };
                    for (int j = 0; j < rh.Length; j++)
                    {
                        int note;
                        if (j == 0) note = Nearest(notes, ch.Tones, prev);
                        else
                        {
                            int idx = notes.IndexOf(prev); if (idx < 0) idx = notes.Count / 2;
                            int[] steps = { -2, -1, -1, 1, 1, 2, 0 };
                            idx = Math.Max(0, Math.Min(notes.Count - 1, idx + steps[r.Next(steps.Length)]));
                            note = notes[idx];
                        }
                        double vel = (j == 0 ? 0.42 : 0.30) + r.NextDouble() * 0.08;
                        MusicBox(buf, sr, t0 + rh[j] * beat + r.NextDouble() * 0.012, note, vel * (guitar ? 0.75 : 1));
                        prev = note;
                    }
                    if (p == passes - 1 && b % 2 == 1) MusicBox(buf, sr, t0 + 3.5 * beat, Nearest(notes, ch.Tones, prev + 12), 0.14);
                }
            }
            // final chord
            double tf = passes * prog.Length * bar;
            Pad(buf, sr, tf, bar * 1.5, prog[0].Pad, 0.02);
            Bass(buf, sr, tf, prog[0].Root, 0.2);
            MusicBox(buf, sr, tf + beat, Nearest(notes, prog[0].Tones, prev), 0.3);
            double[] o = Reverb(buf, sr, 0.86, 0.3, 0.42);
            int fade = (int)(4 * sr);
            for (int i = 0; i < o.Length; i++) { o[i] = Math.Tanh(o[i] * 1.2); if (i > o.Length - fade) o[i] *= (o.Length - i) / (double)fade; }
            return o;
        }

        static int Nearest(List<int> notes, int[] tones, int target)
        {
            int best = notes[0], bd = 999;
            foreach (int n in notes)
            {
                bool ok = false; foreach (int t in tones) if (((n % 12) + 12) % 12 == t) ok = true;
                if (!ok) continue;
                int d = Math.Abs(n - target); if (d < bd) { bd = d; best = n; }
            }
            return best;
        }

        // ---------- ambience (seamless loops)
        static double[] Loop(double[] longBuf, int n, int xf)
        {
            double[] o = new double[n];
            for (int i = 0; i < n; i++) o[i] = longBuf[i];
            for (int i = 0; i < xf; i++) { double t = i / (double)xf; o[i] = longBuf[n + i] * Math.Cos(t * Math.PI / 2) + longBuf[i] * Math.Sin(t * Math.PI / 2); }
            return o;
        }

        public static double[] Wind(int sr, int n, Random r, double strength, double baseHz)
        {
            double[] o = new double[n]; double low = 0, band = 0, brown = 0; double ph = r.NextDouble() * 6;
            for (int i = 0; i < n; i++)
            {
                double t = i / (double)sr;
                double fc = baseHz + baseHz * 0.8 * Math.Sin(TAU * 0.045 * t + ph) + baseHz * 0.5 * Math.Sin(TAU * 0.13 * t + 2 * ph);
                double f = 2 * Math.Sin(Math.PI * Math.Max(60, fc) / sr);
                double x = r.NextDouble() * 2 - 1;
                low += f * band; double high = x - low - 0.25 * band; band += f * high;
                brown = brown * 0.995 + x * 0.05;
                double env = 0.55 + 0.25 * Math.Sin(TAU * 0.07 * t + ph) + 0.2 * Math.Sin(TAU * 0.19 * t + 1);
                o[i] = (band * 0.6 + brown * 0.9) * env * strength;
            }
            return o;
        }

        public static double[] Ambience(Season s, int sr, double seconds, int seed)
        {
            Random r = new Random(seed);
            int n = (int)(seconds * sr), xf = (int)(2.5 * sr);
            double[] bed;
            switch (s)
            {
                case Season.Winter: bed = Wind(sr, n + xf, r, 1.0, 420); break;
                case Season.Autumn: bed = Wind(sr, n + xf, r, 0.7, 520); break;
                default: bed = Wind(sr, n + xf, r, 0.25, 600); break;
            }
            double[] o = Loop(bed, n, xf);
            if (s == Season.Winter)
            {
                // a distant owl, twice per loop
                for (int k = 0; k < 2; k++)
                {
                    double t0 = seconds * (0.1 + 0.5 * k) + r.NextDouble();
                    for (int h = 0; h < 2; h++)
                    {
                        double ts = t0 + h * 0.55; int s0 = (int)(ts * sr), len = (int)(0.38 * sr);
                        for (int i = 0; i < len; i++) { double t = i / (double)sr; double env = Math.Sin(Math.PI * t / 0.38); double f = 390 - 30 * t / 0.38; Add(o, s0 + i, 0.06 * env * env * Math.Sin(TAU * f * t + 0.3 * Math.Sin(TAU * 5 * t)), true); }
                    }
                }
            }
            if (s == Season.Spring)
            {
                for (int k = 0; k < 14; k++)
                {
                    double t0 = r.NextDouble() * seconds; int notesN = 2 + r.Next(5);
                    double f1 = 2400 + r.Next(1800), f2 = f1 + 600 + r.Next(1400), amp = 0.03 + r.NextDouble() * 0.04;
                    for (int c = 0; c < notesN; c++)
                    {
                        int s0 = (int)((t0 + c * 0.11) * sr), len = (int)((0.05 + r.NextDouble() * 0.04) * sr);
                        for (int i = 0; i < len; i++) { double t = i / (double)len; double f = f1 + (f2 - f1) * t; double env = Math.Sin(Math.PI * t); Add(o, s0 + i, amp * env * Math.Sin(TAU * f * i / sr + 2 * Math.Sin(TAU * 30 * i / sr)), true); }
                    }
                }
            }
            if (s == Season.Summer)
            {
                for (int cr = 0; cr < 6; cr++)
                {
                    double fc = 4300 + r.Next(900), amp = 0.012 + r.NextDouble() * 0.02, period = 0.45 + r.NextDouble() * 0.5, t = r.NextDouble();
                    while (t < seconds)
                    {
                        if (r.Next(6) > 0)
                            for (int pulse = 0; pulse < 3; pulse++)
                            {
                                int s0 = (int)((t + pulse * 0.032) * sr), len = (int)(0.018 * sr);
                                for (int i = 0; i < len; i++) { double e = Math.Sin(Math.PI * i / len); Add(o, s0 + i, amp * e * Math.Sin(TAU * fc * i / sr), true); }
                            }
                        t += period * (0.9 + r.NextDouble() * 0.2);
                    }
                }
            }
            if (s == Season.Autumn)
            {
                for (int k = 0; k < 40; k++)
                {
                    double t0 = r.NextDouble() * seconds; int len = (int)((0.15 + r.NextDouble() * 0.5) * sr); double amp = 0.05 + r.NextDouble() * 0.08;
                    double low = 0, band = 0; double f = 2 * Math.Sin(Math.PI * (2500 + r.Next(2500)) / sr);
                    int s0 = (int)(t0 * sr);
                    for (int i = 0; i < len; i++)
                    {
                        double x = (r.NextDouble() * 2 - 1) * (r.Next(7) == 0 ? 1.0 : 0.35);
                        low += f * band; double high = x - low - 0.6 * band; band += f * high;
                        double e = Math.Sin(Math.PI * i / len); Add(o, s0 + i, band * amp * e, true);
                    }
                }
            }
            return o;
        }

        // Ambience that follows the title-screen cycle: winter -> spring -> summer -> autumn, equal-power cross-fades
        // aligned with the image cross-fades (segSec per season, last fadeSec of each segment fades into the next).
        public static double[] AmbienceCycle(int sr, double segSec, double fadeSec, int seed)
        {
            Season[] order = { Season.Winter, Season.Spring, Season.Summer, Season.Autumn };
            double total = segSec * order.Length;
            double[][] amb = new double[order.Length][];
            double[] gain = new double[order.Length];
            for (int i = 0; i < order.Length; i++)
            {
                amb[i] = Ambience(order[i], sr, total, seed + i);
                double s2 = 0; foreach (double v in amb[i]) s2 += v * v;
                gain[i] = 0.12 / Math.Sqrt(s2 / amb[i].Length + 1e-12);
            }
            int n = amb[0].Length;
            double[] o = new double[n];
            double hold = segSec - fadeSec;
            for (int i = 0; i < n; i++)
            {
                double t = i / (double)sr;
                int seg = ((int)(t / segSec)) % order.Length;
                double pos = t - seg * segSec, wA = 1, wB = 0;
                if (pos > hold) { double x = (pos - hold) / fadeSec; double k = x * x * (3 - 2 * x); wA = Math.Cos(k * Math.PI / 2); wB = Math.Sin(k * Math.PI / 2); }
                int nx = (seg + 1) % order.Length;
                o[i] = amb[seg][i] * gain[seg] * wA + amb[nx][i] * gain[nx] * wB;
            }
            return o;
        }

        public static double[] Click(int sr)
        {
            int n = (int)(0.09 * sr); double[] o = new double[n]; Random r = new Random(3); double lp = 0;
            for (int i = 0; i < n; i++)
            {
                double t = i / (double)sr;
                lp = lp * 0.7 + (r.NextDouble() * 2 - 1) * 0.3;
                o[i] = (Math.Sin(TAU * 520 * t) * 0.7 + Math.Sin(TAU * 860 * t) * 0.3) * Math.Exp(-t * 70) + lp * Math.Exp(-t * 160) * 0.8;
            }
            return o;
        }

        public static double[] Hover(int sr)
        {
            int n = (int)(0.05 * sr); double[] o = new double[n];
            for (int i = 0; i < n; i++) { double t = i / (double)sr; o[i] = Math.Sin(TAU * 1760 * t) * Math.Exp(-t * 120) * Math.Min(1, t / 0.002); }
            return o;
        }
    }

    // ------------------------------------------------------------------ entry point
    public static class Program
    {
        public static string Run(string outDir, int frames, int fps)
        {
            StringBuilder log = new StringBuilder();
            string ui = Path.Combine(outDir, "ui"), mus = Path.Combine(outDir, "musica"), snd = Path.Combine(outDir, "sonidos"), esc = Path.Combine(outDir, "escenas");
            foreach (string d in new[] { outDir, ui, mus, snd, esc }) Directory.CreateDirectory(d);

            string[] names = { "invierno", "primavera", "verano", "otono", "neutral" };
            Season[] ss = { Season.Winter, Season.Spring, Season.Summer, Season.Autumn, Season.Neutral };
            for (int i = 0; i < ss.Length; i++)
            {
                Scene sc = new Scene(ss[i], 7, frames);
                List<Canvas> fr = sc.Frames();
                string p = Path.Combine(esc, names[i] + ".apng");   // FancyMenu only animates files with the .apng extension
                Apng.Write(p, fr, 1, fps);
                fr[0].Save(Path.Combine(esc, names[i] + "_preview.png"), 2);
                log.AppendLine(names[i] + ".apng " + (new FileInfo(p).Length / 1024) + " KB");
                if (ss[i] == Season.Neutral)
                {
                    fr[0].Save(Path.Combine(esc, "carga_inicial.png"), 4);
                    Canvas dark = fr[0].Copy();
                    for (int k = 0; k < dark.P.Length; k++) dark.P[k] = Canvas.Shade(dark.P[k] | unchecked((int)0xFF000000), 0.55);
                    dark.Save(Path.Combine(esc, "fondo_menus.png"), 1);
                }
            }
            // seasonal cycle for the title screen: 4 x 64 frames @ 8 fps = 32 s (6.5 s hold + 1.5 s cross-fade each)
            List<Canvas> cyc = Scene.Cycle(64, 12, 5);
            string cp = Path.Combine(esc, "ciclo.apng");
            Apng.Write(cp, cyc, 1, 8);
            for (int k = 0; k < 4; k++) cyc[k * 64 + 58].Save(Path.Combine(esc, "ciclo_preview_" + k + ".png"), 2);
            log.AppendLine("ciclo.apng " + cyc.Count + " frames " + (new FileInfo(cp).Length / 1024) + " KB");
            Ui.Logo(false, 5).Save(Path.Combine(ui, "logo.png"), 1);
            Ui.Logo(true, 5).Save(Path.Combine(ui, "logo_nieve.png"), 1);
            Ui.Logo(false, 5).Save(Path.Combine(ui, "logo_x3.png"), 3);
            Ui.Button(0, 11).Save(Path.Combine(ui, "boton.png"), 1);
            Ui.Button(1, 11).Save(Path.Combine(ui, "boton_hover.png"), 1);
            Ui.Button(2, 11).Save(Path.Combine(ui, "boton_inactivo.png"), 1);
            Ui.SliderBack(12).Save(Path.Combine(ui, "slider_fondo.png"), 1);
            Ui.SliderHandle(false).Save(Path.Combine(ui, "slider_mango.png"), 1);
            Ui.SliderHandle(true).Save(Path.Combine(ui, "slider_mango_hover.png"), 1);
            Ui.Panel().Save(Path.Combine(ui, "panel.png"), 1);
            Ui.Icon().Save(Path.Combine(ui, "icono_16.png"), 1);
            Ui.Icon().Save(Path.Combine(ui, "icono_32.png"), 2);
            log.AppendLine("ui ok");
            return log.ToString();
        }

        // quest-book chapter background: the valley in a given season, darkened and fading out at the edges
        public static string RunQuestBackground(string path, string season, int frame)
        {
            Season s = (Season)Enum.Parse(typeof(Season), season, true);
            Canvas c = new Scene(s, 7, 64, 1).Frame(frame);
            Canvas o = new Canvas(c.W, c.H);
            double cx = c.W / 2.0, cy = c.H / 2.0;
            for (int y = 0; y < c.H; y++) for (int x = 0; x < c.W; x++)
            {
                double dx = (x - cx) / cx, dy = (y - cy) / cy;
                double d = Math.Sqrt(dx * dx * 0.85 + dy * dy);
                double a = 1.18 - d; if (a < 0) a = 0; if (a > 1) a = 1;
                a = Math.Pow(a, 1.4) * 0.62;
                int col = Canvas.Shade(c.P[y * c.W + x] | unchecked((int)0xFF000000), 0.82);
                o.P[y * c.W + x] = Canvas.C((int)(a * 255), Canvas.R(col), Canvas.G(col), Canvas.B(col));
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            o.Save(path, 1);
            return path;
        }

        // full-screen quest-book background (FTB Quests theme "background"): opaque valley, darkened, darker at the edges
        public static string RunQuestFullBackground(string path, string season, int frame)
        {
            Season s = (Season)Enum.Parse(typeof(Season), season, true);
            Canvas c = new Scene(s, 7, 64, 1).Frame(frame);
            Canvas o = new Canvas(c.W, c.H);
            double cx = c.W / 2.0, cy = c.H / 2.0;
            for (int y = 0; y < c.H; y++) for (int x = 0; x < c.W; x++)
            {
                double dx = (x - cx) / cx, dy = (y - cy) / cy;
                double v = 1.0 - 0.35 * Math.Min(1.0, (dx * dx + dy * dy) / 2.0);   // soft vignette
                o.P[y * c.W + x] = Canvas.Shade(c.P[y * c.W + x] | unchecked((int)0xFF000000), 0.5 * v);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            o.Save(path, 1);
            return path;
        }

        // title-menu pieces (called by make-layouts.ps1, which also reads their sizes)
        public static string RunMenu(string outDir, string version)
        {
            string ui = Path.Combine(outDir, "ui", "menu");
            Directory.CreateDirectory(ui);
            string[] ids = { "solo", "amigos", "mods", "opciones", "salir" };
            string[] labels = { "JUGAR SOLO", "JUGAR CON AMIGOS", "MODS", "OPCIONES", "SALIR" };
            for (int i = 0; i < ids.Length; i++)
            {
                Ui.MenuItem(labels[i], false).Save(Path.Combine(ui, ids[i] + ".png"), 1);
                Ui.MenuItem(labels[i], true).Save(Path.Combine(ui, ids[i] + "_hover.png"), 1);
            }
            int cream = Canvas.C(234, 220, 192), gold = Canvas.C(255, 208, 120);
            Ui.SmallText("\u0001 IDIOMA", cream, 0.9).Save(Path.Combine(ui, "idioma.png"), 1);
            Ui.SmallText("\u0001 IDIOMA", gold, 1.0).Save(Path.Combine(ui, "idioma_hover.png"), 1);
            Ui.SmallText("\u0002 ACCESIBILIDAD", cream, 0.9).Save(Path.Combine(ui, "accesibilidad.png"), 1);
            Ui.SmallText("\u0002 ACCESIBILIDAD", gold, 1.0).Save(Path.Combine(ui, "accesibilidad_hover.png"), 1);
            Ui.SmallText("A CASA ANTES DE QUE OSCUREZCA", Canvas.C(232, 214, 180), 0.9).Save(Path.Combine(ui, "lema.png"), 1);
            Ui.SmallLinesRight(new[] { "HOME BEFORE DARK " + version.ToUpperInvariant(), "NO ES UN PRODUCTO OFICIAL DE MINECRAFT" },
                new[] { cream, Canvas.C(206, 194, 176) }, new[] { 0.85, 0.65 }).Save(Path.Combine(ui, "info.png"), 1);
            Ui.ShadeLeft().Save(Path.Combine(ui, "sombra_izq.png"), 1);
            Ui.ShadeBottom().Save(Path.Combine(ui, "sombra_abajo.png"), 1);
            return "menu ok";
        }

        public static string RunAudio(string outDir)
        {
            StringBuilder log = new StringBuilder();
            string mus = Path.Combine(outDir, "musica"), snd = Path.Combine(outDir, "sonidos");
            Directory.CreateDirectory(mus); Directory.CreateDirectory(snd);
            int sr = 22050;
            Snd.Chord[] p1 = {
                new Snd.Chord(36, new[] { 52, 55, 59, 64 }, new[] { 0, 4, 7, 11 }),
                new Snd.Chord(45, new[] { 52, 55, 60, 64 }, new[] { 9, 0, 4, 7 }),
                new Snd.Chord(41, new[] { 53, 57, 60, 64 }, new[] { 5, 9, 0, 4 }),
                new Snd.Chord(43, new[] { 55, 59, 62, 67 }, new[] { 7, 11, 2 }),
                new Snd.Chord(40, new[] { 52, 55, 59, 62 }, new[] { 4, 7, 11, 2 }),
                new Snd.Chord(45, new[] { 52, 57, 60, 64 }, new[] { 9, 0, 4 }),
                new Snd.Chord(38, new[] { 53, 57, 60, 65 }, new[] { 2, 5, 9, 0 }),
                new Snd.Chord(43, new[] { 55, 60, 62, 67 }, new[] { 7, 0, 2 }) };
            double[] a = Snd.Song(sr, 70, p1, 3, 101, false, new[] { 0, 2, 4, 7, 9 }, 72, 88);
            Snd.Wav(Path.Combine(mus, "hogar.wav"), a, sr, 0.8);
            Snd.Chord[] p2 = {
                new Snd.Chord(38, new[] { 50, 53, 57, 60 }, new[] { 2, 5, 9, 0 }),
                new Snd.Chord(34, new[] { 50, 53, 57, 62 }, new[] { 10, 2, 5, 9 }),
                new Snd.Chord(41, new[] { 48, 53, 57, 60 }, new[] { 5, 9, 0 }),
                new Snd.Chord(36, new[] { 48, 52, 55, 60 }, new[] { 0, 4, 7 }),
                new Snd.Chord(43, new[] { 50, 55, 58, 62 }, new[] { 7, 10, 2, 5 }),
                new Snd.Chord(38, new[] { 50, 53, 57, 62 }, new[] { 2, 5, 9 }),
                new Snd.Chord(34, new[] { 50, 53, 58, 62 }, new[] { 10, 2, 5 }),
                new Snd.Chord(45, new[] { 49, 52, 57, 61 }, new[] { 9, 1, 4 }) };
            double[] b = Snd.Song(sr, 78, p2, 3, 202, true, new[] { 2, 5, 7, 9, 0 }, 74, 89);
            Snd.Wav(Path.Combine(mus, "sendero.wav"), b, sr, 0.8);
            log.AppendLine("musica " + (a.Length / sr) + "s + " + (b.Length / sr) + "s");
            int asr = 16000;
            Season[] ss = { Season.Winter, Season.Spring, Season.Summer, Season.Autumn };
            string[] names = { "invierno", "primavera", "verano", "otono" };
            for (int i = 0; i < 4; i++) Snd.Wav(Path.Combine(snd, "ambiente_" + names[i] + ".wav"), Snd.Ambience(ss[i], asr, 32, 300 + i), asr, 0.6);
            Snd.Wav(Path.Combine(snd, "ambiente_ciclo.wav"), Snd.AmbienceCycle(asr, 8.0, 1.5, 400), asr, 0.6);
            Snd.Wav(Path.Combine(snd, "click.wav"), Snd.Click(sr), sr, 0.7);
            Snd.Wav(Path.Combine(snd, "hover.wav"), Snd.Hover(sr), sr, 0.25);
            log.AppendLine("sonidos ok");
            return log.ToString();
        }
    }
}
