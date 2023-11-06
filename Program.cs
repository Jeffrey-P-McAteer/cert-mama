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
using WinRT;

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
            icon_menu.Items.Add("Check Certs Now");
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
            else if (clicked_txt.Contains("check") && clicked_txt.Contains("now"))
            {
                last_url_poll_time.Clear(); // Forget what we polled for previously
                PollServersOnce();
            }

        }

        public void PollServersThread()
        {
            while (!this.want_exit)
            {
                PollServersOnce();
                // Inspect new URLs from text file every 15 minutes, if they exist. Most of this will be a no-op.
                for (int i = 0; i < 15 * 60; i += 1)
                {
                    Thread.Sleep(1000);
                    if (this.want_exit)
                    {
                        break;
                    }
                }
            }
        }

        private Dictionary<string, DateTime> last_url_poll_time = new Dictionary<string, DateTime>();

        public void PollServersOnce()
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
        }

        public void InspectOneUrl(string url)
        {
            var now = DateTime.Now;
            // Have we inspected this within the last 22 hours?
            if (last_url_poll_time.ContainsKey(url))
            {
                var url_last_poll_time = last_url_poll_time[url];
                var hours_since_last_poll = Math.Abs((now - url_last_poll_time).TotalHours);
                if (hours_since_last_poll <= 22)
                {
                    return; // Don't re-poll
                }
            }

            DateTime? cert_expire_date = null;

            try
            {
                var handler = new HttpClientHandler
                {
                    UseDefaultCredentials = true,

                    ServerCertificateCustomValidationCallback = (sender, cert, chain, error) =>
                    {

                        if (cert != null)
                        {
                            // Access cert object.
                            //X509Certificate2UI.DisplayCertificate(cert);
                            cert_expire_date = cert.NotAfter;
                        }

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
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            last_url_poll_time[url] = now;

            if (cert_expire_date == null)
            {
                new ToastContentBuilder()
                    .SetToastScenario(ToastScenario.IncomingCall)
                    .AddText("Unable to get HTTPS certificate expiration Date!")
                    .AddText("" + url)
                    .Show();
                return;
            }

            var one_month_in_future = DateTime.Today.AddMonths(1);
            if (cert_expire_date < DateTime.Today)
            {
                new ToastContentBuilder()
                    .SetToastScenario(ToastScenario.IncomingCall)
                    .AddText("Certificate Has Expired " + Math.Abs(Math.Round((DateTime.Today - (DateTime)cert_expire_date).TotalDays, 0)) + " Days Ago!")
                    .AddText("" + url)
                    .Show();
            }
            else if (cert_expire_date < one_month_in_future)
            {
                // Cert will expire within 1 month of DateTime.Today!
                new ToastContentBuilder()
                    .SetToastScenario(ToastScenario.IncomingCall)
                    .AddText("Certificate Expires in "+Math.Abs(Math.Round((one_month_in_future - (DateTime) cert_expire_date).TotalDays, 0))+" Days!")
                    .AddText("" + url)
                    .Show();
            }
            
        }
    }
}
