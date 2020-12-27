using System;
using System.Text;
using System.Diagnostics;
using System.Security.Cryptography;
using System.IO;
using System.Threading.Tasks;
using Open.Nat;

namespace Server
{
    static class Commands
    {
        public static void Beep(int frequency = 3000, int duration = 2000)
        {
            Console.Beep(frequency, duration);
        }

        public static int StartProcess(string processPath, string[] args)
        {
            Process process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = processPath,
                UseShellExecute = true,
                Arguments = String.Join(' ', args)
            };

            process.Start();
            return process.Id;

        }

        public static string ExecuteCMD(string[] args)
        {
            Process process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                UseShellExecute = false,
                Arguments = "/c" + String.Join(' ', args),
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            //process.WaitForExit();
            return output;
        }

        public static void EncryptFile(string inputFile, string outputFile, string password) //sa scot try catch de aici si sa bag in main
        {
            //le-am scos din try ca sa pot sa le inchid in catch
            FileStream fsCrypt = new FileStream(outputFile, FileMode.Create);
            FileStream fsIn = new FileStream(inputFile, FileMode.Open);

            try
            {
                UnicodeEncoding UE = new UnicodeEncoding();
                byte[] key = UE.GetBytes(password);

                RijndaelManaged RMCrypto = new RijndaelManaged();

                CryptoStream cs = new CryptoStream(fsCrypt,
                    RMCrypto.CreateEncryptor(key, key),
                    CryptoStreamMode.Write);

                int data;
                while ((data = fsIn.ReadByte()) != -1)
                    cs.WriteByte((byte)data);

                fsIn.Close();
                cs.Close();
                fsCrypt.Close();
            }
            catch (Exception ex)
            {
                //in caz ca cheia e gresita inchide filestream-ul ca sa mai poata fi accesat fisierul
                fsIn.Close();
                fsCrypt.Close();

                //daca dadea eroare tot ramanea fisierul
                if (File.Exists(outputFile))
                    File.Delete(outputFile);

                throw ex;
            }

        }

        public static void DecryptFile(string inputFile, string outputFile, string password)
        {
            //le-am scos din try ca sa pot sa le inchid in catch
            FileStream fsCrypt = new FileStream(inputFile, FileMode.Open);
            FileStream fsOut = new FileStream(outputFile, FileMode.Create);

            try
            {
                UnicodeEncoding UE = new UnicodeEncoding();
                byte[] key = UE.GetBytes(password);

                RijndaelManaged RMCrypto = new RijndaelManaged();

                CryptoStream cs = new CryptoStream(fsCrypt,
                    RMCrypto.CreateDecryptor(key, key),
                    CryptoStreamMode.Read);

                int data;
                while ((data = cs.ReadByte()) != -1)
                    fsOut.WriteByte((byte)data);

                fsOut.Close();
                cs.Close();
                fsCrypt.Close();
            }

            catch (Exception ex)
            {
                //in caz ca cheia e gresita inchide filestream-ul ca sa mai poata fi accesat fisierul
                fsOut.Close();
                fsCrypt.Close();

                //daca dadea eroare tot ramanea fisierul
                if (File.Exists(outputFile))
                    File.Delete(outputFile);

                throw ex;
            }
        }
    }
}
