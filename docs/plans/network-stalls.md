# 🛰️ Plan: players hanging in the air (network stalls)

Players reported seeing each other float a little above the ground for a moment before they drop into place, as if
out of sync. Nothing shows in the logs. This page is the analysis and the plan for the fix, written for a world far
into the game: many ships, many portals and much of the map discovered.

**Status:** carried out after 0.143.0 (see *Unreleased* in the [changelog](../../CHANGELOG.md)); not yet measured in
a live world, see section 4.

## 1. What the game does (vanilla)

**A player whose position is late is moved on in a straight line.** `ZSyncTransform.SyncPosition` on the machines
that do not own a character:

```csharp
m_targetPosTimer = Mathf.Min(m_targetPosTimer + dt, 2f);
position += zdo.GetVec3(ZDOVars.s_velHash, Vector3.zero) * m_targetPosTimer;
```

The timer starts again when a new ZDO revision arrives. Until then, the last known velocity is carried on for up to
two seconds, **without gravity**. A player who jumped, climbed a slope or ran up stairs keeps going up on the other
screens until the next update arrives, and then drops into place. That is exactly the report, and the game does not
log it because to the game it is normal.

**Positions stop when the connection's queue holds more than 10 KB.** `ZDOMan.SendZDOs`:

```csharp
int sendQueueSize = peer.m_peer.m_socket.GetSendQueueSize();
if (!flush && sendQueueSize > 10240) return false;
int num = 10240 - sendQueueSize;
if (num < 2048) return false;
```

The server visits one peer per update every 50 ms, and fills at most 10 KB minus what is already queued. The queue
size counts the game's own queue plus Steam's pending and sent-but-unacknowledged bytes.

**Steam sends 150 KB/s.** `ZSteamSocket.RegisterGlobalCallbacks` sets `SendRateMin`/`SendRateMax` to 153600.

**Routed RPCs ignore the 10 KB rule.** `ZRoutedRpc.InvokeRoutedRPC` queues the message at once. So every routed
message that leaves more than ~8 KB in a player's queue stops all ZDO updates to that player, positions included,
until Steam has sent it: roughly *(size − 8 KB) / 150 KB/s*.

## 2. What White Hilt sends that fills the queue

### 2.1 The portal map, every two seconds while any ship is loaded (the steady pulse)

`PortalMapService.Update` reads every ship's position again every 2 s (`ShipUpdateInterval`) and calls `Publish`.
`UpdateShip` stores the exact `zdo.GetPosition()`, and a ship in water bobs on the waves, so the list's fingerprint
(`TryGetChanged`) changes every time, even for a moored ship. The whole list, **all portals and all ships**, then goes
to every player every 2 s. In a late world with a few hundred portals that is tens of KB per player per pulse, and
each pulse stops positions for 100–300 ms. At 7 m/s up after a jump that is 1–2 m in the air: "a little up in the
air", "suddenly".

### 2.2 Large lists are sent in 192 KB pieces (the long stall)

`ServerLists.SendPieces` sends the next piece once the queue is **below 192 KB**. Vanilla stops positions at 10 KB.
While a large list is on its way (discoveries, map areas, portals) the player gets no positions at all: 600 KB is
about four seconds of standing still or floating. A newer list for the same call also cut the older one off half
sent, so a list that changes faster than it can be sent never arrives.

### 2.3 Discoveries are sent whole whenever anything changed

`DiscoveryService` sends every marker (20 bytes each, thousands in an explored world, gzipped twice) to every player
whenever the list changed. Whether berries can be picked (`Ripe`) changes all the time, so in practice the whole list
goes out after nearly every scan.

### 2.4 Shared blueprints go in one message

`SharedBlueprints.Share` and `RPC_Fetch` send the whole blueprint (up to `MaxKilobytes`, default 256 KB) as one
routed message: one stall of up to ~1.7 s for the player who shares or fetches.

### Not the cause

`PortalGhostPatch` (only clears the copy left at a portal), `ShipPassengerSync` (only White Hilt ships),
`OldLandGrounding` (only vegetation), and the world scans (all spread over frames or rare).

## 3. The fix

### 3.1 `ServerLists`: one paced queue per player

- **Small pieces:** 16 KB instead of 192 KB.
- **The game's data first:** a piece is sent only when the player's queue is below 6 KB, under the 8 KB at which the
  game sends its own ZDOs. A piece then holds back positions by tens of milliseconds, not seconds.
- **A share of the line:** at most 64 KB/s per player for lists, so ZDOs keep the larger part of Steam's 150 KB/s.
- **One queue per player, in order.** Every list for a player waits in one queue, sent in the order queued. A newer
  list replaces an older list *of the same call* only while that one has not started. A list being sent is finished,
  so a list that changes often still arrives.
- **Local calls** (the host to itself) are delivered at once.
- `ServerLists.IsWaiting(peer, rpc)` tells a sender whether its last list still waits.
- The queue rules live in `SendQueue`, a plain class with tests; a coroutine per player drives it.

### 3.2 Portal map: ships apart, and only when they really move

- Ships go in their own small message (`WhiteHiltShipMap`), portals in theirs (`WhiteHiltPortalMap`). The client
  keeps both and shows them together.
- A ship's position is sent again only once it has moved 8 m (`ShipMoveResend`). A moored ship that bobs never
  sends; a sailing one sends a list of ships only (about 40 bytes each) every 2 s.
- A new full scan keeps a ship's last sent position while it has not moved that far, so the scan itself does not
  send the list again.

### 3.3 Discoveries: changes only

- The server keeps the list it sent last, in a fixed order, with a version number. The kinds table only grows, so a
  kind keeps its number; the client shows only the kinds in use (`DiscoveryPackage.ForMap`).
- `DiscoveryPackage.Diff` and `DiscoveryPackage.Apply` do the work, the same code on both sides, with tests.
- After a scan it works out what changed: markers removed (by their place in the old list), markers whose count or
  ripeness changed (same kind and place), and markers added. The new list is the old one with those changes, in the
  same order the client builds it, so both always hold the same list.
- A player who has the previous version gets only the changes; one who joined, missed a version or whose last list
  still waits gets the whole list. The changes are also sent whole when they would be larger than the list.
- A client that gets changes for a version it does not have asks the server for the whole list
  (`WhiteHiltDiscoveriesResync`).
- The list is gzipped once, by `ServerLists`, instead of twice.

### 3.4 Blueprints: through `ServerLists`

Sharing and fetching send through `ServerLists` in paced pieces, without a second gzip. The server stops collecting
pieces of a share once they exceed `MaxKilobytes`, before unpacking.

## 4. How to check

- `whitehilt_perf start` on the server and on a client, play a while with ships loaded, then `whitehilt_perf report`.
  `WhiteHiltPortalMap` should no longer come every 2 s, `WhiteHiltShipMap` should be small, and
  `WhiteHiltDiscoveries` should be small after the first list.
- Two players: one jumps and runs up a slope next to a loaded ship while the other watches. The jumper should no
  longer hang in the air.
- Join a late world: discoveries, portals and areas still arrive (a little slower for a very large list, at most
  64 KB/s).
