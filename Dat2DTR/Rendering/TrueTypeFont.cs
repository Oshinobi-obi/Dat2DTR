using System.Text;

namespace Dat2DTR
{
    /// <summary>
    /// This is a TrueType font file (.ttf) read by the program itself - no font library needed. It gives
    /// the font's names, how wide each letter is (to centre text, and for the PDF) and the measurements
    /// the PDF needs to carry the font inside the file. Only the letters of Western European text
    /// (the same WinAnsi letters as the rest of the PDF) are used.
    /// </summary>
    public sealed class TrueTypeFont
    {
        /// <summary>This is the file the font was read from.</summary>
        public string Path = "";

        /// <summary>This is the whole font file, put inside the PDF.</summary>
        public byte[] Data = new byte[0];

        /// <summary>This is the family name, e.g. "Old English Text MT", and the PostScript name, e.g. "OldEnglishTextMT".</summary>
        public string FamilyName = "", StyleName = "", PostScriptName = "";

        /// <summary>This is the width of each WinAnsi letter (0 to 255) in thousandths of the font size.</summary>
        public readonly double[] Widths = new double[256];

        /// <summary>This is the measurements of the font in thousandths of the font size (what the PDF wants).</summary>
        public double Ascent, Descent, CapHeight, ItalicAngle;

        /// <summary>This is the height above the baseline that Windows uses to place the text (usWinAscent), in thousandths.</summary>
        public double WinAscent;
        public double[] BoundingBox = new double[4];

        /// <summary>This is true when the font allows being carried inside a document (it is not "restricted").</summary>
        public bool CanEmbed;

        /// <summary>This is the width of the text in points when drawn at that size.</summary>
        public double TextWidth(string text, double size)
        {
            double total = 0;
            foreach (char c in text)
                total += Widths[FontMetrics.ToWinAnsi(c)];
            return total * size / 1000.0;
        }

        /// <summary>This is a font file read from disk, or null when it is not a TrueType font that can be used.</summary>
        public static TrueTypeFont? TryLoad(string path)
        {
            try
            {
                byte[] data = File.ReadAllBytes(path);
                TrueTypeFont? font = Read(data);
                if (font != null)
                    font.Path = path;
                return font;
            }
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
            catch (IndexOutOfRangeException) { return null; }   // This is a damaged file
            catch (ArgumentOutOfRangeException) { return null; }
        }

        /// <summary>This is the reading of the font's tables (see the TrueType / OpenType specification).</summary>
        public static TrueTypeFont? Read(byte[] data)
        {
            // This is the table directory: TrueType fonts start with 0x00010000 (or "true").
            uint version = U32(data, 0);
            if (version != 0x00010000 && version != 0x74727565)
                return null;   // This is not TrueType outlines (for example a CFF ".otf" font)

            int tableCount = U16(data, 4);
            var tables = new Dictionary<string, int>();
            for (int i = 0; i < tableCount; i++)
            {
                int record = 12 + i * 16;
                string tag = Encoding.ASCII.GetString(data, record, 4);
                tables[tag] = (int)U32(data, record + 8);
            }
            foreach (string needed in new[] { "head", "hhea", "hmtx", "cmap", "name", "glyf" })
                if (!tables.ContainsKey(needed))
                    return null;

            var font = new TrueTypeFont();
            font.Data = data;

            // This is head: the size of the design grid and the box around every letter.
            int head = tables["head"];
            double unitsPerEm = U16(data, head + 18);
            if (unitsPerEm <= 0) return null;
            Func<int, double> scale = v => v * 1000.0 / unitsPerEm;
            font.BoundingBox = new[] { scale(S16(data, head + 36)), scale(S16(data, head + 38)),
                                       scale(S16(data, head + 40)), scale(S16(data, head + 42)) };

            // This is hhea: ascent, descent and how many letters have their own width.
            int hhea = tables["hhea"];
            font.Ascent = scale(S16(data, hhea + 4));
            font.Descent = scale(S16(data, hhea + 6));
            int metricCount = U16(data, hhea + 34);

            // This is OS/2: whether the font may be put inside a document, and the height of capitals.
            font.CanEmbed = true;
            font.CapHeight = font.Ascent;
            font.WinAscent = font.Ascent;
            int os2;
            if (tables.TryGetValue("OS/2", out os2))
            {
                int fsType = U16(data, os2 + 8);
                font.CanEmbed = (fsType & 0x0002) == 0 || (fsType & 0x000C) != 0;   // This is "restricted" unless preview / editable is allowed
                font.WinAscent = scale(U16(data, os2 + 74));
                if (U16(data, os2) >= 2)
                    font.CapHeight = scale(S16(data, os2 + 88));
            }
            int post;
            if (tables.TryGetValue("post", out post))
                font.ItalicAngle = (int)U32(data, post + 4) / 65536.0;

            // This is hmtx: the width of each glyph. Glyphs after metricCount use the last width.
            int hmtx = tables["hmtx"];
            Func<int, double> glyphWidth = glyph =>
            {
                int index = Math.Min(glyph, metricCount - 1);
                return scale(U16(data, hmtx + index * 4));
            };

            // This is cmap: which glyph each letter uses (Windows Unicode table, format 4).
            Func<int, int> glyphOf = FindUnicodeMap(data, tables["cmap"]);
            for (int code = 0; code < 256; code++)
            {
                int unicode = WinAnsiToUnicode(code);
                font.Widths[code] = glyphWidth(unicode < 0 ? 0 : glyphOf(unicode));
            }

            // This is name: the family name and the PostScript name.
            ReadNames(data, tables["name"], font);
            if (font.PostScriptName.Length == 0)
                font.PostScriptName = font.FamilyName.Replace(" ", "");
            return font;
        }

