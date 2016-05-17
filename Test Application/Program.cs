using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using vIRC;

namespace Test_Application
{
    class Program
    {
        static IrcClient cl = new IrcClient();
        static IrcClientIdentification id = new IrcClientIdentification()
        {
            Nickname = "vIRC_N",
            Username = "vIRC_U",
            RealName = "Vercas's IRC library",
            UseSasl = true,
            NickServUsername = "vIRC",
            NickServPassword = "ratatatatatatata",
        };

        static Uri server = new Uri("irc://vps229649.ovh.net:6667");

        static void Main(string[] args)
        {
            cl.ConnectAsync(server, id).Wait();

            if (cl.LocalUser == null)
                Console.WriteLine("LOCAL USER IS NULL!");
            else
            {
                Console.WriteLine("Local user data:\n\tNickname: {0}\n\tUsername: {1}\n\tReal Name: {2}\n\tHost: {3}"
                    , cl.LocalUser.Nickname, cl.LocalUser.Username ?? "NULL", cl.LocalUser.RealName ?? "NULL", cl.LocalUser.Hostname ?? "NULL");

                Console.WriteLine("\tModes: {0}", string.Concat(cl.LocalUser.Modes.Select(c => c.ToString())));
            }

            Console.WriteLine("Server info:\n\tName: {0}\n\tVersion: {1}\n\tPrefixes & Modes: {2}"
                , cl.ServerInformation.Name, cl.ServerInformation.Version
                , string.Join(" ", cl.ServerInformation.PrefixModes.Select(kv => kv.Key.ToString() + kv.Value)));
            
            string input = null;
            string quitReason = null;
            string[] split;
            IrcChannel testChan = null;

            cl.ChannelMessageReceived += (s, e) =>
            {
                Console.WriteLine("{0} | {1}: {2} [{3}]", e.ChannelUser.Channel.Name, e.ChannelUser.User.Nickname, e.Message, e.MessageType);
            };

            cl.UserMessageReceived += (s, e) =>
            {
                Console.WriteLine("{0} : {1} [{2}]", e.User.Nickname, e.Message, e.MessageType);
            };

            do
            {
                //Console.Write("> ");
                input = Console.ReadLine();
                split = input.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                switch (split[0])
                {
                case "quit":
                    if (split.Length > 1)
                        quitReason = split[1];

                    break;

                case "testchanmsg":
                    var t3 = testChan.SendMessageAsync(input.Substring("testchanmsg".Length + 1));
                    t3.Wait();
                    break;

                case "msg":
                    var t4 = cl.SendMessageAsync(split[1], input.Substring("msg".Length + 2 + split[1].Length));
                    t4.Wait();
                    break;

                case "nick":
                    var t5 = cl.ChangeNicknameAsync(split[1]);
                    t5.Wait();
                    Console.WriteLine(t5.Result);
                    break;

                case "away":
                    var t6 = cl.SetAwayAsync(split.Length > 1 ? split[1] : null);
                    t6.Wait();
                    break;

                case "part":
                    var t1 = testChan.PartAsync();
                    t1.Wait();
                    break;

                case "join":
                    var t2 = cl.JoinAsync(split[1]);
                    t2.Wait();
                    testChan = t2.Result;

                    goto case "channel";

                case "channel":
                    Console.WriteLine("Test channel data:\n\tName: {0}\n\tTopic: {1}\n\tModes: {2}\n\tJoined: {3}"
                        , testChan.Name, testChan.Topic ?? "NULL"
                        , string.Concat(testChan.Modes.Select(c => c.ToString()))
                        , testChan.Joined);

                    goto case "names";

                case "names":
                    Console.WriteLine("Test channel users: ({0})", testChan.Users.Count());

                    foreach (var cu in testChan.Users)
                        Console.WriteLine("\tNickname: {0}; Modes: {1}"
                            , cu.User.Nickname
                            , string.Concat(cu.Modes.Select(c => c.ToString())));
                    break;
                }
            } while (split[0] != "quit");

            var tx = cl.QuitAsync(quitReason);

            if (!tx.Wait(5000))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.BackgroundColor = ConsoleColor.White;

                Console.WriteLine("Quitting timed out!");
            }
        }
    }
}
