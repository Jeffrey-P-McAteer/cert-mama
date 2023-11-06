using cert_mama.Properties;

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
// using System.Windows.Controls;

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
            else
            {
                Console.WriteLine("Got Args: " + string.Join(", ", args));
                Console.WriteLine("URLsToCheckTextFileMonth = " + Settings.Default.URLsToCheckTextFileMonth);

            }

            Application.SetCompatibleTextRenderingDefault(false);
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
            var icon_menu = new ContextMenuStrip();

            icon_menu.Items.Add("Select Monthly URL Text File");
            icon_menu.Items.Add("Exit");

            icon_menu.ItemClicked += MenuItemClicked;

            tray_icon.ContextMenuStrip = icon_menu;

        }

        public void MenuItemClicked(object? sender, ToolStripItemClickedEventArgs e)
        {
            string clicked_txt = (""+e.ClickedItem).ToLower().Trim();
            if (clicked_txt.Equals("exit"))
            {
                Application.Exit();
            }
            else if (clicked_txt.Contains("select") && clicked_txt.Contains("text file"))
            {
                var file_picker = new OpenFileDialog()
                {
                    Title = "Select a text file with URLs to check",
                    DefaultExt = ".txt",
                };
                var r = file_picker.ShowDialog();
                if (r == DialogResult.OK)
                {
                    Settings.Default.URLsToCheckTextFileMonth = file_picker.FileName;
                    Settings.Default.Save();
                }
            }

        }
    }
}