        /// <summary>This is the Windows Unicode letter table of the font (format 4) as a lookup: letter -> glyph (0 = missing).</summary>
        private static Func<int, int> FindUnicodeMap(byte[] data, int cmap)
        {
            int count = U16(data, cmap + 2);
            int best = -1;
            for (int i = 0; i < count; i++)
            {
                int record = cmap + 4 + i * 8;
                int platform = U16(data, record), encoding = U16(data, record + 2);
                int offset = cmap + (int)U32(data, record + 4);
                if (U16(data, offset) != 4) continue;
                if (platform == 3 && encoding == 1) { best = offset; break; }
                if (platform == 0 && best < 0) best = offset;
            }
            if (best < 0)
                return unicode => 0;

            int table = best;
            int segments = U16(data, table + 6) / 2;
            int ends = table + 14, starts = ends + segments * 2 + 2, deltas = starts + segments * 2, ranges = deltas + segments * 2;
            return unicode =>
            {
                for (int s = 0; s < segments; s++)
                {
                    int end = U16(data, ends + s * 2);
                    if (unicode > end) continue;
                    int start = U16(data, starts + s * 2);
                    if (unicode < start) return 0;
                    int delta = S16(data, deltas + s * 2);
                    int rangeOffset = U16(data, ranges + s * 2);
                    if (rangeOffset == 0)
                        return (unicode + delta) & 0xFFFF;
                    int at = ranges + s * 2 + rangeOffset + (unicode - start) * 2;
                    int glyph = U16(data, at);
                    return glyph == 0 ? 0 : (glyph + delta) & 0xFFFF;
                }
                return 0;
            };
        }

        /// <summary>This is where the family, style and PostScript names are read (Windows English names first).</summary>
        private static void ReadNames(byte[] data, int name, TrueTypeFont font)
        {
            int count = U16(data, name + 2);
            int strings = name + U16(data, name + 4);
            var found = new Dictionary<int, string>();
            var foundMac = new Dictionary<int, string>();
            for (int i = 0; i < count; i++)
            {
                int record = name + 6 + i * 12;
                int platform = U16(data, record), encoding = U16(data, record + 2), language = U16(data, record + 4);
                int id = U16(data, record + 6), length = U16(data, record + 8), offset = U16(data, record + 10);
                if (id != 1 && id != 2 && id != 6) continue;
                if (platform == 3 && (encoding == 1 || encoding == 0) && language == 0x409)
                    found[id] = Encoding.BigEndianUnicode.GetString(data, strings + offset, length);
                else if (platform == 1 && encoding == 0)
                    foundMac[id] = Encoding.ASCII.GetString(data, strings + offset, length);
            }
            Func<int, string> get = id => found.ContainsKey(id) ? found[id] : foundMac.ContainsKey(id) ? foundMac[id] : "";
            font.FamilyName = get(1).Trim();
            font.StyleName = get(2).Trim();
            var ps = new StringBuilder();
            foreach (char c in get(6))
                if (c > 32 && c < 127 && "[](){}<>/%".IndexOf(c) < 0) ps.Append(c);   // This is a name PDF can use
            font.PostScriptName = ps.ToString();
        }

        /// <summary>This is the Unicode letter of a WinAnsi code (-1 for the few codes WinAnsi leaves empty).</summary>
        internal static int WinAnsiToUnicode(int code)
        {
            if (code < 0x80 || code >= 0xA0)
                return code;
            int[] high =
            {
                0x20AC, -1, 0x201A, 0x0192, 0x201E, 0x2026, 0x2020, 0x2021, 0x02C6, 0x2030, 0x0160, 0x2039, 0x0152, -1, 0x017D, -1,
                -1, 0x2018, 0x2019, 0x201C, 0x201D, 0x2022, 0x2013, 0x2014, 0x02DC, 0x2122, 0x0161, 0x203A, 0x0153, -1, 0x017E, 0x0178
            };
            return high[code - 0x80];
        }

