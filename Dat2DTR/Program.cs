namespace Dat2DTR
{
    internal static class Program
    {
        /// <summary>
        /// This is the main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // This is the application configuration. To customize it, such as setting high DPI or the default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}