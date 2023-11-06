using cert_mama.Properties;

using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace CertMama // Note: actual namespace depends on the project name.
{
    public class MainCertMama
    {
        public static CertMamaApp? cma = null;
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                var handle = ConsoleUtils.GetConsoleWindow();
                ConsoleUtils.ShowWindow(handle, ConsoleUtils.SW_HIDE);
            }

            Console.WriteLine("Got Args: " + string.Join(", ", args));

            Application.EnableVisualStyles();
            cma = new CertMamaApp();
            Application.Run(cma);

            /*
            Console.WriteLine("Hello World! URLS = " + Settings.Default.URLsToCheckTextFileMonth );
            Console.WriteLine("Set URLsToCheckTextFileMonth: ");
            Settings.Default.URLsToCheckTextFileMonth = Console.ReadLine();
            Settings.Default.Save();*/

        }

    }

    public class ConsoleUtils
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;

    }

    public class CertMamaApp: ApplicationContext
    {
        public NotifyIcon? tray_icon = null;
        public CertMamaApp(): base()
        {
            tray_icon = new NotifyIcon();
            tray_icon.Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetEntryAssembly()?.Location);
            tray_icon.Visible = true;
            tray_icon.Text = "Cert Mama";
            tray_icon.ContextMenuStrip = new ContextMenuStrip();

            
        }
    }
}
