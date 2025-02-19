﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Windows;

using Microsoft.Win32;

using Bloxstrap.Enums;

namespace Bloxstrap
{
    static class ProtocolHandler
    {
        // map uri keys to command line args
        private static readonly IReadOnlyDictionary<string, string> UriKeyArgMap = new Dictionary<string, string>()
        {
            // excluding roblox-player and launchtime
            { "launchmode", "--" },
            { "gameinfo", "-t " },
            { "placelauncherurl", "-j "},
            { "launchtime", "--launchtime=" },
            { "browsertrackerid", "-b " },
            { "robloxLocale", "--rloc " },
            { "gameLocale", "--gloc " },
            { "channel", "-channel " }
        };

        public static string ParseUri(string protocol)
        {
            string[] keyvalPair;
            string key;
            string val;
            StringBuilder commandLine = new();

            foreach (var parameter in protocol.Split('+'))
            {
                if (!parameter.Contains(':'))
                    continue;

                keyvalPair = parameter.Split(':');
                key = keyvalPair[0];
                val = keyvalPair[1];

                if (!UriKeyArgMap.ContainsKey(key) || string.IsNullOrEmpty(val))
                    continue;

                if (key == "launchmode" && val == "play")
                    val = "app";

                if (key == "placelauncherurl")
                    val = HttpUtility.UrlDecode(val);

                // we'll set this before launching because for some reason roblox just refuses to launch if its like a few minutes old so ???
                if (key == "launchtime")
                    val = "LAUNCHTIMEPLACEHOLDER";

                if (key == "channel")
                {
                    if (val.ToLower() != App.Settings.Prop.Channel.ToLower() && App.Settings.Prop.ChannelChangeMode != ChannelChangeMode.Ignore)
                    {
                        MessageBoxResult result = App.Settings.Prop.ChannelChangeMode == ChannelChangeMode.Automatic ? MessageBoxResult.Yes : App.ShowMessageBox(
                            $"{App.ProjectName} was launched with the Roblox build channel set to {val}, however your current preferred channel is {App.Settings.Prop.Channel}.\n\n" +
                            $"Would you like to switch channels from {App.Settings.Prop.Channel} to {val}?",
                            MessageBoxImage.Question,
                            MessageBoxButton.YesNo
                        );

                        if (result == MessageBoxResult.Yes)
                        {
                            App.Logger.WriteLine($"[Protocol::ParseUri] Changed Roblox build channel from {App.Settings.Prop.Channel} to {val}");
                            App.Settings.Prop.Channel = val;
                        }
                    }

                    // we'll set the arg when launching
                    continue;
                }

                commandLine.Append(UriKeyArgMap[key] + val + " ");
            }

            return commandLine.ToString();
        }

        public static void Register(string key, string name, string handler)
        {
            string handlerArgs = $"\"{handler}\" %1";
            RegistryKey uriKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{key}");
            RegistryKey uriIconKey = uriKey.CreateSubKey("DefaultIcon");
            RegistryKey uriCommandKey = uriKey.CreateSubKey(@"shell\open\command");

            if (uriKey.GetValue("") is null)
            {
                uriKey.SetValue("", $"URL: {name} Protocol");
                uriKey.SetValue("URL Protocol", "");
            }

            if ((string?)uriCommandKey.GetValue("") != handlerArgs)
            {
                uriIconKey.SetValue("", handler);
                uriCommandKey.SetValue("", handlerArgs);
            }

            uriKey.Close();
            uriIconKey.Close();
            uriCommandKey.Close();
        }

        public static void Unregister(string key)
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{key}");
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine($"[Protocol::Unregister] Failed to unregister {key}: {ex}");
            }
        }
    }
}
