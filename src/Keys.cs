using System;
using HarmonyLib;
using UnityEngine;

namespace Fleyta
{
    /// <summary>
    /// Reading the configured key the way the game reads one. Valheim runs on the new Input
    /// System, where UnityEngine.Input does not see every button, and the failure is the
    /// worst kind: the key is bound, the cfg is right and nothing happens. ZInput routes a
    /// KeyCode to keyboard, mouse or pad itself.
    ///
    /// A keystroke that belongs to chat, the console or a text field is not ours.
    /// </summary>
    internal static class Keys
    {
        internal static bool Pressed(KeyCode key)
        {
            if (key == KeyCode.None) return false;

            // logWarning: false, or ZInput grumbles about every KeyCode it cannot map.
            if (!ZInput.GetKeyDown(key, false)) return false;

            if (Chat.instance != null && Chat.instance.HasFocus()) return false;
            return !Console.IsVisible() && !TextInput.IsVisible();
        }

        private static Func<Player, bool> _takeInput;
        private static bool _takeInputBound;

        /// <summary>
        /// Player.TakeInput is the game's one answer to "is this player free to act": false
        /// in a cutscene, while a station or the map is open, and whenever another mod's
        /// window has claimed input through its own patch of it. It is protected, so it is
        /// reached by reflection, bound on first use inside a try so that a rename costs this
        /// check and never the plugin. Unbound, the key is allowed, as before.
        /// </summary>
        internal static bool PlayerTakesInput(Player player)
        {
            if (!_takeInputBound)
            {
                _takeInputBound = true;
                try
                {
                    var method = AccessTools.Method(typeof(Player), "TakeInput");
                    if (method != null)
                        _takeInput = (Func<Player, bool>)Delegate.CreateDelegate(typeof(Func<Player, bool>), method);
                }
                catch (Exception e)
                {
                    FleytaPlugin.Log.LogWarning("Player.TakeInput could not be bound: " + e.Message);
                }
            }

            return _takeInput == null || _takeInput(player);
        }
    }
}
