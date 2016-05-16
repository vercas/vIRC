using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vIRC.Utils
{
    /// <summary>
    /// Contains methods for dealing with RPL_ISUPPORT messages.
    /// </summary>
    internal static class RPL_ISUPPORT
    {
        internal static readonly string NumericCommand = "005";

        internal delegate Task ParameterHandler(IrcClient cl, string param);

        internal static Dictionary<string, ParameterHandler> handlers = new Dictionary<string, ParameterHandler>(StringComparer.OrdinalIgnoreCase)
        {
            { "CASEMAPPING", CasemappingHandler },

            { "MAXCHANNELS", MaxchannelsHandler },
            { "CHANLIMIT", ChanlimitHandler },

            { "NICKLEN", NicklenHandler },
            { "CHANNELLEN", ChannellenHandler },
            { "TOPICLEN", TopiclenHandler },
            { "KICKLEN", KicklenHandler },

            { "MAXTARGETS", MaxtargetsHandler },
            { "TARGMAX", TargmaxHandler },

            { "PREFIX", PrefixHandler },
            { "CHANMODES", ChanmodesHandler },
            { "STATUSMSG", StatusmsgHandler },

            { "WHOX", WhoxHandler },
        };

        internal static async Task Handler(IrcClient cl, Prefix source, List<string> args)
        {
            for (int i = 1; i < args.Count - 1; ++i)
            {
                string param = args[i];
                int eqInd = param.IndexOf('=');
                ParameterHandler han = null;

                if (eqInd > 0)
                {
                    if (!handlers.TryGetValue(param.Substring(0, eqInd), out han))
                    {
                        Trace.WriteLine(string.Format("\tUnhandled RPL_ISUPPORT parameter: {0}", param));

                        continue;
                    }
                }
                else
                    if (!handlers.TryGetValue(param, out han))
                    {
                        Trace.WriteLine(string.Format("\tUnhandled RPL_ISUPPORT parameter: {0}", param));

                        continue;
                    }

                //  Reaching this point means there is a handler found for this parameter.

                await han(cl, param);
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        internal static async Task CasemappingHandler(IrcClient cl, string param)
        {
            if (param == "CASEMAPPING=ascii")
                cl.normalizer = new CaseMappings.Ascii();
        }

        internal static async Task MaxchannelsHandler(IrcClient cl, string param)
        {
            cl.ServerInformation.MaxChannels = int.Parse(param.Substring("MAXCHANNELS=".Length), System.Globalization.CultureInfo.InvariantCulture);
        }

        internal static async Task ChanlimitHandler(IrcClient cl, string param)
        {
            cl.ServerInformation.chanLims.AddRange(param.SplitEx(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries, "CHANLIMIT=".Length).Select(s => new ChannelLimit(s)));
        }

        internal static async Task NicklenHandler(IrcClient cl, string param)
        {
            cl.ServerInformation.NickLength = int.Parse(param.Substring("NICKLEN=".Length), System.Globalization.CultureInfo.InvariantCulture);
        }

        internal static async Task ChannellenHandler(IrcClient cl, string param)
        {
            cl.ServerInformation.ChannelLength = int.Parse(param.Substring("CHANNELLEN=".Length), System.Globalization.CultureInfo.InvariantCulture);
        }

        internal static async Task TopiclenHandler(IrcClient cl, string param)
        {
            cl.ServerInformation.TopicLength = int.Parse(param.Substring("TOPICLEN=".Length), System.Globalization.CultureInfo.InvariantCulture);
        }

        internal static async Task KicklenHandler(IrcClient cl, string param)
        {
            cl.ServerInformation.KickLength = int.Parse(param.Substring("KICKLEN=".Length), System.Globalization.CultureInfo.InvariantCulture);
        }

        internal static async Task MaxtargetsHandler(IrcClient cl, string param)
        {
            cl.ServerInformation.MaxTargets = int.Parse(param.Substring("MAXTARGETS=".Length), System.Globalization.CultureInfo.InvariantCulture);
        }

        internal static async Task TargmaxHandler(IrcClient cl, string param)
        {
            cl.ServerInformation.cmdTrgLims.AddRange(param.SplitEx(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries, "TARGMAX=".Length).Select(s => new CommandTargetLimit(s)));
        }

        internal static async Task PrefixHandler(IrcClient cl, string param)
        {
            if (param.Length < "PREFIX=(a)b=".Length)
            {
                //  No (null) value means no prefixes are supported.

                cl.ServerInformation.ChannelUserPrefixes = null;
                cl.ServerInformation.prefixModes.Clear();
            }

            int parenInd = param.IndexOf(')');

            System.Diagnostics.Debug.Assert(parenInd >= "PREFIX=(a".Length);

            cl.ServerInformation.ChannelUserPrefixes = param.Substring(parenInd + 1);

            for (int i = "PREFIX=(".Length, j = parenInd + 1; i < parenInd; ++i, ++j)
            {
                cl.ServerInformation.channelModes[param[i]].Type = ChannelModeTypes.Prefix;
                cl.ServerInformation.prefixModes[param[j]] = param[i];
            }
        }

        internal static async Task ChanmodesHandler(IrcClient cl, string param)
        {
            var types = param.Substring("CHANMODES=".Length).Split(',');

            System.Diagnostics.Debug.Assert(types.Length == 4);

            for (int t = 0; t < types.Length; ++t)
                for (int i = 0; i < types[t].Length; ++i)
                {
                    var spec = cl.ServerInformation.channelModes[types[t][i]];

                    if (spec.Type == ChannelModeTypes.Unknown)
                        switch(t)
                        {
                        case 0: spec.Type = ChannelModeTypes.A; break;
                        case 1: spec.Type = ChannelModeTypes.B; break;
                        case 2: spec.Type = ChannelModeTypes.C; break;
                        case 3: spec.Type = ChannelModeTypes.D; break;
                        }
                }
        }

        internal static async Task StatusmsgHandler(IrcClient cl, string param)
        {
            cl.ServerInformation.NoticePrefixes = param.Substring("STATUSMSG=".Length);
        }

        internal static async Task WhoxHandler(IrcClient cl, string param)
        {
            cl.ServerInformation.UsesWhox = true;
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
