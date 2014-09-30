/*
 * FTP
 * author Jenny Zhen
 * date: 09.30.14
 * language: C#
 * file: Program.cs
 * assignment: FTP
 */
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace FTP
{
    /// <summary>
    /// FTP class for DataComm I
    /// </summary>
    class Ftp
    {
        // The prompt
        public const string PROMPT = "FTP> ";

		// The port to connect with
		public const int PORT = 21;

		// The line endings for network stream writer to use
		public static readonly string LINEEND = "\r\n";

        // Information to parse commands
        public static readonly string[] COMMANDS = { "ascii",
					      "binary",
					      "cd",
					      "cdup",
					      "debug",
					      "dir",
					      "get",
					      "help",
					      "passive",
                          "put",
                          "pwd",
                          "quit",
                          "user" };

        public const int ASCII = 0;
        public const int BINARY = 1;
        public const int CD = 2;
        public const int CDUP = 3;
        public const int DEBUG = 4;
        public const int DIR = 5;
        public const int GET = 6;
        public const int HELP = 7;
        public const int PASSIVE = 8;
        public const int PUT = 9;
        public const int PWD = 10;
        public const int QUIT = 11;
        public const int USER = 12;

        // Help message

        public static readonly String[] HELP_MESSAGE = {
	        "ascii      --> Set ASCII transfer type",
	        "binary     --> Set binary transfer type",
	        "cd <path>  --> Change the remote working directory",
	        "cdup       --> Change the remote working directory to the",
                "               parent directory (i.e., cd ..)",
	        "debug      --> Toggle debug mode",
	        "dir        --> List the contents of the remote directory",
	        "get path   --> Get a remote file",
	        "help       --> Displays this text",
	        "passive    --> Toggle passive/active mode",
            "put path   --> Transfer the specified file to the server",
	        "pwd        --> Print the working directory on the server",
            "quit       --> Close the connection to the server and terminate",
	        "user login --> Specify the user name (will prompt for password" };



        static void Main(string[] args)
        {
			bool debug = false;
			bool passive = false;
            bool eof = false;
            String input = null;
			String server = null;
			TcpClient connection = null;
			TcpClient client = null;
			TcpListener listener = null;
			NetworkStream stream = null;
			StreamReader mreader = null;
			StreamWriter mwriter = null;
			NetworkStream dstream = null;
			StreamReader dreader = null;
			Stream strm = null;
			StreamReader strmreader = null;
			int randomport = new Random().Next(1025, 65535);
			int filesize = 0;

            // Handle the command line.

			if (args.Length == 1)
			{
				server = args[0];
				try
				{
					// Try to connect to the given server and port.
					connection = new TcpClient(server, PORT);
					stream = connection.GetStream();
					mreader = new StreamReader(stream);
					mwriter = new StreamWriter(stream);
					listener = new TcpListener(IPAddress.Any, 0);
					listener.Start(); // Start listening for incoming connection requests.

					// Display the welcome text.
					ReadOutput(mreader);
				}
				catch (ArgumentNullException e)
				{
					Console.WriteLine("ArgumentNullException: {0}", e);
				}
				catch (ArgumentOutOfRangeException e)
				{
					Console.WriteLine("ArgumentOutOfRangeException: {0}", e);
				}
				catch (SocketException)
				{
					Console.Error.WriteLine(server + ": Name or service not known");
					Console.ReadKey();
					Environment.Exit(1);
				}
			}
			else
			{
				Console.Error.WriteLine("Usage: [mono] Ftp server");
				Console.ReadKey();
				Environment.Exit(1);
			}

			// Have the user log in with the username.
			Console.Write("Name (" + server + ":" + Environment.UserName + "): ");
			String user = Console.ReadLine();
			mwriter.Write("USER " + user.Trim() + LINEEND);
			mwriter.Flush();
			ReadOutput(mreader);

			// Have the user log in with the password.
			Console.Write("Password: ");
			String password = Console.ReadLine();
			mwriter.Write("PASS " + password.Trim() + LINEEND);
			mwriter.Flush();
			ReadOutput(mreader);

			// Display the system type.
			mwriter.Write("SYST" + LINEEND);
			mwriter.Flush();
			String output = mreader.ReadLine();
			Console.WriteLine("Remote system type is " + output.Split(' ')[1]);

			// Display the default file transfer mode.
			mwriter.Write("TYPE I" + LINEEND);
			mwriter.Flush();
			mreader.ReadLine();
			Console.WriteLine("Using binary mode to transfer files.");

            // Command line is done - accept commands.
            do
            {
                try
                {
                    Console.Write(PROMPT);
                    input = Console.ReadLine();

                }
                catch (Exception e)
                {
                    eof = true;
                }

                // Keep going if we have not hit end of file.
                if (!eof && input.Length > 0)
                {
                    int cmd = -1;
                    string[] argv = Regex.Split(input, "\\s+");

                    // What command was entered?
                    for (int i = 0; i < COMMANDS.Length && cmd == -1; i++)
                    {
                        if (COMMANDS[i].Equals(argv[0], StringComparison.CurrentCultureIgnoreCase))
                        {
                            cmd = i;
                        }
                    }

                    // Execute the command.
                    switch (cmd)
                    {
                        case ASCII:
							if (debug)
								Console.WriteLine("---> TYPE A");
							RunCommand(mwriter, mreader, "TYPE A");
                            break;

                        case BINARY:
							if (debug)
								Console.WriteLine("---> TYPE I");
							RunCommand(mwriter, mreader, "TYPE I");
                            break;

                        case CD:
							if (argv.Length != 2)
								Console.WriteLine("Usage: CD <path>");
							else
								RunCommand(mwriter, mreader, "CWD", argv[1]);
                            break;

                        case CDUP:
							RunCommand(mwriter, mreader, "CDUP");
                            break;

                        case DEBUG:
							if (debug)
							{
								debug = false;
								Console.WriteLine("Debugging on (debug=0).");
							}
							else
							{
								debug = true;
								Console.WriteLine("Debugging on (debug=1).");
							}
                            break;

                        case DIR:
							if (debug)
								Console.WriteLine("---> LIST");

							// Use PORT command before LIST.
							if (!passive)
							{
								String arg = null;
								IPAddress[] addr = System.Net.Dns.GetHostAddresses(server);
								arg = addr[0].ToString().Replace(".", ",") + "," +
									(randomport / 256).ToString() + "," + (randomport % 256).ToString();
								Console.WriteLine("arg: " + arg);
								//String porta = (IPEndPoint)listener.LocalEndpoint.Port.ToString();
								mwriter.Write("PORT" + " " + arg + LINEEND);
								mwriter.Flush();

								ReadOutput(mreader);

								// Use the server and new port to create new TcpClient connection to transmit data.
								client = new TcpClient(server, randomport);
								dstream = client.GetStream();
								dreader = new StreamReader(dstream);

								//IPEndPoint endPoint = (IPEndPoint)connection.Client.RemoteEndPoint;
								//int port = endPoint.Port;
								//Console.WriteLine("port: " + port);
								//ReadOutput(reader); // No output

								//client = listener.AcceptTcpClient();

								randomport += 1;
							}
							else
							{
								mwriter.Write("PASV" + LINEEND);
								mwriter.Flush();

								while (true)
								{
									String output1 = mreader.ReadLine();
									Console.WriteLine(output1);

									// Parse the PASV output to get the port.
									string[] port = output1.Split(',');
									int p1 = Convert.ToInt32(port[port.Length - 2]);
									int p2 = Convert.ToInt32(port[port.Length - 1].TrimEnd(')'));
									int p3 = (p1 * 256) + p2;

									// Use the server and new port to create new TcpClient connection to transmit data.
									client = new TcpClient(server, p3);
									dstream = client.GetStream();
									dreader = new StreamReader(dstream);

									// Check for end of message.
									string[] outp = output1.Split(' ');
									if (!outp[0].EndsWith("-"))
										break;
								}
							}

							// Run LIST for both if/else.
							RunCommand(mwriter, mreader, "LIST");
							ReadOutput(dreader);
							ReadOutput(mreader);
                            break;

                        case GET:
							if (argv.Length != 2)
								Console.WriteLine("Usage: GET <filename>");
							else
							{
								if (!passive)
								{
									// Use PORT command before RETR command.
									String arg = null;
									IPAddress[] addr = System.Net.Dns.GetHostAddresses(server);
									arg = addr[0].ToString().Replace(".", ",") + "," +
										(randomport / 256).ToString() + "," + (randomport % 256).ToString();
									mwriter.Write("PORT" + " " + arg + LINEEND);
								}
								else
								{
									mwriter.Write("PASV" + LINEEND);
									mwriter.Flush();

									while (true)
									{
										String output1 = mreader.ReadLine();
										Console.WriteLine(output1);

										// Parse the PASV output to get the port.
										string[] port = output1.Split(',');
										int p1 = Convert.ToInt32(port[port.Length - 2]);
										int p2 = Convert.ToInt32(port[port.Length - 1].TrimEnd(')'));
										int p3 = (p1 * 256) + p2;

										// Use the server and new port to create new TcpClient connection to transmit data.
										client = new TcpClient(server, p3);
										dstream = client.GetStream();
										dreader = new StreamReader(dstream);

										// Check for end of message.
										string[] outp = output1.Split(' ');
										if (!outp[0].EndsWith("-"))
											break;
									}
								}

								// Run RETR for both if/else.
								mwriter.Write("RETR " + argv[1] + LINEEND);
								mwriter.Flush();
								//RunCommand(mwriter, mreader, "RETR", argv[1]);

								// Get the size of the file being transferred.
								String output2 = null;
								while (true)
								{
									output2 = mreader.ReadLine();
									Console.WriteLine(output2);

									// Check for end of message.
									string[] outp = output2.Split(' ');
									if (!outp[0].EndsWith("-"))
										break;
								}

								// Get the file size.
								string[] outarr = output2.Split(' ');
								filesize = Convert.ToInt32(outarr[outarr.Length - 2].TrimStart('('));

								ReadOutput(dreader);
								ReadOutput(mreader);
								dreader.ReadLine();

								strm = connection.GetStream();
								ReadFileTransfer(strm, filesize, argv[1]);
							}
								
                            break;

                        case HELP:
                            for (int i = 0; i < HELP_MESSAGE.Length; i++)
                            {
                                Console.WriteLine(HELP_MESSAGE[i]);
                            }
                            break;

                        case PASSIVE:
							if (passive)
							{
								passive = false;
								Console.WriteLine("Passive mode off.");
							}
							else
							{
								passive = true;
								Console.WriteLine("Passive mode on.");
							}
							//RunCommand(writer, reader, "PASV");
                            break;

                        case PUT:
							if (argv.Length != 2)
								Console.WriteLine("Usage: PUT <filename>");
							else
								RunCommand(mwriter, mreader, "APPE", argv[1]);
                            break;

                        case PWD:
							RunCommand(mwriter, mreader, "PWD");
                            break;

                        case QUIT:
							RunCommand(mwriter, mreader, "QUIT");
                            eof = true;
                            break;

                        case USER:
							if (argv.Length != 2)
								Console.WriteLine("Usage: USER <username>");
							else
								RunCommand(mwriter, mreader, "USER", argv[1]);
                            break;

                        default:
                            Console.WriteLine("Invalid command");
                            break;
                    }
                }
            } while (!eof);
        }

		/**
		 * RunCommand - Sends the given command and argument to the server 
		 * using the writer, and calls the print method.
		 * writer - the networkstream's stream writer that writes to the server.
		 * reader - the networkstream's stream reader that reads the server.
		 * command - the command to be understood by the server.
		 * arg - the argument associated with the command.
		 */
		public static void RunCommand(StreamWriter writer, StreamReader reader, 
			String command, String arg = null)
		{
			if (arg == null)
				writer.Write(command + LINEEND);
			else
				writer.Write(command + " " + arg + LINEEND);
			
			writer.Flush();
			ReadOutput(reader);
		}

		/**
		 * ReadOutput - Reads the messages from the server.
		 * reader - the networkstream's stream reader that reads the server.
		 **/
		public static void ReadOutput(StreamReader reader)
		{
			while (true)
			{
				String output = reader.ReadLine();
				Console.WriteLine(output);

				// Check for end of message.
				string[] outp = output.Split(' ');
				if (!outp[0].EndsWith("-"))
					break;
			}
		}

		public static void ReadFileTransfer(Stream stream, int size, String filename)
		{
			byte[] buffer = new byte[size];
			FileStream filestream = new FileStream(
				Environment.CurrentDirectory + "\\" + filename, 
				FileMode.Create, FileAccess.ReadWrite);

			// Keep reading until you read 0 bytes.
			while (stream.Read(buffer, 0, buffer.Length) != 0)
			{
				filestream.Write(buffer, 0, buffer.Length);
			}
			filestream.Close();
		}
    }
}