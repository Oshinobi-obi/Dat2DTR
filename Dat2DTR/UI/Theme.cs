namespace Dat2DTR
{
    /// <summary>
    /// Colours, logos and button looks, kept in one place so every window matches.
    ///
    /// The colours come from the DepEd NCR seal (navy and yellow) and the
    /// "One NCR, Stronger Together" banner (orange and red).
    /// To re-colour the whole program, change the values here.
    /// (Form1.Designer.cs repeats a few of them so the designer preview looks right.)
    /// </summary>
    internal static class Theme
    {
        // ---- the four logo colours
        public static readonly Color Navy = Color.FromArgb(0, 0, 128);
        public static readonly Color Yellow = Color.FromArgb(255, 223, 0);
        public static readonly Color Orange = Color.FromArgb(250, 175, 58);
        public static readonly Color Red = Color.FromArgb(219, 31, 46);

        // ---- where they are used
        public static readonly Color Header = Navy;                               // the bar at the top
        public static readonly Color HeaderButtonHover = Color.FromArgb(30, 30, 170);
        public static readonly Color HeaderText = Color.FromArgb(255, 240, 170);  // small text on the navy bar
        public static readonly Color Accent = Navy;                               // main buttons, headings, step numbers
        public static readonly Color AccentHover = Color.FromArgb(30, 30, 170);
        public static readonly Color AccentDisabled = Color.FromArgb(170, 170, 205);
        public static readonly Color Warm = Color.FromArgb(224, 130, 0);          // orange dark enough to read on white
        public static readonly Color Highlight = Color.FromArgb(255, 247, 204);   // pale yellow when the mouse is over a choice
        public static readonly Color Panel = Color.FromArgb(246, 246, 251);
        public static readonly Color PreviewBackground = Color.FromArgb(226, 227, 236);
        public static readonly Color Line = Color.FromArgb(208, 210, 226);
        public static readonly Color TextStrong = Color.FromArgb(30, 32, 64);
        public static readonly Color TextMuted = Color.FromArgb(96, 100, 124);
        public static readonly Color NoticeBack = Color.FromArgb(255, 241, 173);
        public static readonly Color NoticeText = Color.FromArgb(0, 0, 110);

        // ---- names of the pictures that are built into the .exe (see Dat2DTR.csproj)
        public const string SealImage = "DepEd-NCR-seal.png";
        public const string BannerImage = "One-NCR-banner-plate.png";

        private static Icon? _appIcon;
        private static bool _appIconLoaded;
        private static readonly Dictionary<string, Image?> _images = new Dictionary<string, Image?>();

        /// <summary>
        /// The program's logo as a window icon. The file Assets\Dat2DTR.ico is built
        /// into the .exe (see Dat2DTR.csproj), so it is read from there, not from disk.
        /// </summary>
        public static Icon? GetAppIcon()
        {
            if (!_appIconLoaded)
            {
                _appIconLoaded = true;
                using (Stream? stream = typeof(Theme).Assembly.GetManifestResourceStream("Dat2DTR.ico"))
                {
                    if (stream != null)
                        _appIcon = new Icon(stream);
                }
            }
            return _appIcon;
        }

        /// <summary>
        /// One of the built-in pictures (use Theme.SealImage or Theme.BannerImage).
        /// Returns null if the picture is missing, so the program still runs without it.
        /// </summary>
        public static Image? GetImage(string name)
        {
            Image? image;
            if (_images.TryGetValue(name, out image))
                return image;

            image = null;
            using (Stream? stream = typeof(Theme).Assembly.GetManifestResourceStream(name))
            {
                if (stream != null)
                {
                    // A picture needs its data to stay available for as long as it is shown,
                    // so the data is copied into memory first.
                    var memory = new MemoryStream();
                    stream.CopyTo(memory);
                    memory.Position = 0;
                    image = Image.FromStream(memory);
                }
            }

            _images[name] = image;
            return image;
        }

        /// <summary>The main "do this" buttons: solid navy with white text.</summary>
        public static void StylePrimary(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = AccentHover;
            button.FlatAppearance.MouseDownBackColor = AccentHover;
            button.ForeColor = Color.White;
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;

            // A disabled navy button with grey text is hard to read, so it gets its own colour.
            EventHandler update = (sender, e) =>
                button.BackColor = button.Enabled ? Accent : AccentDisabled;
            button.EnabledChanged += update;
            update(button, EventArgs.Empty);
        }

        /// <summary>The buttons on the navy bar at the top: navy with a yellow outline.</summary>
        public static void StyleHeaderButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Yellow;
            button.FlatAppearance.MouseOverBackColor = HeaderButtonHover;
            button.FlatAppearance.MouseDownBackColor = HeaderButtonHover;
            button.BackColor = Header;
            button.ForeColor = Color.White;
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
        }
    }
}
