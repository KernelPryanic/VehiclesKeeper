using System.Collections.Generic;
using System.Xml.Serialization;
using GTA;
using GTA.Math;

namespace VehicleKeeper {
	// All DTO types below are persisted with the framework's XmlSerializer (no
	// third-party JSON dependency). XmlSerializer requires a public parameterless
	// constructor and public settable members on every serialized type, so each
	// nested class keeps a parameterless ctor alongside the convenience ctor the
	// capture code uses.

	public class VehicleWindowData {
		public VehicleWindowData() { }
		public VehicleWindowData(VehicleWindowIndex index, bool isIntact) {
			Index = index;
			IsIntact = isIntact;
		}
		public VehicleWindowIndex Index { get; set; }
		public bool IsIntact { get; set; }
	}

	public class VehicleDoorData {
		public VehicleDoorData() { }
		public VehicleDoorData(VehicleDoorIndex index, bool isBroken) {
			Index = index;
			IsBroken = isBroken;
		}
		public VehicleDoorIndex Index { get; set; }
		public bool IsBroken { get; set; }
	}

	public class VehicleWheelData {
		public VehicleWheelData() { }
		public VehicleWheelData(
			VehicleWheelBoneId index,
			float health,
			float tireHealth,
			bool isBursted,
			bool isPunctured
		) {
			Index = index;
			Health = health;
			TireHealth = tireHealth;
			IsBursted = isBursted;
			IsPunctured = isPunctured;
		}
		public VehicleWheelBoneId Index { get; set; }
		public float Health { get; set; }
		public float TireHealth { get; set; }
		public bool IsBursted { get; set; }
		public bool IsPunctured { get; set; }
	}

	public class VehicleNeonData {
		public VehicleNeonData() { }
		public VehicleNeonData(VehicleNeonLight index, bool enabled) {
			Index = index;
			Enabled = enabled;
		}
		public VehicleNeonLight Index { get; set; }
		public bool Enabled { get; set; }
	}

	public class VehicleExtraData {
		public VehicleExtraData() { }
		public VehicleExtraData(int index, bool isOn) {
			Index = index;
			IsOn = isOn;
		}
		public int Index { get; set; }
		public bool IsOn { get; set; }
	}

	public class VehicleModData {
		public VehicleModData() { }
		public VehicleModData(VehicleModType type, int index, bool variation) {
			Type = type;
			Index = index;
			Variation = variation;
		}
		public VehicleModType Type { get; set; }
		public int Index { get; set; }
		public bool Variation { get; set; }
	}

	public class VehicleToggleModData {
		public VehicleToggleModData() { }
		public VehicleToggleModData(VehicleToggleModType type, bool isInstalled) {
			Type = type;
			IsInstalled = isInstalled;
		}
		public VehicleToggleModType Type { get; set; }
		public bool IsInstalled { get; set; }
	}

	public class VehicleData {
		public override bool Equals(object obj) {
			if (!(obj is VehicleData other)) return false;
			return ID == other.ID;
		}

		public override int GetHashCode() {
			return ID.GetHashCode();
		}

		// General
		public string ID { get; set; }
		public int Handle { get; set; }
		public string VehicleName { get; set; }
		public uint Vehicle { get; set; }
		public float DirtLevel { get; set; }
		public float BodyHealth { get; set; }
		public int Livery { get; set; }
		public VehicleWindowTint WindowTint { get; set; }
		public VehicleRoofState RoofState { get; set; }
		public VehicleWheelType WheelType { get; set; }

		public float HeliEngineHealth { get; set; }

		// Location
		public Vector3 Position { get; set; }
		public Vector3 Rotation { get; set; }

