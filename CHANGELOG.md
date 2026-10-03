# Changelog

## 0.1.0 - unreleased

First version. Stand beside a boat that is aground and press the shove key (B by default):
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

Not run in game yet. The defaults for how deep the water must be before a boat counts as
afloat (2 m) and for the push speed are estimates; Verbose writes the real hull masses and
every decision to the log so they can be tuned.
