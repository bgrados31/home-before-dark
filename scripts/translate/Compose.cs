// Compositional ES translator for modded block/item names built from vanilla material names.
// e.g. "Cornered Light Gray Wool" -> "Lana gris claro (esquina)", "Birch Planks Crate Slab" -> "Losa de tablones de abedul (caja)"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HBDT
{
    public class Composer
    {
        public Dictionary<string, string> Vanilla = new Dictionary<string, string>();   // EN full name -> ES
        public Dictionary<string, string> Material = new Dictionary<string, string>();  // EN material word(s) -> ES noun phrase ("roble")
        public Dictionary<string, string> Shape = new Dictionary<string, string>();     // EN shape noun(s) -> ES noun ("Losa")
        public Dictionary<string, string> Word = new Dictionary<string, string>();      // EN modifier -> ES modifier
        public Dictionary<string, int> Unknown = new Dictionary<string, int>();
        int maxVan = 1, maxMat = 1, maxShape = 1;

        public void Index()
        {
            foreach (string k in Vanilla.Keys) maxVan = Math.Max(maxVan, k.Split(' ').Length);
            foreach (string k in Material.Keys) maxMat = Math.Max(maxMat, k.Split(' ').Length);
            foreach (string k in Shape.Keys) maxShape = Math.Max(maxShape, k.Split(' ').Length);
        }

        static string Lower1(string s) { return s.Length > 0 ? char.ToLowerInvariant(s[0]) + s.Substring(1) : s; }
        static string Upper1(string s) { return s.Length > 0 ? char.ToUpperInvariant(s[0]) + s.Substring(1) : s; }

        class Cand { public int S, L; public string Es; public bool Van; }

        // returns null when it cannot translate confidently
        public string Translate(string en, bool record)
        {
            string[] w = en.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (w.Length == 0) return null;
            string direct;
            if (Vanilla.TryGetValue(string.Join(" ", w), out direct)) return direct;
            List<Cand> cands = new List<Cand>();
            int maxLen = Math.Max(maxVan, maxMat);
            for (int len = Math.Min(maxLen, w.Length); len >= 1; len--)
                for (int s = 0; s + len <= w.Length; s++)
                {
                    string span = string.Join(" ", w, s, len), es;
                    if (len <= maxVan && Vanilla.TryGetValue(span, out es)) cands.Add(new Cand { S = s, L = len, Es = es, Van = true });
                    if (len <= maxMat && Material.TryGetValue(span, out es)) cands.Add(new Cand { S = s, L = len, Es = es, Van = false });
                }
            if (cands.Count == 0) { if (record) foreach (string x in w) Note(x); return null; }
            foreach (Cand c in cands.Take(8))
            {
                string r = Compose(w, c, false);
                if (r != null) return r;
            }
            if (record) Compose(w, cands[0], true);
            return null;
        }

        string Compose(string[] w, Cand c, bool record)
        {
            int ms = c.S, ml = c.L;
            string shapeEs = null; int shapeStart = w.Length;
            for (int len = Math.Min(maxShape, w.Length - (ms + ml)); len >= 1; len--)
            {
                string span = string.Join(" ", w, w.Length - len, len), es;
                if (Shape.TryGetValue(span, out es)) { shapeEs = es; shapeStart = w.Length - len; break; }
            }
            List<string> mods = new List<string>();
            bool ok = true;
            for (int i = 0; i < w.Length; i++)
            {
                if (i >= ms && i < ms + ml) continue;
                if (i >= shapeStart) continue;
                string es;
                if (i + 1 < w.Length && !(i + 1 >= ms && i + 1 < ms + ml) && i + 1 < shapeStart && Word.TryGetValue(w[i] + " " + w[i + 1], out es)) { if (es.Length > 0) mods.Add(es); i++; continue; }
                if (Word.TryGetValue(w[i], out es)) { if (es.Length > 0) mods.Add(es); continue; }
                ok = false; if (record) Note(w[i]);
            }
            if (!ok) return null;
            string result;
            if (shapeEs != null) result = shapeEs + " de " + (c.Van ? Lower1(c.Es) : c.Es);
            else result = c.Es;
            if (mods.Count > 0) result += " (" + string.Join(", ", mods.Distinct()) + ")";
            return Upper1(result);
        }

        void Note(string x) { int n; Unknown.TryGetValue(x, out n); Unknown[x] = n + 1; }
    }
}
