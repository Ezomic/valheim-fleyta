using BepInEx.Configuration;
using UnityEngine;

namespace Fleyta
{
    /// <summary>
    /// Everything tunable. The .cfg is the document a player reads instead of the README, so
    /// the comments carry the reasoning and the consequences, including the ones that look
    /// like a bug.
    ///
    /// BepInEx writes every entry on first run and the saved value beats a new default in
    /// code: changing a default here does nothing on a machine that has already run the
    /// plugin. Edit <c>ezomic.valheim.fleyta.cfg</c> in the same change.
    /// </summary>
    internal static class FleytaConfig
    {
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<KeyCode> ShoveKey;
        internal static ConfigEntry<float> StaminaCost;
        internal static ConfigEntry<float> Reach;
        internal static ConfigEntry<float> MaxDistance;
        internal static ConfigEntry<float> FloatDepth;
        internal static ConfigEntry<float> MaxRise;
        internal static ConfigEntry<float> BaseSpeed;
        internal static ConfigEntry<float> ReferenceMass;
        internal static ConfigEntry<float> MinSpeed;
        internal static ConfigEntry<float> MaxSpeed;
        internal static ConfigEntry<float> Acceleration;
        internal static ConfigEntry<string> Excluded;
        internal static ConfigEntry<bool> Verbose;

        internal static void Bind(ConfigFile cfg)
        {
            Enabled = cfg.Bind("Fleyta", "Enabled", true,
                "Off leaves the plugin loaded and the key doing nothing.");

            // A key and not hold-E. A boat hull has nothing to hover over: the only thing
            // the game lets you look at on a ship is the helm, and E there already means
            // "take the wheel". Hold-E would also fight the tap that boards. A key works
            // from anywhere beside the hull, and the helm's hover text names it.
            ShoveKey = cfg.Bind("Fleyta", "ShoveKey", KeyCode.B,
                "Stand beside a boat that is aground and press this to shove it toward the "
                + "nearest water. Yours alone: the host never rebinds it.");

            StaminaCost = cfg.Bind("Fleyta", "StaminaCost", 20f,
                "Stamina spent on each shove, taken when the shove starts (when the boat's owner accepts it) and not refunded if "
                + "it jams on a rock. One press is the whole job, so this is a flat price and "
                + "not a drain: a longship costs the same to start as a karve, it just moves "
                + "slower. 0 makes it free.");

            Reach = cfg.Bind("Fleyta", "Reach", 4f,
                "How close you must stand, in metres, measured to the nearest point of the "
                + "hull rather than its middle, so a longship does not need to be reached "
                + "from the helm.");

            MaxDistance = cfg.Bind("Fleyta", "MaxDistance", 30f,
                "The furthest the water can be, in metres along the ground. Past this the "
                + "boat is not moved at all and you are told so, because a boat left halfway "
                + "up a beach is worse than one that stayed put.");

            FloatDepth = cfg.Bind("Fleyta", "FloatDepth", 2f,
                "How far below the surface the sea floor must be under the middle of the hull "
                + "before the boat counts as afloat, in metres. Too shallow and the keel "
                + "grounds on the bottom in water that looks deep enough; too deep and the "
                + "push runs further than it needs to. Raise it first if a boat is left "
                + "stuck in the shallows. The vanilla hulls were not measured when this was "
                + "written, so this is the number to tune in game.");

            MaxRise = cfg.Bind("Fleyta", "MaxRise", 1.5f,
                "How much higher than where it sits the ground may climb on the way to the "
                + "water, in metres, before that direction counts as blocked. A boat slides "
                + "down a slope; it is not hauled over a dune. Raising it lets a shove cross "
                + "low humps and makes the wrong side of a ridge a candidate.");

            BaseSpeed = cfg.Bind("Fleyta", "BaseSpeed", 2f,
                "Sliding speed in metres a second for a hull of ReferenceMass.");

            ReferenceMass = cfg.Bind("Fleyta", "ReferenceMass", 1000f,
                "The hull mass that slides at exactly BaseSpeed. Speed scales with the square "
                + "root of ReferenceMass over the hull's own mass, so a heavy longship is "
                + "slower and a light karve quicker, without a longship taking a minute. "
                + "Turn Verbose on to read the real masses in the log.");

            MinSpeed = cfg.Bind("Fleyta", "MinSpeed", 0.5f,
                "Slowest a shove ever goes, so the heaviest hull still arrives in reasonable "
                + "time.");

            MaxSpeed = cfg.Bind("Fleyta", "MaxSpeed", 4f,
                "Fastest a shove ever goes. A light hull at speed would clear the shoreline "
                + "and keep going; this is the brake on that.");

            Acceleration = cfg.Bind("Fleyta", "Acceleration", 10f,
                "How hard the push works against ground friction, in metres a second squared. "
                + "Friction on a dragged hull is around 6, so below that the boat does not "
                + "move at all; the default clears it with room. It does not change the "
                + "speed, only how quickly it is reached.");

            Excluded = cfg.Bind("Fleyta", "Excluded", "Raft",
                "Prefab names, comma separated, that are never shoved. The raft is left out "
                + "by decision: it is the starter boat, and the gesture is meant for the "
                + "Karve and the longships.");

            // Personal by intent, see the plugin: a host turning on someone else's logging
            // is not something anybody asked for.
            Verbose = cfg.Bind("Fleyta", "Verbose", false,
                "Write what was measured and decided to BepInEx/LogOutput.log: the hull's "
                + "mass, the ground under it, the water found, the speed chosen and why a "
                + "shove stopped. Off unless something looks wrong.");
        }
    }
}
