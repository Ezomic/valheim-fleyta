using System;
using UnityEngine;

namespace Fleyta
{
    /// <summary>
    /// The pusher's side: find the boat, decide whether and which way, pay, and hand the
    /// slide to whoever owns the hull.
    ///
    /// A Ship is moved by the machine that owns its ZDO. A force added from any other
    /// machine is overwritten by the owner's next physics sync, so a client shoving a boat
    /// someone else last touched would see it nudge and snap back. ClaimOwnership is not the
    /// answer either: it is not a lock, and taking a boat off a player who is rowing it in
    /// the next zone is a worse fault than the one being fixed. So the request travels as an
    /// RPC to the owner, who runs the slide with the direction and speed this side measured.
    /// That is why the mod needs to be on every machine and not only the host's.
    ///
    /// Direction and distance come from the terrain under the hull (Water.Find) and never
    /// from where the pusher stands.
    /// </summary>
    internal static class Shoving
    {
        internal const string RpcName = "Fleyta_Shove";

        private const float MaxAcceptedSpeed = 12f;

        /// <summary>
        /// The key press, and the console command's route. Answers with the reason in the
        /// message area when it does nothing, because a silent key is the worst thing a
        /// mod like this can be.
        /// </summary>
        internal static string TryShove(Player player)
        {
            var ship = Nearest(player);
            if (ship == null) return Refuse(player, "No boat within reach.");

            return Launch(player, ship);
        }

        internal static string Launch(Player player, Ship ship)
        {
            if (IsExcluded(ship)) return Refuse(player, "That boat is not one to shove.");
            if (player.GetStandingOnShip() != null || ship.HasPlayerOnboard())
                return Refuse(player, "Nobody can be aboard while it is shoved.");
            if (Fleyta.Shove.Active(ship)) return Refuse(player, "It is already moving.");

            var body = ship.GetComponent<Rigidbody>();
            var nview = ship.GetComponent<ZNetView>();
            if (body == null || nview == null || !nview.IsValid())
                return Refuse(player, "It will not budge.");

            var centre = body.worldCenterOfMass;

            if (Water.Afloat(centre)) return Refuse(player, "It is already afloat.");
            if (!Water.Aground(centre)) return Refuse(player, "The ground here has not loaded yet.");

            var plan = Water.Find(centre);
            if (!plan.Found)
                return Refuse(player, "The water is too far to shove it ("
                                      + FleytaConfig.MaxDistance.Value.ToString("0") + " m at most).");

            var cost = FleytaConfig.StaminaCost.Value;
            if (cost > 0f && !player.HaveStamina(cost)) return Refuse(player, "Not enough stamina.");

            var speed = SpeedFor(body.mass);
            var seconds = plan.Distance / speed * 2f + 3f;

            if (FleytaConfig.Verbose.Value)
                FleytaPlugin.Log.LogInfo("Shoving " + Utils.GetPrefabName(ship.gameObject)
                    + ": mass " + body.mass.ToString("0") + ", water " + plan.Distance.ToString("0")
                    + " m toward " + plan.Direction.ToString("0.00") + ", speed " + speed.ToString("0.00")
                    + " m/s, " + seconds.ToString("0.0") + " s allowed.");

            if (cost > 0f) player.UseStamina(cost);

            Send(ship, nview, plan.Direction, speed, seconds);

            Say(player, "You heave the boat toward the water (" + plan.Distance.ToString("0") + " m).");
            return "shoving " + plan.Distance.ToString("0") + " m";
        }

        /// <summary>
        /// Heavier is slower, and only by the square root, so the longship takes a few
        /// seconds longer than the karve and not a minute. Clamped both ways so a modded
        /// hull with an odd mass cannot stand still or fire across the beach.
        /// </summary>
        internal static float SpeedFor(float mass)
        {
            var scaled = FleytaConfig.BaseSpeed.Value
                         * Mathf.Sqrt(FleytaConfig.ReferenceMass.Value / Mathf.Max(mass, 1f));

            return Mathf.Clamp(scaled, FleytaConfig.MinSpeed.Value, FleytaConfig.MaxSpeed.Value);
        }

        internal static Ship Nearest(Player player)
        {
            Ship best = null;
            var bestDistance = FleytaConfig.Reach.Value;

            foreach (var instance in Ship.Instances)
            {
                var ship = instance as Ship;
                if (ship == null || ship.m_floatCollider == null) continue;

                var distance = Vector3.Distance(
                    ship.m_floatCollider.ClosestPoint(player.transform.position),
                    player.transform.position);

                if (distance > bestDistance) continue;

                bestDistance = distance;
                best = ship;
            }

            return best;
        }

        internal static bool IsExcluded(Ship ship)
        {
            var name = Utils.GetPrefabName(ship.gameObject);

            foreach (var entry in FleytaConfig.Excluded.Value.Split(','))
                if (string.Equals(entry.Trim(), name, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }

        private static void Send(Ship ship, ZNetView nview, Vector3 direction, float speed, float seconds)
        {
            if (nview.IsOwner())
            {
                Fleyta.Shove.Begin(ship, direction, speed, seconds);
                return;
            }

            var owner = nview.GetZDO().GetOwner();
            if (owner == 0L)
            {
                nview.ClaimOwnership();
                Fleyta.Shove.Begin(ship, direction, speed, seconds);
                return;
            }

            nview.InvokeRPC(owner, RpcName, direction, speed, seconds);
        }

        /// <summary>
        /// Runs on the owner, for a request from anywhere. The numbers are the sender's, so
        /// they are checked rather than trusted: a client can send anything down an RPC,
        /// and the cheapest harm here would be a boat fired across the map.
        /// </summary>
        internal static void OnRequest(Ship ship, Vector3 direction, float speed, float seconds)
        {
            if (!FleytaConfig.Enabled.Value || ship == null) return;
            if (!ship.IsOwner() || ship.HasPlayerOnboard() || IsExcluded(ship)) return;

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) return;
            if (float.IsNaN(speed) || speed <= 0f || speed > MaxAcceptedSpeed) return;
            if (float.IsNaN(seconds) || seconds <= 0f || seconds > 60f) return;

            Fleyta.Shove.Begin(ship, direction.normalized, speed, seconds);
        }

        private static string Refuse(Player player, string text)
        {
            Say(player, text);
            return text;
        }

        private static void Say(Player player, string text)
        {
            player.Message(MessageHud.MessageType.Center, text);
        }
    }
}
