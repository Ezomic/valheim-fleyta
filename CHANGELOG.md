# Changelog

## 1.0.0 - 2026-10-08

First release, a member of Longhouse 2.4.0. Stand beside a boat that is aground and press the shove key (B by default):
it slides toward the nearest water until it floats, and from there it is the game's. It
does nothing to a boat that already floats, and nothing to the raft.

The direction comes from the ground under the hull, not from where you stand, so shoving
from the sea side still sends the boat to the sea. If no water is within 30 metres the boat
is not moved at all and you are told so; a boat left halfway up a beach is worse than one
that stayed put. A heavier hull slides slower, by the square root of its mass, and every
shove costs 20 stamina.

Whoever owns the boat does the pushing. Vanilla moves a ship only from the machine that owns
it and discards force from any other, so the request is sent to the owner and the mod has to
be installed on every machine, not only the host's.

Not run in game yet, alone or with other players, so the request to a boat's owner is untested. The defaults for how deep the water must be before a boat counts as
afloat (2 m) and for the push speed are estimates; Verbose writes the real hull masses and
every decision to the log so they can be tuned.

Review follow-ups, made before this release:

- The boat's owner no longer takes a shove on trust. It checks that the hull is aground, that
  the sender's player is standing within reach of it (by the game's synced position, not a
  number in the request), and that it is not already moving, then clamps speed and time to its
  own limits instead of dropping out-of-range requests.
- Both ends take their limits from the same config (the longest shove is MaxDistance over
  MinSpeed, doubled, plus three seconds), so a longship moves the same whether you or someone
  else owns it. The owner now answers each request, and stamina is paid when it accepts, so a
  refused shove costs nothing and says why.
- The shove key also waits for the game's own input check, so it does nothing during a
  cutscene or while another window has taken input.
- A boat floating in shallow water is no longer treated as aground. Aground now requires the
  hull to be out of the water's reach by the ship's own buoyancy test, so a moored boat in
  1.5 m is not offered a shove and cannot be pushed off its mooring.
- Documented that an unattended hull in the water is damped hard, which bears on FloatDepth
  and Acceleration; both are on the play test list.