		// Coloring
		public VehicleColor PrimaryColor { get; set; }
		public VehicleColor SecondaryColor { get; set; }
		// Mod-color paint per slot: the (paintType, colorIndex[, pearl]) triple from
		// GET_VEHICLE_MOD_COLOR_1/_2, which carries finishes VehicleColor can't
		// express (chrome, matte, worn, …). These are the game's mod-color numbers,
		// a different space from VehicleColor — restore must feed back these exact
		// values, not the enum. PaintType -1 means "not a mod color", so restore
		// leaves the slot alone and the VehicleColor path fully governs it.
		public int PrimaryPaintType { get; set; } = -1;
		public int PrimaryPaintColor { get; set; } = -1;
		public int PrimaryPaintPearl { get; set; } = -1;
		public int SecondaryPaintType { get; set; } = -1;
		public int SecondaryPaintColor { get; set; } = -1;
		public VehicleColor DashboardColor { get; set; }
		public VehicleColor PearlescentColor { get; set; }
		public VehicleColor RimColor { get; set; }
		public VehicleColor TrimColor { get; set; }
		public int ColorCombination { get; set; }
		// Xenon headlight tint. The on/off state is a toggle mod (saved with the
		// other toggle mods); only the color index needs its own field. -1 means
		// the stock/default xenon color (no override).
		public int XenonColorIndex { get; set; }
		// Whether each slot uses a custom RGB paint vs. a standard palette color.
		// Standard palette entries carry the finish (matte/metallic/pearlescent);
		// forcing a custom RGB over a standard paint flattens that finish, so the
		// restore must branch on these flags.
		public bool IsPrimaryColorCustom { get; set; }
		public bool IsSecondaryColorCustom { get; set; }

		// System.Drawing.Color is not round-tripped cleanly by XmlSerializer, so each
		// color is serialized as a 32-bit ARGB int and the Color property is XmlIgnored.
		// The capture/restore code keeps using the Color properties; only the on-disk
		// representation differs.
		[XmlIgnore] public System.Drawing.Color CustomPrimaryColor { get; set; }
		[XmlIgnore] public System.Drawing.Color CustomSecondaryColor { get; set; }
		[XmlIgnore] public System.Drawing.Color TireSmokeColor { get; set; }
		[XmlIgnore] public System.Drawing.Color NeonLightsColor { get; set; }

		[XmlElement("CustomPrimaryColor")]
		public int CustomPrimaryColorArgb {
			get => CustomPrimaryColor.ToArgb();
			set => CustomPrimaryColor = System.Drawing.Color.FromArgb(value);
		}
		[XmlElement("CustomSecondaryColor")]
		public int CustomSecondaryColorArgb {
			get => CustomSecondaryColor.ToArgb();
			set => CustomSecondaryColor = System.Drawing.Color.FromArgb(value);
		}
		[XmlElement("TireSmokeColor")]
		public int TireSmokeColorArgb {
			get => TireSmokeColor.ToArgb();
			set => TireSmokeColor = System.Drawing.Color.FromArgb(value);
		}
		[XmlElement("NeonLightsColor")]
		public int NeonLightsColorArgb {
			get => NeonLightsColor.ToArgb();
			set => NeonLightsColor = System.Drawing.Color.FromArgb(value);
		}

		// License plate
		public string LicensePlate { get; set; }
		public LicensePlateStyle LicensePlateStyle { get; set; }

		// Lights
		public float LightsMultiplier { get; set; }
		public bool AreLightsOn { get; set; }
		public bool IsLeftHeadlightBroken { get; set; }
		public bool IsRightHeadlightBroken { get; set; }
		public bool AreHighBeamsOn { get; set; }
		public bool IsSearchLightOn { get; set; }

		// Fuel
		public float PetrolTankHealth { get; set; }
		public float FuelLevel { get; set; }
		public float OilLevel { get; set; }

		// Door locks
		public VehicleLockStatus LockStatus { get; set; }

		// Alarm
		public bool Alarm { get; set; }

		// Engine
		public float EngineHealth { get; set; }
		public bool IsEngineRunning { get; set; }
		public bool IsDriveable { get; set; }

		// Proofs
		public bool IsBulletProof { get; set; }
		// Bulletproof tires: CanTiresBurst == false means tires can't be shot out.
		public bool CanTiresBurst { get; set; }
		public bool IsFireProof { get; set; }
		public bool IsExplosionProof { get; set; }
		public bool IsCollisionProof { get; set; }
		public bool IsMeleeProof { get; set; }
		public bool IsSteamProof { get; set; }

		// Other
		public uint TowedVehicle { get; set; }

		public List<VehicleExtraData> Extras { get; set; } = new List<VehicleExtraData>();
		public List<VehicleWindowData> Windows { get; set; } = new List<VehicleWindowData>();
		public List<VehicleDoorData> Doors { get; set; } = new List<VehicleDoorData>();
		public List<VehicleWheelData> Wheels { get; set; } = new List<VehicleWheelData>();
		public List<VehicleNeonData> Neon { get; set; } = new List<VehicleNeonData>();
		public List<VehicleModData> Mods { get; set; } = new List<VehicleModData>();
		public List<VehicleToggleModData> ToggleMods { get; set; } = new List<VehicleToggleModData>();
	}
}