        private static int U16(byte[] d, int i) { return (d[i] << 8) | d[i + 1]; }
        private static int S16(byte[] d, int i) { return (short)((d[i] << 8) | d[i + 1]); }
        private static uint U32(byte[] d, int i) { return ((uint)d[i] << 24) | ((uint)d[i + 1] << 16) | ((uint)d[i + 2] << 8) | d[i + 3]; }
    }

    /// <summary>
    /// This is where the two fonts of the certificate heading are found: Old English Text MT (Republic of the
    /// Philippines, Department of Education) and Trajan Pro (National Capital Region). They are licensed fonts,
    /// so they are not built into the program. They are looked for, in this order:
    ///   1. the "Fonts" folder next to Dat2DTR.exe,
    ///   2. the "Fonts" folder in the data folder (%AppData%\Dat2DTR\Fonts),
    ///   3. the fonts installed in Windows.
    /// Only TrueType (.ttf) files can be used. When a font is not found, the certificate uses Helvetica / Arial.
    /// </summary>
    public static class CertificateFonts
    {
        public const string OldEnglish = "Old English Text MT";
        public const string Trajan = "Trajan Pro";

        private static readonly Dictionary<string, TrueTypeFont?> Cache = new Dictionary<string, TrueTypeFont?>(StringComparer.OrdinalIgnoreCase);

        /// <summary>This is the extra folders to look in first (used by the self test).</summary>
        public static readonly List<string> ExtraFolders = new List<string>();

        /// <summary>This is the font of that family (the regular style when there are several), or null when it is not found.</summary>
        public static TrueTypeFont? Find(string family)
        {
            lock (Cache)
            {
                TrueTypeFont? font;
                if (Cache.TryGetValue(family, out font))
                    return font;
                font = Search(family);
                Cache[family] = font;
                return font;
            }
        }

        /// <summary>This is where the fonts found are forgotten, so they are looked for again (after fonts were added).</summary>
        public static void Forget()
        {
            lock (Cache)
                Cache.Clear();
        }

        /// <summary>This is the list of folders searched, in order (the Windows ones only for likely file names).</summary>
        public static List<string> Folders()
        {
            var folders = new List<string>(ExtraFolders);
            folders.Add(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts"));
            folders.Add(System.IO.Path.Combine(DtrSettings.DataFolder, "Fonts"));
            return folders;
        }

        private static TrueTypeFont? Search(string family)
        {
            TrueTypeFont? best = null;
            foreach (string folder in Folders())
            {
                best = Pick(family, best, Files(folder, "*.ttf", false));
                if (best != null && IsRegular(best)) return best;
            }

            // This is the fonts installed in Windows: only files whose name looks like the family, so it stays quick.
            string[] hints = family.StartsWith("Old", StringComparison.OrdinalIgnoreCase)
                ? new[] { "oldengl", "old english", "oldenglish" } : new[] { "trajan" };
            var windows = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Fonts")
            };
            foreach (string folder in windows)
            {
                IEnumerable<string> likely = Files(folder, "*.ttf", true)
                    .Where(f => hints.Any(h => System.IO.Path.GetFileName(f).ToLowerInvariant().Contains(h)));
                best = Pick(family, best, likely);
                if (best != null && IsRegular(best)) return best;
            }
            return best;
        }

        private static bool IsRegular(TrueTypeFont font)
        {
            return font.StyleName.Length == 0 || string.Equals(font.StyleName, "Regular", StringComparison.OrdinalIgnoreCase);
        }

        private static TrueTypeFont? Pick(string family, TrueTypeFont? best, IEnumerable<string> files)
        {
            foreach (string file in files)
            {
                TrueTypeFont? font = TrueTypeFont.TryLoad(file);
                if (font == null || !string.Equals(font.FamilyName, family, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (best == null || (IsRegular(font) && !IsRegular(best)))
                    best = font;
                if (IsRegular(best)) break;
            }
            return best;
        }

        private static IEnumerable<string> Files(string folder, string pattern, bool topOnly)
        {
            try
            {
                if (!Directory.Exists(folder))
                    return new string[0];
                return Directory.GetFiles(folder, pattern, topOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories);
            }
            catch (IOException) { return new string[0]; }
            catch (UnauthorizedAccessException) { return new string[0]; }
        }
    }
}
