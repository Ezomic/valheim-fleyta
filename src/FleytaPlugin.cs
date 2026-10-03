using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using Ezomic.Core;
using HarmonyLib;

namespace Fleyta
{
    /// <summary>
    /// Fleyta, Old Norse "to set afloat". Stand beside a boat that is aground, press a key,
    /// and it slides toward the nearest water until it floats.
    ///
    /// Vanilla gives a player on foot no way to move a boat at all: a hull run onto a beach
    /// is recoverable only by boarding and rowing against the sand, or by waiting for waves.
    /// This is the one gesture that fixes that, and nothing more: it does nothing to a boat
    /// that already floats, and the moment the hull is over floating depth the game's own
    /// buoyancy and the player's oars take over.
    ///
    /// Not client-side, and the reason is where the work happens. A Ship is moved by the
    /// machine that owns its ZDO, and a force from any other machine is overwritten, so the
    /// pusher measures the terrain and routes an RPC to the owner, who runs the slide.
    /// Whoever owns a boat may be any player, so every machine needs the handler:
    /// Requirement.Everyone below is load-bearing.
    ///
    /// There is deliberately no BepInProcess attribute. A dedicated server runs
    /// valheim_server.exe, and Core's gate only refuses on the server side of RPC_PeerInfo -
    /// so a mod that must be enforced has to be allowed to load there.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    // Soft, not hard. A hard dependency that is absent does not degrade - the plugin never
    // loads at all - and every mod here has to be installable on its own, because a stranger
    // should not need two installs to get one mod. Soft still buys the load-order guarantee
    // when Core is present, which is all that registering with the gate needs.
    [BepInDependency(CoreGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public class FleytaPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "ezomic.valheim.fleyta";
        public const string PluginName = "Fleyta";
        public const string PluginVersion = "0.1.0";
        public const string PluginAuthor = "Robbin Thijssen";

        /// <summary>Core's plugin GUID. Optional - see TryRegisterWithCore.</summary>
        private const string CoreGuid = "ezomic.valheim.core";

        internal static ManualLogSource Log;

        /// <summary>
        /// Whether Core answered at load. Worth keeping even when nothing reads it yet: the
        /// difference between gated and ungated is invisible to a player otherwise, and this
        /// is what a warning on spawn would be driven by.
        /// </summary>
        internal static bool CorePresent;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            // Config first. Registering absorbs every entry the mod has bound, so anything
            // bound after this line is carried only because Core re-absorbs at manifest
            // time - and depending on the order of two lines in an Awake is not a thing
            // worth relying on.
            FleytaConfig.Bind(Config);

            TryRegisterWithCore();

            // PatchAll over a named type, never the whole assembly. A bare PatchAll() walks
            // every type in the DLL, so a half-written patch class in another file goes live
            // the moment it compiles.
            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(FleytaPatches));
            _harmony.PatchAll(typeof(DevConsole.Hook));

            // The startup line every mod in the suite writes. It is how a log answers "which
            // build of what is actually loaded" without anyone guessing.
            Log.LogInfo(PluginName + " " + PluginVersion + " by " + PluginAuthor + " - ready.");
        }

        /// <summary>
        /// Joins Core's version gate when Core is installed, and does nothing when it is not.
        ///
        /// Standing alone costs the enforcement, and here a little more: nothing refuses a
        /// client without the mod, and such a client that owns a boat silently drops a shove
        /// aimed at it. It is the server owner's call, which is why this logs rather than
        /// refusing to run.
        /// </summary>
        private void TryRegisterWithCore()
        {
            CorePresent = Chainloader.PluginInfos.ContainsKey(CoreGuid);

            if (!CorePresent)
            {
                Log.LogInfo("Core not installed - running standalone, without the version gate.");
                return;
            }

            RegisterWithCore();
        }

        /// <summary>
        /// Kept separate and never inlined on purpose. The JIT resolves the assemblies a
        /// method needs when it first compiles that method, so a Suite call sitting directly
        /// in Awake would drag Ezomic.Core in before the check above could prevent it - and
        /// the missing-assembly exception would land during plugin load, which is the exact
        /// failure this arrangement exists to avoid. Isolating it means the type is only
        /// ever resolved on a machine that has Core.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RegisterWithCore()
        {
            // Everyone, deliberately. HostOnly would let in a client without the mod, and that
            // client can own a boat: the shove request is routed to the owner of the Ship's
            // ZDO, an owner without the handler drops it, and the pusher would see the stamina
            // spent and the boat sit still. The mod registers no prefab and changes no item,
            // so this is about who can receive an RPC and not about hashes.
            Suite.Register(PluginGuid, PluginName, PluginVersion, Config, Requirement.Everyone);

            Suite.Sync(FleytaConfig.Enabled);

            // Personal by intent. Core syncs the whole config by default, and a host turning
            // on someone else's logging is not something anybody asked for. The key is
            // exempt already; naming it would only repeat that.
            Suite.Local(FleytaConfig.Verbose);

        }

        private void OnDestroy()
        {
            // UnpatchSelf, never UnpatchAll(). The argumentless one unpatches every mod in
            // the process, not just this one.
            if (_harmony != null) _harmony.UnpatchSelf();
        }
    }
}
