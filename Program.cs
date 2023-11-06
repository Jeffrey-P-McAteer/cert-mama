using cert_mama.Properties;
using System;

namespace CertMama // Note: actual namespace depends on the project name.
{
    public class MainCertMama
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World! URLS = " + Settings.Default.URLsToCheckTextFileMonth );
            Console.WriteLine("Set URLsToCheckTextFileMonth: ");
            Settings.Default.URLsToCheckTextFileMonth = Console.ReadLine();
            Settings.Default.Save();

        }
    }
}
