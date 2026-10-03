using UnityEngine;

namespace Fleyta
{
    /// <summary>
    /// What the terrain says about a boat's position: is it aground, and where is the
    /// nearest water.
    ///
    /// Read off the heightmap and the world's sea level, never off where the pusher stands.
    /// The question is "which way is down to the sea from the hull", and a player on the
    /// seaward side would otherwise push the boat inland.
    ///
    /// Valheim's sea is one flat level, ZoneSystem.m_waterLevel, so water is simply terrain
    /// lower than that. Rivers and ponds do not exist in vanilla terrain, which is what keeps
    /// this a few lines. Ashlands lava is not water to Floating.GetWaterLevel and is not
    /// detected here either, so a boat shoved on an Ashlands shore is launched by sea level
    /// alone and left to the game.
    /// </summary>
    internal static class Water
    {
        private const int Directions = 24;
        private const float Step = 2f;

        internal struct Plan
        {
            internal bool Found;
            internal Vector3 Direction;
            internal float Distance;
        }

        internal static float SeaLevel
        {
            get { return ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : 30f; }
        }

        /// <summary>
        /// True when the sea floor under this point is deep enough to float a hull. False
        /// also answers "unknown" (no heightmap loaded there), which is why Aground exists
        /// as its own question rather than being !Afloat.
        /// </summary>
        internal static bool Afloat(Vector3 at)
        {
            float ground;
            return Heightmap.GetHeight(at, out ground)
                   && ground <= SeaLevel - FleytaConfig.FloatDepth.Value;
        }

        /// <summary>
        /// True only when the terrain is known and too shallow to float the hull. A boat on
        /// a dock stands over deep ground, so it is afloat by this measure whatever it rests
        /// on, and a boat in a zone that is not loaded is neither.
        /// </summary>
        internal static bool Aground(Vector3 at)
        {
            float ground;
            return Heightmap.GetHeight(at, out ground)
                   && ground > SeaLevel - FleytaConfig.FloatDepth.Value;
        }

        /// <summary>
        /// The nearest direction in which the ground drops to floating depth without first
        /// climbing more than MaxRise above where the hull sits.
        ///
        /// Rings outward, 2m a step, 24 directions: the first ring that has any answer wins,
        /// so it is the nearest water by distance, and within that ring the deepest ground
        /// wins, which is the steepest way down. A direction is dropped for good the moment
        /// one sample on it is too high or has no heightmap, so a ridge between the boat and
        /// a nearer sea cannot be crossed by sampling past it.
        ///
        /// The samples are a line through the hull's centre. The hull is several metres wide
        /// and a shove that meets a rock stops there; that is the physics' call and not
        /// something to second-guess in a planner.
        /// </summary>
        internal static Plan Find(Vector3 from)
        {
            var plan = new Plan();
            float start;
            if (!Heightmap.GetHeight(from, out start)) return plan;

            float target = SeaLevel - FleytaConfig.FloatDepth.Value;
            float ceiling = start + FleytaConfig.MaxRise.Value;
            var blocked = new bool[Directions];

            for (float distance = Step; distance <= FleytaConfig.MaxDistance.Value; distance += Step)
            {
                var bestDepth = float.MaxValue;

                for (int i = 0; i < Directions; i++)
                {
                    if (blocked[i]) continue;

                    var direction = Direction(i);
                    var point = from + direction * distance;

                    float ground;
                    if (!Heightmap.GetHeight(point, out ground) || ground > ceiling)
                    {
                        blocked[i] = true;
                        continue;
                    }

                    if (ground > target || ground >= bestDepth) continue;

                    bestDepth = ground;
                    plan.Found = true;
                    plan.Direction = direction;
                    plan.Distance = distance;
                }

                if (plan.Found) return plan;
            }

            return plan;
        }

        private static Vector3 Direction(int index)
        {
            var angle = index * (360f / Directions) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
        }
    }
}
