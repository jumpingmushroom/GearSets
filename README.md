# GearSets

World of Warcraft's Equipment Manager for Valheim. Save what you are wearing — and your hotbar
layout — as named gear sets, then swap back with one click from the inventory or by holding a key
in the world.

## Features

- **Gear Sets window**: a "Gear Sets" tab on the inventory opens a movable window listing your
  sets with their status (Equipped, Ready, N items missing). Save the current gear as a new set,
  update a set from what you wear, rename it, pick its icon, delete it.
- **Partial sets**: click any slot or hotbar position to ignore it; ignored slots are left alone
  when you equip the set.
- **Follows the exact item**: a set remembers the very copy you saved, wherever it moves. If it is
  gone, the best item of the same type is used and the message says so.
- **Radial picker**: built into Valheim's own radial menu. Press or hold **H** to open it on your sets,
  or open the **Gear Sets** group in the radial (**G**, or the gamepad radial). Each set shows its
  status and any missing items.
- **Respects the game**: equipping uses the normal equip time (about a second per armour piece)
  and can be interrupted by jumping or dodging; use the set again to finish. `InstantSwap` in the
  config equips everything at once.
- **Missing items are named**, e.g. "Mining equipped. Missing: Iron Pickaxe (used Antler Pickaxe)."
- **Set items are marked**: a small diamond on their inventory cell and a "Gear sets: …" line in
  their tooltip.
- **Protection**: confirms before dropping or obliterating a set item.

## Works with

Fully standalone — only BepInEx is required. When these are installed GearSets adapts:

- **ExtraSlots**: up to five utility items per set; gear in ExtraSlots' equipment cells is found and
  never pulled out of them.
- **Quick Stack Store Sort Trash Restock**: confirms before trashing set items; quick-trash and
  Store All leave them alone.
- **MyLittleUI**: the tooltip line is added after its regrouping; the marker stays clear of its
  quality stars.
- **Yatsuta AutoShield**: hands are equipped so a set's off-hand (torch, empty, a specific shield)
  is respected.

Each integration can be switched off in the config.

## Configuration

`BepInEx/config/com.jumpingmushroom.gearsets.cfg`: MaxSets, InstantSwap, RadialKey, radial style
(built-in or classic), the Gear Sets group in the radial, window scale and position, tooltip line,
cell marker and its corner, each protection prompt, each integration.

Client-side only; nothing to install on the server. Sets are saved with your character.
