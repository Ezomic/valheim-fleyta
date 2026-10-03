# Fleyta

Fleyta lets you shove a beached boat back into the water. Stand beside a boat that is aground,
press the shove key (B by default), and it slides toward the nearest water until it floats.
Then the boat is the game's again. It does nothing to a boat that is already afloat, and
nothing to the raft.

## Why

Vanilla gives a player on foot no way to move a boat at all. A hull run onto a beach is
recoverable only by climbing aboard and rowing against the sand, or by waiting for waves that
may never come. That is the whole annoyance, and this is the whole fix. It is deliberately
narrower than the things it could have been: it does not push you off a dock while aboard, it
does not nudge a boat along open water, and it does not drag a boat across land. One
gesture, for the one situation the game has no answer to.

Two design calls follow from that.

The direction comes from the terrain, not from you. The mod finds the nearest direction in
which the ground drops to floating depth, without first climbing a ridge, and pushes that way.
Standing on the sea side and pressing the key still sends the boat to the sea, because "which
way is the water" is a fact about the boat's position and not about yours. If no water is
within 30 metres, nothing moves and you are told so. A boat left halfway up a beach is worse
than one that stayed put.

The key is a key, not hold-E. A hull has nothing to hover over: the only thing the crosshair
can land on is the helm, where E already means "take the wheel", and holding it would fight
the tap that boards. The helm's hover text names the shove key when pressing it would do
something.

## How it feels

- One press, 20 stamina, and the boat slides at a walking pace until it is over water deep
  enough to float. Nothing to hold, no need to stay.
- A heavier hull is slower. The speed scales with the square root of the hull's mass, so a
  longship is slower than a karve by seconds and not by minutes.
- It refuses, and says why, when the boat is already afloat, when someone is aboard, when
  you are out of reach, or when the water is too far.

## Installing

Needs BepInEx. Nothing else. Through a mod manager it is one install. By hand, put
`Fleyta.dll` in `BepInEx/plugins/Fleyta/`.

Then start the game once and quit. That first run writes the config file. It does not exist
before the mod has loaded, which is the usual reason people think it is broken.

## Settings

The file is `BepInEx/config/ezomic.valheim.fleyta.cfg`. Open it in any text editor. Every
setting has a comment above it, so the file explains itself. The two worth knowing first are
`FloatDepth`, how deep the sea floor must be before a boat counts as afloat, and `BaseSpeed`.
Neither was measured against the real hulls when the mod was written, so they are the numbers
to tune if a boat stops in the shallows or the slide feels wrong. Turning `Verbose` on writes
each decision, including the hull's real mass, to the log.

Note that changing a default in a new version does nothing on a machine that has already run
the mod. BepInEx writes every entry on first run and the saved value wins.

## Needs a play test

Not run in game yet, and two settings depend on a vanilla behaviour that could only be read
from the code. An unattended hull is damped hard in the water: the game multiplies its
horizontal speed by 0.1 every physics step while nobody is aboard and the hull's centre is
0.5 m or more under the water line. A shove therefore does not coast into the sea, and where
the hull is already in that regime but not yet over `FloatDepth` the push crawls. Tune
`FloatDepth` and `Acceleration` together: a `FloatDepth` that is too deep makes the last
metres crawl, an `Acceleration` that is too low makes them stall. `Verbose` logs where a
shove ends.

## Multiplayer

**Everyone needs it.** A boat is moved by whichever player's game owns it, and force applied
from any other machine is silently thrown away. So the mod measures the terrain on your side
and sends the request to the boat's owner, who does the pushing. That owner can be anyone, so
every player needs the mod, at the same version. A player without it cannot be the one who
shoves, and would drop a request sent to a boat they own.

If [Core](https://github.com/Ezomic/valheim-core) is installed, this mod registers with its
version gate and the host's settings apply to everyone connected to it, in memory only -
your own config file is never written to and comes back the moment you disconnect. Keybinds
and `Verbose` stay yours. Without Core the mod still runs; what is lost is the enforcement.

## Bugs and ideas

Both go to the site. [longhouse.thijssensoftware.nl/bugs](https://longhouse.thijssensoftware.nl/bugs)
is for anything broken, and [longhouse.thijssensoftware.nl/ideas](https://longhouse.thijssensoftware.nl/ideas)
is for what a mod should do next. You can vote on other people's ideas there as well.

Signing in takes a Steam or Discord account. I work from that list, so the votes decide what
I pick up next.

## Licence

MIT. See `LICENSE`.
