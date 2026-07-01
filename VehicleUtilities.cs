using System;
using System.Security.Cryptography;
using System.Text;
using GTA;
using GTA.Math;
using GTA.Native;

namespace VehicleKeeper {
	public static class VehicleUtilities {
		static readonly VehicleWindowIndex[] Windows = (VehicleWindowIndex[])Enum.GetValues(typeof(VehicleWindowIndex));
		static readonly VehicleDoorIndex[] Doors = (VehicleDoorIndex[])Enum.GetValues(typeof(VehicleDoorIndex));
		static readonly VehicleWheelBoneId[] Wheels = (VehicleWheelBoneId[])Enum.GetValues(typeof(VehicleWheelBoneId));
		static readonly VehicleNeonLight[] Neon = (VehicleNeonLight[])Enum.GetValues(typeof(VehicleNeonLight));
		static readonly VehicleModType[] Mods = (VehicleModType[])Enum.GetValues(typeof(VehicleModType));
		static readonly VehicleToggleModType[] ToggleMods = (VehicleToggleModType[])Enum.GetValues(typeof(VehicleToggleModType));
		// Vehicle "extras" (togglable body parts). Extras[idx].Exists() filters to
		// the slots a given model actually has; the enum values are the 1..16 indices.
		static readonly VehicleExtraIndex[] Extras = (VehicleExtraIndex[])Enum.GetValues(typeof(VehicleExtraIndex));

		public static byte[] GetHash(string input) {
			using (HashAlgorithm algorithm = SHA256.Create()) {
				return algorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
			}
		}

		public static string GetHashString(string input) {
			StringBuilder sb = new StringBuilder();
			foreach (byte b in GetHash(input))
				sb.Append(b.ToString("X2"));

			return sb.ToString();
		}

