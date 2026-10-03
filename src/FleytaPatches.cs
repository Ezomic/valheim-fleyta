using HarmonyLib;
using UnityEngine;

namespace Fleyta
{
    /// <summary>
    /// Three small hooks: register the RPC on every ship, read the key, and name the key in
    /// the helm's hover text.
    ///
    /// Nothing here touches movement. The push is a component on the boat (Shove) and the
    /// game's own buoyancy takes over once the hull floats, which is the seam that survives
    /// a game update.
    /// </summary>
    internal static class FleytaPatches
    {
        /// <summary>
        /// Every ship, on every machine that has the mod, can be asked to shove itself. The
        /// registration has to exist wherever the ZDO might be owned, because the request is
        /// routed to the owner and an owner without the handler drops it with a log line.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Ship), "Start")]
        private static void ShipStart(Ship __instance)
        {
            var nview = __instance.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid()) return;

            var ship = __instance;
            nview.Register<Vector3, float, float>(Shoving.RpcName,
                (sender, direction, speed, seconds) => Shoving.OnRequest(ship, direction, speed, seconds));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), "Update")]
        private static void PlayerUpdate(Player __instance)
        {
            if (__instance != Player.m_localPlayer) return;
            if (!FleytaConfig.Enabled.Value) return;
            if (__instance.IsDead() || InventoryGui.IsVisible() || Menu.IsVisible()) return;
            if (!Keys.Pressed(FleytaConfig.ShoveKey.Value)) return;

            Shoving.TryShove(__instance);
        }

        /// <summary>
        /// The helm is the only thing on a ship the crosshair can land on, so its hover is
        /// where the key is named. Shown only when pressing it would do something: an
        /// unattended hull, not excluded, and aground.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShipControlls), nameof(ShipControlls.GetHoverText))]
        private static void HelmHover(ShipControlls __instance, ref string __result)
        {
            if (!FleytaConfig.Enabled.Value || FleytaConfig.ShoveKey.Value == KeyCode.None) return;

            var ship = __instance.m_ship;
            if (ship == null || ship.HasPlayerOnboard() || Shoving.IsExcluded(ship)) return;
            if (Shove.Active(ship)) return;

            var body = ship.GetComponent<Rigidbody>();
            if (body == null || !Water.Aground(body.worldCenterOfMass)) return;

            __result += "\n[<color=yellow><b>" + FleytaConfig.ShoveKey.Value + "</b></color>] Shove toward the water";
        }
    }
}
