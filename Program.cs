using cert_mama.Properties;

using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;
using Microsoft.Toolkit.Uwp.Notifications;

using System.Net;
using System.Net.Http;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

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
        public Thread? poll_servers_t = null;
        public bool want_exit = false;
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

            poll_servers_t = new Thread(PollServersThread);
            poll_servers_t.Start();
        }

        public void MenuItemClicked(object? sender, ToolStripItemClickedEventArgs e)
        {
            string clicked_txt = (""+e.ClickedItem).ToLower().Trim();
            if (clicked_txt.Equals("exit"))
            {
                want_exit = true;
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

        public void PollServersThread()
        {
            while (!this.want_exit)
            {
                try
                {
                    var server_txt_f = Settings.Default.URLsToCheckTextFileMonth;
                    if (server_txt_f.Length > 2 && File.Exists(server_txt_f))
                    {
                        const Int32 BufferSize = 1024;
                        using (var fileStream = File.OpenRead(server_txt_f))
                        {
                            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
                            {
                                String line;
                                while ((line = streamReader.ReadLine()) != null)
                                {
                                    if (!string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith("#"))
                                    {
                                        try
                                        {
                                            this.InspectOneUrl(line.Trim());
                                        }
                                        catch (Exception e)
                                        {
                                            Debug.WriteLine(e);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
                Thread.Sleep(9000);
            }
        }

        public void InspectOneUrl(string url)
        {
            var handler = new HttpClientHandler
            {
                UseDefaultCredentials = true,

                ServerCertificateCustomValidationCallback = (sender, cert, chain, error) =>
                {

                    // Access cert object.
                    X509Certificate2UI.DisplayCertificate(cert);

                    return true;
                }
            };

            using (HttpClient client = new HttpClient(handler))
            {
                using (HttpResponseMessage response = client.GetAsync(url).Result)
                {
                    using (HttpContent content = response.Content)
                    {

                    }
                }
            }



            //Debug.WriteLine("Inspecting " + url);
            new ToastContentBuilder()
                .SetToastScenario(ToastScenario.IncomingCall)
                .AddArgument("action", "viewConversation")
                //.AddArgument("conversationId", 9813)
                .AddText("Inspecting " + url)
                //.AddText("Check this out, The Enchantments in Washington!")
                .Show();
            
        }
    }
}
