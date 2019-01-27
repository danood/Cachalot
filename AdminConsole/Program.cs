using System;
using System.Linq;
using AdminConsole.AutoCompleteUtils;
using AdminConsole.ConsoleUtils;
using Channel;
using Client.Core;
using Client.Interface;
using CommandLineParser = AdminConsole.Commands.CommandLineParser;
using CommandType = AdminConsole.Commands.CommandType;
using ConsoleLogger = AdminConsole.Commands.ConsoleLogger;


namespace AdminConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            // by default conect to the local server
            string server = "localhost";
            if (args.Length > 0)
                server = args[0];


            // default port
            int port = 4848;
            if (args.Length > 1)
            {
                if (int.TryParse(args[1], out var customPort))
                {
                    port = customPort;
                }
            }

           

            
            var channel = new TcpClientChannel(new TcpClientPool(1, 1, server, port));
            ICacheClient client = new CacheClient{Channel = channel};
            
            
            Logger.CommandLogger = new ConsoleLogger();

            Logger.Write("connecting to server {0} on port {1}", server, port);

            try
            {
                ClusterInformation serverDesc = null;

                try
                {
                    serverDesc = client.GetClusterInformation();
                }
                catch (Exception )
                {
                    Logger.WriteEror("Not connected to server: Only CONNECT and HELP commands are available");
                   
                }
                

                //Profiler.Logger = new ProfileOutput(Console.Out);
                CommandLineParser parser = new CommandLineParser(serverDesc);
                Logger.Write("Type HELP for command list. Advanced autocompletion is also available");

                ConsoleExt.SetLine(">>");

                var running = true;
                var cyclingAutoComplete = new CyclingAutoComplete{KnownTypes = serverDesc?.Schema.ToList()};
                while (running)
                {
                    var result = ConsoleExt.ReadKey();
                    switch (result.Key)
                    {
                        case ConsoleKey.Enter:
                            var line = result.LineBeforeKeyPress.Line;
                            line = line.TrimStart('>');
                            var cmd = parser.Parse(line);
                            if (cmd.Success )
                            {
                                if (cmd.CmdType != CommandType.Exit)
                                {

                                    var title = Console.Title;
                                    Console.Title = " WORKING...";
                                    client = cmd.TryExecute(client);
                                    Console.Title = title;

                                    // table definitions may have changed by command execution (connect, import, recreate) 
                                    // or by external action

                                    try
                                    {
                                        serverDesc = client.GetClusterInformation();
                                        // if the connection changed reinitialize the autocomplete with the new schema
                                        cyclingAutoComplete = new CyclingAutoComplete { KnownTypes = serverDesc?.Schema.ToList() };
                                        parser = new CommandLineParser(serverDesc);
                                    }
                                    catch (Exception)
                                    {
                                        Logger.WriteEror("Not connected to server: Only CONNECT and HELP commands are available");


                                    }

                                }
                                else
                                {
                                    running = false;
                                }
                                
                            }
                            else
                            {
                                Logger.WriteEror("invalid command");
                            }

                            ConsoleExt.SetLine(">>");

                            break;
                        case ConsoleKey.Tab:
                            var shiftPressed = (result.Modifiers & ConsoleModifiers.Shift) != 0;
                            var cyclingDirection = shiftPressed ? CyclingDirections.Backward : CyclingDirections.Forward;
                            line = result.LineBeforeKeyPress.LineBeforeCursor.TrimStart('>');
                            
                            var autoCompletedLine =
                                cyclingAutoComplete.AutoComplete(line, cyclingDirection);

                            ConsoleExt.SetLine(">>" + autoCompletedLine);
                            break;
                    }
                }

                Logger.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}