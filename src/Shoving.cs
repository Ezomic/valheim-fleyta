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

        internal const string ReplyName = "Fleyta_Shoved";

        private const float PusherSlack = 3f;

        private static string _pendingNote;

        /// <summary>
        /// The longest a shove is ever allowed to run, from the same config on both sides:
        /// the slowest speed over the furthest water, doubled, plus three seconds. The pusher
        /// never asks for more than this by construction and the owner clamps to it, so the
        /// two ends cannot disagree about what a legal request is.
        /// </summary>
        internal static float MaxSeconds()
        {
            return FleytaConfig.MaxDistance.Value / Mathf.Max(FleytaConfig.MinSpeed.Value, 0.05f) * 2f + 3f;
        }

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
            if (!Water.Shallow(centre)) return Refuse(player, "The ground here has not loaded yet.");
            if (!Water.Resting(ship, centre)) return Refuse(player, "It is floating, not aground.");

            var plan = Water.Find(centre);
            if (!plan.Found)
                return Refuse(player, "The water is too far to shove it ("
                                      + FleytaConfig.MaxDistance.Value.ToString("0") + " m at most).");

            var cost = FleytaConfig.StaminaCost.Value;
            if (cost > 0f && !player.HaveStamina(cost)) return Refuse(player, "Not enough stamina.");

            var speed = SpeedFor(body.mass);
            var seconds = Mathf.Min(plan.Distance / speed * 2f + 3f, MaxSeconds());

            if (FleytaConfig.Verbose.Value)
                FleytaPlugin.Log.LogInfo("Shoving " + Utils.GetPrefabName(ship.gameObject)
                    + ": mass " + body.mass.ToString("0") + ", water " + plan.Distance.ToString("0")
                    + " m toward " + plan.Direction.ToString("0.00") + ", speed " + speed.ToString("0.00")
                    + " m/s, " + seconds.ToString("0.0") + " s allowed.");

            var note = "You heave the boat toward the water (" + plan.Distance.ToString("0") + " m).";

            if (!Send(ship, nview, plan.Direction, speed, seconds))
            {
                _pendingNote = note;
                return "asked the owner of the boat to shove it " + plan.Distance.ToString("0") + " m";
            }

            Pay(player);
            Say(player, note);
            return "shoving " + plan.Distance.ToString("0") + " m";
        }

        /// <summary>
        /// Stamina is paid when the shove is certain to start. Locally that is at once. For a
        /// boat someone else owns it is when the owner answers yes, because the owner can say
        /// no for reasons only it can see (the boat floated, the pusher is too far from its
        /// copy of the hull, it is already moving) and a refused shove must not cost anything.
        /// A reply RPC rather than a refund: nothing is taken that has to be given back.
        /// </summary>
        private static void Pay(Player player)
        {
            var cost = FleytaConfig.StaminaCost.Value;
            if (cost > 0f) player.UseStamina(cost);
        }

        internal static void OnReply(long sender, bool accepted, string reason)
        {
            var player = Player.m_localPlayer;
            if (player == null) return;

            var note = _pendingNote;
            _pendingNote = null;

            if (accepted)
            {
                Pay(player);
                if (note != null) Say(player, note);
                return;
            }

            Say(player, string.IsNullOrEmpty(reason) ? "It will not budge." : reason);
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

        /// <summary>
        /// True when the shove began on this machine, false when it was handed to another
        /// owner and the answer is still to come.
        /// </summary>
        private static bool Send(Ship ship, ZNetView nview, Vector3 direction, float speed, float seconds)
        {
            if (nview.IsOwner())
            {
                Fleyta.Shove.Begin(ship, direction, speed, seconds);
                return true;
            }

            var owner = nview.GetZDO().GetOwner();
            if (owner == 0L)
            {
                nview.ClaimOwnership();
                Fleyta.Shove.Begin(ship, direction, speed, seconds);
                return true;
            }

            nview.InvokeRPC(owner, RpcName, direction, speed, seconds);
            return false;
        }

        /// <summary>
        /// Runs on the owner, for a request from anywhere. Anything can be sent down an RPC,
        /// and the cheapest harm here is a boat fired across the map or a moored one pushed
        /// off by someone nowhere near it. So the owner re-derives what it can from its own
        /// view (is the hull really aground, is the sender really standing at it, is it
        /// already moving) and takes only the sender's direction on trust, clamping the speed
        /// and time to the limits its own config allows. Answers the sender either way, so a
        /// refusal costs the pusher nothing and says why.
        /// </summary>
        internal static void OnRequest(Ship ship, long sender, Vector3 direction, float speed, float seconds)
        {
            if (!FleytaConfig.Enabled.Value || ship == null) return;
            if (!ship.IsOwner()) return;

            var refusal = Judge(ship, sender, direction, speed, seconds);
            if (refusal != null)
            {
                Reply(ship, sender, false, refusal);
                return;
            }

            speed = Mathf.Min(speed, FleytaConfig.MaxSpeed.Value);
            seconds = Mathf.Min(seconds, MaxSeconds());

            direction.y = 0f;
            Fleyta.Shove.Begin(ship, direction.normalized, speed, seconds);
            Reply(ship, sender, true, null);
        }

        private static string Judge(Ship ship, long sender, Vector3 direction, float speed, float seconds)
        {
            if (ship.HasPlayerOnboard() || IsExcluded(ship)) return "That boat is not one to shove.";
            if (Fleyta.Shove.Active(ship)) return "It is already moving.";

            var body = ship.GetComponent<Rigidbody>();
            if (body == null || ship.m_floatCollider == null) return "It will not budge.";
            if (!Water.Aground(ship, body.worldCenterOfMass)) return "It is not aground.";
            if (!PusherIsAtHull(ship, sender)) return "You are too far from the boat.";

            if (!Sane(direction.x) || !Sane(direction.z)
                || direction.x * direction.x + direction.z * direction.z < 0.01f)
                return "It will not budge.";
            if (!Sane(speed) || speed <= 0f || !Sane(seconds) || seconds <= 0f) return "It will not budge.";

            return null;
        }

        private static bool Sane(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        /// <summary>
        /// The sender's player is found by owning peer among the players this machine has
        /// loaded, which works on a host and on a client that happens to own the hull alike
        /// (a client has no peer table for other clients). Its position is the game's own
        /// synced one, not a number from the request. A sender whose player is not loaded
        /// here cannot be standing next to a hull this machine has loaded.
        /// </summary>
        private static bool PusherIsAtHull(Ship ship, long sender)
        {
            var limit = FleytaConfig.Reach.Value + PusherSlack;

            foreach (var other in Player.GetAllPlayers())
            {
                if (other == null || other.GetOwner() != sender) continue;

                var at = other.transform.position;
                if (Vector3.Distance(ship.m_floatCollider.ClosestPoint(at), at) <= limit) return true;
            }

            return false;
        }

        private static void Reply(Ship ship, long sender, bool accepted, string reason)
        {
            var nview = ship.GetComponent<ZNetView>();
            if (nview != null && nview.IsValid())
                nview.InvokeRPC(sender, ReplyName, accepted, reason ?? "");
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
