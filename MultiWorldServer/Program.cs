using System;
using System.Diagnostics.Eventing.Reader;
using System.Threading;

using RandomizerLib;
using RandomizerLib.Logging;

namespace MultiWorldServer
{
    internal class Program
    {
        private static Server Serv;

        private static void Main()
        {
            LogHelper.LogTarget = new Logger();
            LogicManager.ParseXML();

            Serv = new Server(38281);
            while (Serv.Running)
            {
                string input = Console.ReadLine();

                try
                {
                    string[] commands = input.ToLower().Split(' ');
                    switch (commands[0])
                    {
                        case "give":
                            if (commands.Length != 4)
                            {
                                Console.WriteLine("Usage: give <item> <session id> <player id>");
                                break;
                            }
                            Serv.GiveItem(commands[1], Int32.Parse(commands[2]), Int32.Parse(commands[3]) - 1);
                            break;
                        case "ready":
                            Serv.ListReady();
                            break;
                        case "list":
                            Serv.ListSessions();
                            break;
                        default:
                            Console.WriteLine($"Unrecognized command: '{commands[0]}'");
                            break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error processing command: {e.Message}");
                }

                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write("> ");
            }
        }
    }
}
