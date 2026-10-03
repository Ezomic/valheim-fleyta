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
    }
}
