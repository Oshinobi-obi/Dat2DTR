using System.Reflection;

namespace Dat2DTR
{
    /// <summary>
    /// This is a JPEG picture: its bytes and its size in pixels. A PDF can carry a JPEG as it is, and Windows can
    /// draw it, so no picture library is needed. Only the size is read here (from the JPEG's "frame" header).
    /// </summary>
    public sealed class JpegImage
    {
        public byte[] Data = new byte[0];
        public int Width, Height;

        /// <summary>This is the number of colour channels: 3 = colour (RGB), 1 = grey.</summary>
        public int Components = 3;

        /// <summary>This is a JPEG read from its bytes, or null when they are not a JPEG this program can use.</summary>
        public static JpegImage? Read(byte[] data)
        {
            if (data.Length < 4 || data[0] != 0xFF || data[1] != 0xD8)
                return null;
            int i = 2;
            while (i + 9 < data.Length)
            {
                if (data[i] != 0xFF) { i++; continue; }
                int marker = data[i + 1];
                if (marker == 0xFF) { i++; continue; }
                int length = (data[i + 2] << 8) | data[i + 3];
                // This is the frame header (baseline 0xC0, extended 0xC1, progressive 0xC2): height, width, channels.
                if (marker == 0xC0 || marker == 0xC1 || marker == 0xC2)
                {
                    var image = new JpegImage();
                    image.Data = data;
                    image.Height = (data[i + 5] << 8) | data[i + 6];
                    image.Width = (data[i + 7] << 8) | data[i + 8];
                    image.Components = data[i + 9];
                    return image.Width > 0 && image.Height > 0 && (image.Components == 3 || image.Components == 1) ? image : null;
                }
                i += 2 + length;
            }
            return null;
        }
    }

    /// <summary>
    /// This is the DepEd logo at the top of the Certificate of Leave Credits. It is built into the program
    /// (Assets\DepEd-Logo.jpg, made from DepEd-Logo.webp - Windows printing cannot read .webp pictures).
    /// </summary>
    public static class CertificateLogo
    {
        /// <summary>This is the name of the picture built into the program (see Dat2DTR.csproj).</summary>
        public const string ResourceName = "DepEd-Logo.jpg";

        private static JpegImage? _logo;
        private static bool _looked;

        /// <summary>This is the logo, or null when it is missing (the certificate is then made without it).</summary>
        public static JpegImage? Get()
        {
            if (!_looked)
            {
                _looked = true;
                try
                {
                    using (Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName))
                    {
                        if (stream != null)
                        {
                            var memory = new MemoryStream();
                            stream.CopyTo(memory);
                            _logo = JpegImage.Read(memory.ToArray());
                        }
                    }
                }
                catch (IOException) { _logo = null; }
            }
            return _logo;
        }

        /// <summary>This is for the self test: the logo read from a file instead of from the program.</summary>
        public static void Use(JpegImage? logo)
        {
            _logo = logo;
            _looked = true;
        }
    }
}
