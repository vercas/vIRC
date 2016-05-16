using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace vIRC.Utils
{
    /// <summary>
    /// Contains utilitary methods for assembling IRC messages.
    /// </summary>
    public static class MessageBuilder
    {
        /// <summary>
        /// PASS (password)
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public static IEnumerable<object> Pass(string password)
        {
            if (password == null)
                throw new ArgumentNullException("password");

            return new object[] { "PASS ", password, IrcClient.EndMessageBytes };
        }

        /// <summary>
        /// NICK (nick)
        /// </summary>
        /// <param name="nick"></param>
        /// <returns></returns>
        public static IEnumerable<object> Nick(string nick)
        {
            if (nick == null)
                throw new ArgumentNullException("nick");

            return new object[] { "NICK ", nick, IrcClient.EndMessageBytes };
        }

        /// <summary>
        /// USER (username) (mask) * :(real name)
        /// </summary>
        /// <param name="username"></param>
        /// <param name="realname"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        public static IEnumerable<object> User(string username, string realname, int mask = 0)
        {
            if (username == null)
                throw new ArgumentNullException("username");
            if (realname == null)
                throw new ArgumentNullException("realname");

            return new object[] { "USER ", username, " ", mask, " * :", realname, IrcClient.EndMessageBytes };
        }

        /// <summary>
        /// PING :(message)
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IEnumerable<object> Ping(string message)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            return new object[] { "PING :", message, IrcClient.EndMessageBytes };
        }

        /// <summary>
        /// PONG :(message)
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IEnumerable<object> Pong(string message)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            return new object[] { "PONG :", message, IrcClient.EndMessageBytes };
        }

        /// <summary>
        /// QUIT :(message)
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        public static IEnumerable<object> Quit(string reason)
        {
            if (reason == null)
                return new object[] { "QUIT\r\n" };
            else
                return new object[] { "QUIT :", reason, IrcClient.EndMessageBytes };
        }

        /// <summary>
        /// JOIN (channel)[ key]
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static IEnumerable<object> Join(string channel, string key)
        {
            if (channel == null)
                throw new ArgumentNullException("channel");

            if (key == null)
                return new object[] { "JOIN ", channel, IrcClient.EndMessageBytes };
            else
                return new object[] { "JOIN ", channel, " ", key, IrcClient.EndMessageBytes };
        }

        /// <summary>
        /// JOIN (channel1,channel2,channel3,...)[ key1,key2,key3,...]
        /// </summary>
        /// <param name="channels"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        public static IEnumerable<object> Join(IEnumerable<string> channels, IEnumerable<string> keys)
        {
            if (channels == null)
                throw new ArgumentNullException("channels");

            if (keys == null)
                return new object[] { "JOIN ", string.Join(",", channels), IrcClient.EndMessageBytes };
            else
                return new object[] { "JOIN ", string.Join(",", channels), " ", string.Join(",", keys), IrcClient.EndMessageBytes };
        }

        /// <summary>
        /// PART (channel)[ :reason]
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public static IEnumerable<object> Part(string channel, string reason)
        {
            if (channel == null)
                throw new ArgumentNullException("channel");

            if (reason == null)
                return new object[] { "PART ", channel, IrcClient.EndMessageBytes };
            else
                return new object[] { "PART ", channel, " :", reason, IrcClient.EndMessageBytes };
        }

        /// <summary>
        /// PART (channel1,channel2,channel3,...)[ :reason]
        /// </summary>
        /// <param name="channels"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public static IEnumerable<object> Part(IEnumerable<string> channels, string reason)
        {
            if (channels == null)
                throw new ArgumentNullException("channels");

            if (reason == null)
                return new object[] { "PART ", string.Join(",", channels), IrcClient.EndMessageBytes };
            else
                return new object[] { "PART ", string.Join(",", channels), " :", reason, IrcClient.EndMessageBytes };
        }

        /// <summary>
        /// PRIVMSG (target) :(message)
        /// </summary>
        /// <param name="target"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IEnumerable<object> Privmsg(string target, string message)
        {
            if (target == null)
                throw new ArgumentNullException("target");
            if (message == null)
                throw new ArgumentNullException("message");

            return new object[] { "PRIVMSG ", target, " :", message, IrcClient.EndMessageBytes };
        }

        /// <summary>
        /// PRIVMSG (target1,target2,target3,...) :(message)
        /// </summary>
        /// <param name="targets"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IEnumerable<object> Privmsg(IEnumerable<string> targets, string message)
        {
            if (targets == null)
                throw new ArgumentNullException("targets");
            if (message == null)
                throw new ArgumentNullException("message");

            return new object[] { "PRIVMSG ", string.Join(",", targets), " :", message, IrcClient.EndMessageBytes };
        }

        /// <summary>
        /// NOTICE (target) :(message)
        /// </summary>
        /// <param name="target"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IEnumerable<object> Notice(string target, string message)
        {
            if (target == null)
                throw new ArgumentNullException("target");
            if (message == null)
                throw new ArgumentNullException("message");

            return new object[] { "NOTICE ", target, " :", message, IrcClient.EndMessageBytes };
        }

        /// <summary>
        /// NOTICE (target1,target2,target3,...) :(message)
        /// </summary>
        /// <param name="targets"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IEnumerable<object> Notice(IEnumerable<string> targets, string message)
        {
            if (targets == null)
                throw new ArgumentNullException("targets");
            if (message == null)
                throw new ArgumentNullException("message");

            return new object[] { "NOTICE ", string.Join(",", targets), " :", message, IrcClient.EndMessageBytes };
        }
    }
}
