using HarmonyLib;
using UnityEngine;

namespace Fleyta
{
    /// <summary>
    /// `fleyta shove` and `fleyta state` in the console.
    ///
    /// They exist for a Devkit scenario, which cannot press a key. `shove` takes the same
    /// route the key takes, so a scenario tests the real path and not a copy of it. `state`
    /// prints "afloat", "aground" or "unknown" for the nearest boat, which is the only way a
    /// scenario can read where a boat ended up.
    ///
    /// isCheat false, honestly: it does exactly what the key does, costs the same stamina and
    /// obeys the same limits. A cheat command would need devcommands and flag the character.
    /// </summary>
    internal static class DevConsole
    {
        // Process-wide: Terminal's command table is a private static nothing clears, so a
        // second registration would leave a duplicate behind.
        private static bool _registered;

        [HarmonyPatch(typeof(Terminal), "InitTerminal")]
        internal static class Hook
        {
            private static void Postfix()
            {
                if (_registered) return;
                _registered = true;

                new Terminal.ConsoleCommand("fleyta",
                    "shove | state - shove the nearest boat as the key does, or say whether it is afloat",
                    OnCommand, isCheat: false);
            }
        }

        private static void OnCommand(Terminal.ConsoleEventArgs args)
        {
            var term = args.Context;
            if (term == null) return;

            var player = Player.m_localPlayer;
            if (player == null) { term.AddString("fleyta: no player."); return; }

            var verb = args.Length > 1 ? args[1].ToLowerInvariant() : "";

            if (verb == "shove")
            {
                term.AddString("fleyta: " + Shoving.TryShove(player));
                return;
            }

            if (verb == "state")
            {
                var ship = Shoving.Nearest(player);
                if (ship == null) { term.AddString("fleyta: no boat within reach"); return; }

                var centre = ship.GetComponent<Rigidbody>().worldCenterOfMass;
                var state = Water.Afloat(centre) ? "afloat" : Water.Aground(centre) ? "aground" : "unknown";
                var moving = Shove.Active(ship) ? ", moving" : "";

                term.AddString("fleyta: " + Utils.GetPrefabName(ship.gameObject) + " is " + state + moving);
                return;
            }

            term.AddString("fleyta shove - shove the nearest boat toward the water");
            term.AddString("fleyta state - afloat, aground or unknown for the nearest boat");
        }
    }
}