		public static VehicleData CreateInfo(Vehicle vehicle) {
			VehicleData VehicleData = new VehicleData {
				Handle = vehicle.Handle,
				VehicleName = vehicle.DisplayName,
				Vehicle = (uint)vehicle.Model.Hash,

				// General. The health floors below are deliberate: a saved car is
				// meant to respawn drivable, so we never persist health low enough
				// to spawn it pre-wrecked (body/engine >= 20, petrol tank healthy).
				DirtLevel = vehicle.DirtLevel,
				BodyHealth = Math.Max(vehicle.BodyHealth, (float)20),
				Livery = vehicle.Mods.Livery,
				WindowTint = vehicle.Mods.WindowTint,
				RoofState = vehicle.RoofState,
				WheelType = vehicle.Mods.WheelType,
				IsBulletProof = vehicle.IsBulletProof,

				HeliEngineHealth = Math.Max(vehicle.HeliEngineHealth, (float)20),

				// Location
				Position = vehicle.Position,
				Rotation = vehicle.Rotation,

				// Coloring
				PrimaryColor = vehicle.Mods.PrimaryColor,
				SecondaryColor = vehicle.Mods.SecondaryColor,
				DashboardColor = vehicle.Mods.DashboardColor,
				PearlescentColor = vehicle.Mods.PearlescentColor,
				RimColor = vehicle.Mods.RimColor,
				TrimColor = vehicle.Mods.TrimColor,
				CustomPrimaryColor = vehicle.Mods.CustomPrimaryColor,
				CustomSecondaryColor = vehicle.Mods.CustomSecondaryColor,
				IsPrimaryColorCustom = vehicle.Mods.IsPrimaryColorCustom,
				IsSecondaryColorCustom = vehicle.Mods.IsSecondaryColorCustom,
				TireSmokeColor = vehicle.Mods.TireSmokeColor,
				NeonLightsColor = vehicle.Mods.NeonLightsColor,
				ColorCombination = vehicle.Mods.ColorCombination,
				// No SHVDN wrapper for the xenon color index; -1 == stock color.
				XenonColorIndex = Function.Call<int>(Hash.GET_VEHICLE_XENON_LIGHT_COLOR_INDEX, vehicle),

				// License plate
				LicensePlate = vehicle.Mods.LicensePlate,
				LicensePlateStyle = vehicle.Mods.LicensePlateStyle,

				// Lights
				LightsMultiplier = vehicle.LightsMultiplier,
				AreLightsOn = vehicle.AreLightsOn,
				IsLeftHeadlightBroken = vehicle.IsLeftHeadLightBroken,
				IsRightHeadlightBroken = vehicle.IsRightHeadLightBroken,
				AreHighBeamsOn = vehicle.AreHighBeamsOn,
				IsSearchLightOn = vehicle.IsSearchLightOn,

				// Fuel/Oil
				PetrolTankHealth = Math.Max(vehicle.PetrolTankHealth, (float)750),
				FuelLevel = vehicle.FuelLevel,
				OilLevel = vehicle.OilLevel,

				// Doors locks
				LockStatus = vehicle.LockStatus,

				// Alarm
				Alarm = vehicle.IsAlarmSet,

				// Engine
				EngineHealth = Math.Max(vehicle.EngineHealth, (float)20),
				IsEngineRunning = vehicle.IsEngineRunning,

				// Proofs
				CanTiresBurst = vehicle.CanTiresBurst,
				IsFireProof = vehicle.IsFireProof,
				IsExplosionProof = vehicle.IsExplosionProof,
				IsCollisionProof = vehicle.IsCollisionProof,
				IsMeleeProof = vehicle.IsMeleeProof,
				IsSteamProof = vehicle.IsSteamProof,
			};

			// Mod-color paint per slot (see VehicleData.PrimaryPaintType). The natives
			// only populate the outputs when a mod color is set, so an unset slot leaves
			// them holding garbage — validate the paint type into 0..7 and keep the
			// captured values only then, else the field stays -1 ("leave alone").
			try {
				OutputArgument ptOut = new OutputArgument();
				OutputArgument colOut = new OutputArgument();
				OutputArgument pearlOut = new OutputArgument();
				Function.Call(Hash.GET_VEHICLE_MOD_COLOR_1, vehicle, ptOut, colOut, pearlOut);
				int pt = ptOut.GetResult<int>();
				if (pt >= 0 && pt <= 7) {
					VehicleData.PrimaryPaintType = pt;
					VehicleData.PrimaryPaintColor = colOut.GetResult<int>();
					VehicleData.PrimaryPaintPearl = pearlOut.GetResult<int>();
				}

				OutputArgument pt2Out = new OutputArgument();
				OutputArgument col2Out = new OutputArgument();
				Function.Call(Hash.GET_VEHICLE_MOD_COLOR_2, vehicle, pt2Out, col2Out);
				int pt2 = pt2Out.GetResult<int>();
				if (pt2 >= 0 && pt2 <= 7) {
					VehicleData.SecondaryPaintType = pt2;
					VehicleData.SecondaryPaintColor = col2Out.GetResult<int>();
				}
			} catch (Exception e) {
				Logger.LogError(e.ToString());
			}

			// Other
			if (Function.Call<bool>(Hash.IS_VEHICLE_ATTACHED_TO_TRAILER, vehicle)) {
				OutputArgument trailerOutput = new OutputArgument();
				Function.Call<bool>(Hash.GET_VEHICLE_TRAILER_VEHICLE, vehicle, trailerOutput);
				VehicleData.TowedVehicle = (uint)trailerOutput.GetResult<Vehicle>().Model.Hash;
			}

			// Extras (togglable body parts) — only record the slots this model has.
			foreach (VehicleExtraIndex extra in Extras) {
				try {
					if (vehicle.Extras[extra].Exists()) {
						VehicleData.Extras.Add(new VehicleExtraData((int)extra, vehicle.Extras[extra].Enabled));
					}
				} catch (Exception e) {
					Logger.LogError(e.ToString());
				}
			}

			// Windows
			foreach (VehicleWindowIndex window in Windows) {
				try {
					VehicleData.Windows.Add(
						new VehicleWindowData(vehicle.Windows[window].Index, vehicle.Windows[window].IsIntact)
					);
				} catch (Exception e) {
					Logger.LogError(e.ToString());
				}
			}

			// Doors
			foreach (VehicleDoorIndex door in Doors) {
				try {
					VehicleData.Doors.Add(
						new VehicleDoorData(vehicle.Doors[door].Index, vehicle.Doors[door].IsBroken)
					);
				} catch (Exception e) {
					Logger.LogError(e.ToString());
				}
			}

			// Wheels
			foreach (VehicleWheelBoneId wheel in Wheels) {
				if (wheel > 0) {
					try {
						VehicleData.Wheels.Add(
							new VehicleWheelData(
								wheel,
								vehicle.Wheels[wheel].Health, vehicle.Wheels[wheel].TireHealth,
								vehicle.Wheels[wheel].IsBursted, vehicle.Wheels[wheel].IsPunctured
							)
						);
					} catch (Exception e) {
						Logger.LogError(e.ToString());
					}
				}
			}

			// Neon
			foreach (VehicleNeonLight neon in Neon) {
				VehicleData.Neon.Add(
					new VehicleNeonData(neon, vehicle.Mods.IsNeonLightsOn(neon))
				);
			}

			// Mods
			foreach (VehicleModType mod in Mods) {
				try {
					VehicleData.Mods.Add(
						new VehicleModData(mod, vehicle.Mods[mod].Index, vehicle.Mods[mod].Variation)
					);
				} catch (Exception e) {
					Logger.LogError(e.ToString());
				}
			}

			// Toggle mods
			foreach (VehicleToggleModType mod in ToggleMods) {
				try {
					VehicleData.ToggleMods.Add(
						new VehicleToggleModData(mod, vehicle.Mods[mod].IsInstalled)
					);
				} catch (Exception e) {
					Logger.LogError(e.ToString());
				}
			}

			VehicleData.ID = GetHashString($"{VehicleData.VehicleName} {VehicleData.LicensePlate.Trim()}");

			return VehicleData;
		}

