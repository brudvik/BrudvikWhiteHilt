# 🎬 Videos

[← Back to the README](../README.MD)

The [White Hilt channel on YouTube](https://www.youtube.com/@WhiteHilt) opens the mod's code and goes through it one topic at a time, with the game running alongside. Each episode takes its examples from this repository and tells the story of a real bug: what went wrong, how it was found and what fixed it. The episodes follow the [guide to how the mod is built](architecture.md), and the feature pages link to the episode about them.

[![White Hilt: A Valheim Mod (Trailer)](https://img.youtube.com/vi/tYbkbwbTJFw/maxresdefault.jpg)](https://youtu.be/tYbkbwbTJFw)

## 📺 Episodes

| | Episode | What it is about | Read more |
|---|---------|------------------|-----------|
| [![EP01](https://img.youtube.com/vi/asFji2_038w/mqdefault.jpg)](https://youtu.be/asFji2_038w) | **[EP01 · The Plugin](https://youtu.be/asFji2_038w)** | What a mod runs first: how BepInEx finds the plugin, what Awake must do at once and what waits for Jötunn's events. With the bug where equipped gear vanished from the main menu. | [Start-up](architecture.md#-start-up) |
| [![EP02](https://img.youtube.com/vi/Wr303-u2zMc/mqdefault.jpg)](https://youtu.be/Wr303-u2zMc) | **[EP02 · Harmony Patches](https://youtu.be/Wr303-u2zMc)** | Prefixes, postfixes and `__state`, when a prefix may replace a method, and the one missing attribute that made `PatchAll` skip a patch for almost thirty versions. | [Harmony patches](architecture.md#-harmony-patches) |
| [![EP03](https://img.youtube.com/vi/eMhMZ4ILjRA/mqdefault.jpg)](https://youtu.be/eMhMZ4ILjRA) | **[EP03 · An Item Is a Class](https://youtu.be/eMhMZ4ILjRA)** | A whole new weapon in one small class: reflection, cloning a vanilla item, why a prefab name must never change and why a missing model must never remove an item. | [How the mod is built](architecture.md), [White Hilt gear](equipment.md) |
| [![EP04](https://img.youtube.com/vi/WcdL2K7xbL4/mqdefault.jpg)](https://youtu.be/WcdL2K7xbL4) | **[EP04 · Who Owns It](https://youtu.be/WcdL2K7xbL4)** | Multiplayer: the ZDO is the state, the owner decides, others ask with an RPC and the server checks what clients send. With two players handing the same chests back and forth. | [Multiplayer](architecture.md#-multiplayer) |
| [![EP05](https://img.youtube.com/vi/_5UT5RH4YnM/mqdefault.jpg)](https://youtu.be/_5UT5RH4YnM) | **[EP05 · A Piece From a Pole](https://youtu.be/_5UT5RH4YnM)** | Build pieces as data: a build tool with its own piece table, every piece a clone of the wood pole with its look replaced. With decorations drawn see-through and tables nothing could be set on. | [Decor Hammer](decor.md), [Assets and data files](architecture.md#-assets-and-data-files) |
| [![EP06](https://img.youtube.com/vi/nF4_nJL1HgY/mqdefault.jpg)](https://youtu.be/nF4_nJL1HgY) | **[EP06 · The Chests](https://youtu.be/nF4_nJL1HgY)** | Crafting with what lies in the chests around you, and three designs for taking from a chest another player may hold. | [Crafting from chests](base.md#-crafting-from-chests) |
| [![EP07](https://img.youtube.com/vi/e94Xs37dkLM/mqdefault.jpg)](https://youtu.be/e94Xs37dkLM) | **[EP07 · Skidbladnir](https://youtu.be/e94Xs37dkLM)** | A ship of your own: a clone of the longship with a model from Blender, and why it once looked black and see-through. | [Skidbladnir](ships.md#skidbladnir) |
| [![EP08](https://img.youtube.com/vi/dey8-4RZqTI/mqdefault.jpg)](https://youtu.be/dey8-4RZqTI) | **[EP08 · Portals](https://youtu.be/dey8-4RZqTI)** | The mod's own portals, travel effects every player sees, and a bug in the game itself that left a frozen copy of a traveller behind. | [White Hilt Portals](portals.md#white-hilt-portals) |
| [![EP09](https://img.youtube.com/vi/DDHtV-J-laI/mqdefault.jpg)](https://youtu.be/DDHtV-J-laI) | **[EP09 · Portraits](https://youtu.be/DDHtV-J-laI)** | Every player on the map as a portrait of their Viking, taken once, shared by its hash and cached. With a friend who became a letter after dying. | [Navigation](navigation.md) |

New episodes come out on the [channel](https://www.youtube.com/@WhiteHilt).
