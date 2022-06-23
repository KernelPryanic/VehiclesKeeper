using System.Collections.Generic;
using GTA;
using GTA.Math;

namespace VehicleKeeper {
    public class VehicleWindowData {
        public VehicleWindowData(VehicleWindowIndex index, bool isIntact) {
            Index = index;
            IsIntact = isIntact;
        }
        public VehicleWindowIndex Index { get; set; }
        public bool IsIntact { get; set; }
    }

    public class VehicleDoorData {
        public VehicleDoorData(VehicleDoorIndex index, bool isBroken) {
            Index = index;
            IsBroken = isBroken;
        }
        public VehicleDoorIndex Index { get; set; }
        public bool IsBroken { get; set; }
    }

    public class VehicleWheelData {
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
        public VehicleNeonData(VehicleNeonLight index, bool enabled) {
            Index = index;
            Enabled = enabled;
        }
        public VehicleNeonLight Index { get; set; }
        public bool Enabled { get; set; }
    }

    public class VehicleModData {
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
        public VehicleToggleModData(VehicleToggleModType type, bool isInstalled) {
            Type = type;
            IsInstalled = isInstalled;
        }
        public VehicleToggleModType Type { get; set; }
        public bool IsInstalled { get; set; }
    }

    public class VehicleData {
        public override bool Equals(object obj) {
            if (obj.GetType() != typeof(VehicleData)) return false;
            return this.ID == ((VehicleData) obj).ID;
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
        public bool IsBulletProof { get; set; }

        public float HeliEngineHealth { get; set; }

        // Radio
        // public RadioStation RadioStation { get; set; }

        // Location
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }

        // Engine
        public float EngineHealth { get; set; }
        public bool IsEngineRunning { get; set; }
        public bool IsDriveable { get; set; }

        // Coloring
        public VehicleColor PrimaryColor { get; set; }
        public VehicleColor SecondaryColor { get; set; }
        public VehicleColor DashboardColor { get; set; }
        public VehicleColor PearlescentColor { get; set; }
        public VehicleColor RimColor { get; set; }
        public VehicleColor TrimColor { get; set; }
        public System.Drawing.Color CustomPrimaryColor { get; set; }
        public System.Drawing.Color CustomSecondaryColor { get; set; }
        public System.Drawing.Color TireSmokeColor { get; set; }
        public System.Drawing.Color NeonLightsColor { get; set; }
        public int ColorCombination { get; set; }

        // License plate
        public string LicensePlate { get; set; }
        public LicensePlateStyle LicensePlateStyle { get; set; }

        // Lights
        public bool IsLeftHeadlightBroken { get; set; }
        public bool IsRightHeadlightBroken { get; set; }
        public bool AreLightsOn { get; set; }
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

        // Bumpers
        // public bool FrontBumperBrokenOff { get; set; }
        // public bool RearBumperBrokenOff { get; set; }

        // Other
        public uint TowedVehicle { get; set; }

        public List<VehicleWindowData> Windows { get; set; } = new List<VehicleWindowData>();
        public List<VehicleDoorData> Doors { get; set; } = new List<VehicleDoorData>();
        public List<VehicleWheelData> Wheels { get; set; } = new List<VehicleWheelData>();
        public List<VehicleNeonData> Neon { get; set; } = new List<VehicleNeonData>();
        public List<VehicleModData> Mods { get; set; } = new List<VehicleModData>();
        public List<VehicleToggleModData> ToggleMods { get; set; } = new List<VehicleToggleModData>();
    }
}