		public static Vehicle CreateVehicleFromData(ref VehicleData data, bool nearby) {
			Vector3 spawnPosition;
			if (nearby) {
				spawnPosition = World.GetNextPositionOnStreet(Game.Player.Character.Position, true);
			} else {
				spawnPosition = data.Position;
			}

			// Stream the model in before spawning. DLC/rare models aren't always
			// resident, in which case World.CreateVehicle returns null; requesting
			// with a timeout makes the spawn reliable instead of intermittently failing.
			Model model = new Model((int)data.Vehicle);
			if (!model.IsInCdImage || !model.IsValid) {
				return null;
			}
			model.Request(1000);
			if (!model.IsLoaded) {
				return null;
			}

			Vehicle vehicle = World.CreateVehicle(model, spawnPosition);
			// Let the engine evict the model from memory once the vehicle exists.
			model.MarkAsNoLongerNeeded();
			if (vehicle == null) {
				// Spawn still failed (e.g. no room at the position).
				// Bail so the caller surfaces a clean failure instead of an NRE.
				return null;
			}
			data.Handle = vehicle.Handle;

			// Install the mod kit before any Mods.* write. Liveries, wheel type,
			// colors and per-slot mods are no-ops (or get reset) until the kit is
			// installed, which is what dropped roof mods and corrupted paint before.
			vehicle.Mods.InstallModKit();

			// General
			vehicle.IsPersistent = true;
			vehicle.DirtLevel = data.DirtLevel;
			vehicle.BodyHealth = data.BodyHealth;
			vehicle.Mods.Livery = data.Livery;
			vehicle.Mods.WindowTint = data.WindowTint;
			vehicle.Mods.WheelType = data.WheelType;

			vehicle.HeliEngineHealth = data.HeliEngineHealth;

			// Location
			vehicle.Rotation = data.Rotation;

			// Coloring. ColorCombination goes FIRST: a preset index (>= 0) reapplies
			// that preset's primary/secondary/pearlescent via SET_VEHICLE_COLOUR_
			// COMBINATION, which would clobber individually-set colors if it ran after
			// them. Applied first, the explicit colors below always win. (-1 means "no
			// preset, custom colors"; the native ignores it, so it's harmless.)
			vehicle.Mods.ColorCombination = data.ColorCombination;

			// Standard palette entries carry the finish (matte/metallic/pearlescent);
			// a custom RGB paint flattens it. So apply the standard colors first, then
			// override a slot with its custom RGB only when it was genuinely custom
			// (else matte/metallic paints turn into flat colors).
			vehicle.Mods.PrimaryColor = data.PrimaryColor;
			vehicle.Mods.SecondaryColor = data.SecondaryColor;
			vehicle.Mods.DashboardColor = data.DashboardColor;
			vehicle.Mods.PearlescentColor = data.PearlescentColor;
			vehicle.Mods.RimColor = data.RimColor;
			vehicle.Mods.TrimColor = data.TrimColor;
			if (data.IsPrimaryColorCustom) {
				vehicle.Mods.CustomPrimaryColor = data.CustomPrimaryColor;
			}
			if (data.IsSecondaryColorCustom) {
				vehicle.Mods.CustomSecondaryColor = data.CustomSecondaryColor;
			}
			vehicle.Mods.TireSmokeColor = data.TireSmokeColor;
			vehicle.Mods.NeonLightsColor = data.NeonLightsColor;

			// License plate
			vehicle.Mods.LicensePlate = data.LicensePlate;
			vehicle.Mods.LicensePlateStyle = data.LicensePlateStyle;

			// Lights
			vehicle.LightsMultiplier = data.LightsMultiplier;
			// The AreLightsOn setter is deprecated in favour of SetScriptedLightSetting,
			// but that forces a scripted override with different semantics; we only
			// want to restore the captured on/off bool, so keep the simple setter.
#pragma warning disable CS0618
			vehicle.AreLightsOn = data.AreLightsOn;
#pragma warning restore CS0618
			vehicle.IsLeftHeadLightBroken = data.IsLeftHeadlightBroken;
			vehicle.IsRightHeadLightBroken = data.IsRightHeadlightBroken;
			vehicle.AreHighBeamsOn = data.AreHighBeamsOn;
			vehicle.IsSearchLightOn = data.IsSearchLightOn;

			// Fuel/Oil
			vehicle.PetrolTankHealth = data.PetrolTankHealth;
			vehicle.FuelLevel = data.FuelLevel;
			vehicle.OilLevel = data.OilLevel;

			// Doors locks
			vehicle.LockStatus = data.LockStatus;

			// Alarm
			Function.Call<bool>(Hash.SET_VEHICLE_ALARM, vehicle, data.Alarm);

			// Engine
			vehicle.EngineHealth = data.EngineHealth;
			vehicle.IsUndriveable = false;
			Function.Call(Hash.SET_VEHICLE_ENGINE_ON, vehicle, data.IsEngineRunning, true, true);

			// Spawn with the radio off; the station isn't preserved (no reliable
			// per-vehicle read), and silence is the predictable default.
			Function.Call(Hash.SET_VEHICLE_RADIO_ENABLED, vehicle, false);

			// Proofs
			vehicle.IsBulletProof = data.IsBulletProof;
			vehicle.CanTiresBurst = data.CanTiresBurst;
			vehicle.IsFireProof = data.IsFireProof;
			vehicle.IsCollisionProof = data.IsCollisionProof;
			vehicle.IsExplosionProof = data.IsExplosionProof;
			vehicle.IsMeleeProof = data.IsMeleeProof;
			vehicle.IsSteamProof = data.IsSteamProof;

			// Other
			if (data.TowedVehicle != 0) {
				Vehicle trailer = World.CreateVehicle(new Model((int)data.TowedVehicle), data.Position + new Vector3(5f, 5f, 0f));
				if (trailer != null) {
					trailer.IsPersistent = true;
					Function.Call(Hash.ATTACH_VEHICLE_TO_TRAILER, vehicle, trailer);
				}
			}

			// Extras (togglable body parts)
			foreach (VehicleExtraData extra in data.Extras) {
				try {
					VehicleExtraIndex index = (VehicleExtraIndex)extra.Index;
					if (vehicle.Extras[index].Exists()) {
						vehicle.Extras[index].Enabled = extra.IsOn;
					}
				} catch (Exception e) {
					Logger.LogError(e.ToString());
				}
			}

			// Windows
			foreach (VehicleWindowData window in data.Windows) {
				try {
					if (!window.IsIntact) {
						vehicle.Windows[window.Index].Smash();
					}
				} catch (Exception e) {
					Logger.LogError(e.ToString());
				}
			}

			// Doors
			foreach (VehicleDoorData door in data.Doors) {
				try {
					if (door.IsBroken) {
						vehicle.Doors[door.Index].Break();
					}
				} catch (Exception e) {
					Logger.LogError(e.ToString());
				}
			}

			// Wheels
			foreach (VehicleWheelData wheel in data.Wheels) {
				try {
					if (wheel.IsPunctured) {
						vehicle.Wheels[wheel.Index].Puncture();
					} else if (wheel.IsBursted) {
						vehicle.Wheels[wheel.Index].Burst();
					} else {
						vehicle.Wheels[wheel.Index].Health = wheel.Health;
						vehicle.Wheels[wheel.Index].TireHealth = wheel.TireHealth;
					}
				} catch (Exception e) {
					Logger.LogError(e.ToString());
				}
			}

			// Neon status
			foreach (VehicleNeonData neon in data.Neon) {
				vehicle.Mods.SetNeonLightsOn(neon.Index, neon.Enabled);
			}

			// Mods
			foreach (VehicleModData mod in data.Mods) {
				try {
					vehicle.Mods[mod.Type].Index = mod.Index;
					vehicle.Mods[mod.Type].Variation = mod.Variation;
				} catch (Exception e) {
					Logger.LogError(e.ToString());
				}
			}

			// Toggle mods
			foreach (VehicleToggleModData mod in data.ToggleMods) {
				try {
					vehicle.Mods[mod.Type].IsInstalled = mod.IsInstalled;
				} catch (Exception e) {
					Logger.LogError(e.ToString());
				}
			}

			// Re-assert the extra-colours pair (pearlescent + rim) after the mods above:
			// they share SET_VEHICLE_EXTRA_COLOURS, and applying mods can reset that pair
			// to default. Primary/secondary use a different native and aren't affected.
			vehicle.Mods.PearlescentColor = data.PearlescentColor;
			vehicle.Mods.RimColor = data.RimColor;

			// Mod-color paint per slot (chrome/matte/metallic/…), which the VehicleColor
			// setters don't carry. Applied after the mods and the pearl/rim re-assert so
			// nothing resets it. Feed back the EXACT captured (paintType, colorIndex,
			// pearl) — the colorIndex is a mod-color number, not a VehicleColor. -1 type
			// means "wasn't a mod color", so leave the slot to the VehicleColor path.
			if (data.PrimaryPaintType >= 0) {
				Function.Call(Hash.SET_VEHICLE_MOD_COLOR_1, vehicle, data.PrimaryPaintType, data.PrimaryPaintColor, data.PrimaryPaintPearl);
			}
			if (data.SecondaryPaintType >= 0) {
				Function.Call(Hash.SET_VEHICLE_MOD_COLOR_2, vehicle, data.SecondaryPaintType, data.SecondaryPaintColor);
			}

			// Xenon color must follow the xenon toggle mod above. -1 is stock; only
			// apply an explicit override so we don't force a color on stock xenons.
			if (data.XenonColorIndex >= 0) {
				Function.Call(Hash.SET_VEHICLE_XENON_LIGHT_COLOR_INDEX, vehicle, data.XenonColorIndex);
			}

			// Roof open/closed state goes last: on convertibles the roof is also a
			// mod slot, so set the state after the mods are applied or it gets reset.
			vehicle.RoofState = data.RoofState;

			return vehicle;
		}
	}
}