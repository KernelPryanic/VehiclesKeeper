# Vehicle Keeper

A GTA V single-player mod (ScriptHookV .NET / SHVDN3) that **keeps your favorite
vehicles** — save a car and it survives despawning, comes back exactly as you left it
(paint, mods, damage, fuel and all), and shows on the map so you can always find it.

Save a vehicle once; from then on it's yours to spawn, park and customize, and the mod
re-saves its state as you drive so a respawn is never a fresh, stock car. Compatible with
the Single Player Apartment mod. Derived from the original
[Save Vehicles](https://ru.gta5-mods.com/scripts/save-vehicles-no-more-despawning-1-0)
mod, heavily refactored and extended.

Built on SHVDN3 + LemonUI. Ships as a **single DLL with no third-party dependencies** —
persistence uses the .NET `XmlSerializer`.

## What gets preserved

A saved vehicle is identified by its **model + license plate**, so saving the same model
with the same plate overrides the old entry. On respawn the mod restores:

- **Paint** — primary, secondary, pearlescent, rim, trim and dashboard colors; custom
  RGB paints; and the paint **finish** (chrome, matte, metallic, worn), which the color
  value alone doesn't carry.
- **Mods** — every performance/visual mod slot and toggle, wheel type, livery, window
  tint, neon lights and color, xenon light color, tire smoke, and vehicle extras
  (togglable body parts).
- **Condition** — body/engine/petrol-tank health, fuel and oil level, dirt, broken
  windows/doors, wheel and tire state, and the proof flags (bullet/fire/explosion/etc.,
  plus bulletproof tires).
- **State** — license plate and style, lights, lock status, alarm, roof state, engine
  on/off, and an attached trailer.

Health is floored on capture so a saved car always respawns drivable rather than
pre-wrecked.

## Auto-save

The mod tracks the saved vehicle you're driving and re-saves it automatically, so
appearance and damage changes stick without a manual Save:

- **Position** is written every second while you drive (cheap — only location changes).
- **Full state** is re-captured on a configurable **interval** (10s by default) and the
  instant you **leave** the car — the interval covers quitting the game mid-drive; the
  exit flush covers everything you changed during the drive.

Auto-save is under its own **Auto-Save** submenu, with an on/off toggle and the interval.
With it off, appearance changes persist only on an explicit Save.

## Install

1. Requires [ScriptHookV](http://www.dev-c.com/gtav/scripthookv/) and
   [ScriptHookV .NET (SHVDN3)](https://github.com/scripthookvdotnet/scripthookvdotnet/)
   with [LemonUI](https://www.gta5-mods.com/tools/lemonui).
2. Copy `VehicleKeeper.dll` into your `GTA V/scripts/` folder.
3. Launch the game (or reload scripts).

Or install with the [Vortex extension](https://www.nexusmods.com/site/mods/2023) for
GTA V.

## Use

Press **Shift + T** (configurable) to open the menu. The same key closes it.

Main menu:

- **Saved Vehicles ▸** — every saved vehicle. Scroll a vehicle to pick an action, then
  press: **Spawn**, **Spawn Nearby**, **Despawn**, **Save** (override its stored state),
  **Unsave**, or **Set Spawn Location** (to the current map waypoint).
- **Save Current Vehicle** — save the car you're in (or override it if already saved).
- **Spawn All** / **Despawn All** / **Unsave All**.
- **Blips ▸** — map blip **Color** and **Distance** (how far a saved vehicle stays
  visible; `-1` = always show).
- **Auto-Save ▸** — **Enabled** and re-save **Interval** (see above).
- **Log Level** — verbosity of `VehicleKeeper.log`.

Optional **Save** and **Unsave** quick-keys are unbound by default; set them in the INI.

## Files

Config, logs and saved vehicles live **outside** the game folder, under
`%APPDATA%\GTA V Mods\KernelPryanic\VehicleKeeper\` (GTA V Enhanced locks files written
under the game tree at launch; this location stays writable on both editions):

- `Vehicles\<id>.xml` — one file per saved vehicle (so saving many never rewrites the
  whole set). `<id>` is a hash of the model + plate.
- `VehicleKeeper.ini` — config
- `VehicleKeeper.log` — diagnostics (cleared on startup)

A pre-4.x single-file store (`preserved-vehicles.xml`) is migrated automatically on first
launch.

## Config (`VehicleKeeper.ini`)

Most settings are set through the menu; the keys are also editable by hand. A fresh
install writes the file with every key at its default:

```ini
[Configuration]
MenuKey = Shift, T        ; any key + optional Shift/Control/Alt, e.g. F9  or  Control, M
SaveKey = None            ; quick-key to save the current vehicle (None = unbound)
UnsaveKey = None          ; quick-key to unsave the current vehicle (None = unbound)
BlipColor = Blue          ; Blue | Green | Purple | Red | Orange | Yellow | Pink | White
BlipDistance = 500        ; meters a saved vehicle's blip stays visible (-1 = always)
VehiclePersistencePath =  ; where saved vehicles live (defaults to the %APPDATA% folder)

[AutoSave]
Enabled = True            ; True | False  - auto re-save a driven vehicle on interval + exit
IntervalSeconds = 10      ; 1 | 2 | 5 | 10 | 20 | 30 | 60 | 90 | 120

[Logging]
Level = Info              ; Info | Debug | Error
```

## Build

`make build` (Release x64, .NET 4.8) → `bin/Release/VehicleKeeper.dll`. `make lint` for a
warning-free rebuild, `make package` to zip a deploy-ready archive. See `AGENTS.md` for
conventions.

The build links against `ScriptHookVDotNet3.dll` and `LemonUI.SHVDN3.dll` from
`..\packages\` (not committed). Tagging a `MAJOR.MINOR.PATCH` commit builds and publishes
a release — the tag is the version source of truth.

To update across a major version (`X.0.0`), remove the old `VehicleKeeper` data folder
first, as the storage format may be incompatible.

## Known limitations

- **Chrome and other paint finishes** are preserved for the primary/secondary slots, but
  only when the game reports them as mod colors; an unset slot falls back to the standard
  color.
- **Visual body deformation (dents)** isn't preserved — only the body health value is, so
  a respawned car is straight even if it was dented.
- **Radio station** isn't preserved (the game exposes no reliable per-vehicle read); a
  respawned vehicle keeps the game's default radio, which you can change as usual.
- **Bumper break-off** isn't preserved.

## Credits

Original mod:
[Save Vehicles](https://ru.gta5-mods.com/scripts/save-vehicles-no-more-despawning-1-0)
(permission given to modify).
