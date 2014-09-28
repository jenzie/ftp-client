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
			NetworkStream stream = null;
			StreamReader reader = null;
			StreamWriter writer = null;

            // Handle the command line.

			if (args.Length == 1)
			{
				server = args[0];
				try
				{
					// Try to connect to the given server and port.
					connection = new TcpClient(server, PORT);
					stream = connection.GetStream();
					reader = new StreamReader(stream);
					writer = new StreamWriter(stream);

					// Display the welcome text.
					ReadOutput(reader);
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
			writer.Write("USER " + user.Trim() + LINEEND);
			writer.Flush();
			ReadOutput(reader);

			// Have the user log in with the password.
			Console.Write("Password: ");
			String password = Console.ReadLine();
			writer.Write("PASS " + password.Trim() + LINEEND);
			writer.Flush();
			ReadOutput(reader);

			// Display the system type.
			writer.Write("SYST" + LINEEND);
			writer.Flush();
			String output = reader.ReadLine();
			Console.WriteLine("Remote system type is " + output.Split(' ')[1]);

			// Display the default file transfer mode.
			writer.Write("TYPE I" + LINEEND);
			writer.Flush();
			reader.ReadLine();
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
							RunCommand(writer, reader, "TYPE A");
                            break;

                        case BINARY:
							if (debug)
								Console.WriteLine("---> TYPE I");
							RunCommand(writer, reader, "TYPE I");
                            break;

                        case CD:
							if (argv.Length != 2)
								Console.WriteLine("Usage: CD <path>");
							else
								RunCommand(writer, reader, "CWD", argv[1]);
                            break;

                        case CDUP:
							RunCommand(writer, reader, "CDUP");
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
							// Use PORT command before LIST.
							if (!passive)
							{
								writer.Write("PORT" + LINEEND);
								ReadOutput(reader);
							}

							// Run LIST for both if/else.
							RunCommand(writer, reader, "LIST");
                            break;

                        case GET:
							if (argv.Length != 2)
								Console.WriteLine("Usage: GET <filename>");
							else
								RunCommand(writer, reader, "RETR", argv[1]);
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
								Console.WriteLine("Passive mode on.");
							}
							else
							{
								passive = true;
								Console.WriteLine("Passive mode off.");
							}
							RunCommand(writer, reader, "PASV");
                            break;

                        case PUT:
							if (argv.Length != 2)
								Console.WriteLine("Usage: PUT <filename>");
							else
								RunCommand(writer, reader, "APPE", argv[1]);
                            break;

                        case PWD:
							RunCommand(writer, reader, "PWD");
                            break;

                        case QUIT:
							RunCommand(writer, reader, "QUIT");
                            eof = true;
                            break;

                        case USER:
							if (argv.Length != 2)
								Console.WriteLine("Usage: USER <username>");
							else
								RunCommand(writer, reader, "USER", argv[1]);
                            break;

                        default:
                            Console.WriteLine("Invalid command");
                            break;
                    }
                }
            } while (!eof);
        }

		public static void RunCommand(StreamWriter writer, StreamReader reader, 
			String command, String arg1 = null)
		{
			if (arg1 == null)
				writer.Write(command + LINEEND);
			else
				writer.Write(command + " " + arg1 + LINEEND);
			
			writer.Flush();
			ReadOutput(reader);
		}

		public static void ReadOutput(StreamReader reader, bool print = true)
		{
			while (true)
			{
				String output = reader.ReadLine();
				if (print)
					Console.WriteLine(output);

				// Check for end of message.
				string[] outp = output.Split(' ');
				if (!outp[0].EndsWith("-"))
					break;
			}
		}
    }
}