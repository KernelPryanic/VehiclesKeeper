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

    public class VehicleData {
        public override bool Equals(object obj) {
            if (obj.GetType() != typeof(VehicleData)) return false;
            return this.ID == ((VehicleData) obj).ID;
        }

        public override int GetHashCode() {
            return ID.GetHashCode();
        }

        public string ID { get; set; }

        public int Handle { get; set; }

        public string VehicleName { get; set; }

        public uint Vehicle { get; set; }

        // public float DirtLevel { get; set; }

        // public VehicleColor PrimaryColor { get; set; }

        // public VehicleColor SecondaryColor { get; set; }

        // public VehicleColor DashboardColor { get; set; }

        // public VehicleColor PearlescentColor { get; set; }

        // public VehicleColor RimColor { get; set; }

        // public VehicleColor TrimColor { get; set; }

        // public System.Drawing.Color CustomPrimaryColor { get; set; }

        // public System.Drawing.Color CustomSecondaryColor { get; set; }

        // public System.Drawing.Color TireSmokeColor { get; set; }

        // public System.Drawing.Color NeonLightsColor { get; set; }

        // public int ColorCombination { get; set; }

        public Vector3 Position { get; set; }

        // public Vector3 Rotation { get; set; }

        // public float BodyHealth { get; set; }

        // public float EngineHealth { get; set; }

        // public bool EngineRunning { get; set; }

        // public bool IsDriveable { get; set; }

        // public bool LeftHeadlightBroken { get; set; }

        // public bool RightHeadlightBroken { get; set; }

        // public bool LightsOn { get; set; }

        // public bool HighBeamsOn { get; set; }

        // public bool SearchLightOn { get; set; }

        // public int Livery { get; set; }

        // public bool FrontBumperBrokenOff { get; set; }

        // public bool RearBumperBrokenOff { get; set; }

        // public int LockStatus { get; set; }

        // public bool Alarm { get; set; }

        // public float PetrolTankHealth { get; set; }

        // public float FuelLevel { get; set; }

        // public uint TowedVehicle { get; set; }

        // public VehicleWindowTint WindowTint { get; set; }

        // public VehicleRoofState RoofState { get; set; }

        // public VehicleWheelType WheelType { get; set; }

        // public LicensePlateType NumberPlateType { get; set; }

        public string NumberPlate { get; set; }

        public List<VehicleWindowData> Windows { get; set; } = new List<VehicleWindowData>();

        // public bool[] Neon { get; set; } = new bool[4];

        // public List<bool> Doors { get; set; } = new List<bool>();

        //  public List<bool> Tires { get; set; } = new List<bool>();

        // public List<VehicleMod> Mods { get; set; } = new List<VehicleMod>();
        // public List<VehicleToggleMod> ToggleMods { get; set; } = new List<VehicleToggleMod>();
    }
}