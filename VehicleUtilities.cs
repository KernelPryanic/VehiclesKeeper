using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using GTA;
using GTA.Math;
using GTA.Native;

namespace VehicleKeeper {
    public static class VehicleUtilities {
        static VehicleWindowIndex[] windows = (VehicleWindowIndex[]) Enum.GetValues(typeof(VehicleWindowIndex));
        static VehicleNeonLight[] neons = (VehicleNeonLight[]) Enum.GetValues(typeof(VehicleNeonLight));
        static VehicleModType[] mods = (VehicleModType[]) Enum.GetValues(typeof(VehicleModType));
        static VehicleToggleModType[] toggleMods = (VehicleToggleModType[]) Enum.GetValues(typeof(VehicleToggleModType));

        public static byte[] GetHash(string input) {
            HashAlgorithm algorithm = SHA256.Create();
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
        }

        public static string GetHashString(string input) {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetHash(input))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }

        public static VehicleData CreateInfo(Vehicle vehicle) {
            VehicleData VehicleData = new VehicleData();

            VehicleData.Handle = vehicle.Handle;
            VehicleData.VehicleName = vehicle.DisplayName;
            VehicleData.Vehicle = (uint) vehicle.Model.Hash;

            // // General
            // VehicleData.DirtLevel = vehicle.DirtLevel;
            // VehicleData.BodyHealth = vehicle.BodyHealth;
            // VehicleData.Livery = vehicle.Livery;
            // VehicleData.WindowTint = vehicle.WindowTint;
            // VehicleData.RoofState = vehicle.RoofState;
            // VehicleData.WheelType = vehicle.WheelType;

            // // Position
            VehicleData.Position = vehicle.Position;
            // VehicleData.Rotation = vehicle.Rotation;

            // // Engine
            // VehicleData.EngineHealth = vehicle.EngineHealth;
            // VehicleData.EngineRunning = vehicle.EngineRunning;
            // VehicleData.IsDriveable = vehicle.IsDriveable;

            // // Colors
            // VehicleData.PrimaryColor = vehicle.PrimaryColor;
            // VehicleData.SecondaryColor = vehicle.SecondaryColor;
            // VehicleData.DashboardColor = vehicle.DashboardColor;
            // VehicleData.PearlescentColor = vehicle.PearlescentColor;
            // VehicleData.RimColor = vehicle.RimColor;
            // VehicleData.TrimColor = vehicle.TrimColor;
            // VehicleData.CustomPrimaryColor = vehicle.CustomPrimaryColor;
            // VehicleData.CustomSecondaryColor = vehicle.CustomSecondaryColor;
            // VehicleData.TireSmokeColor = vehicle.TireSmokeColor;
            // VehicleData.NeonLightsColor = vehicle.NeonLightsColor;
            // VehicleData.ColorCombination = vehicle.ColorCombination;

            // // Number plate
            // VehicleData.NumberPlateType = vehicle.Mods.LicensePlateType;
            VehicleData.NumberPlate = vehicle.Mods.LicensePlate;

            // // Lights status
            // VehicleData.LeftHeadlightBroken = vehicle.LeftHeadLightBroken;
            // VehicleData.RightHeadlightBroken = vehicle.RightHeadLightBroken;
            // VehicleData.LightsOn = vehicle.LightsOn;
            // VehicleData.HighBeamsOn = vehicle.HighBeamsOn;
            // VehicleData.SearchLightOn = vehicle.SearchLightOn;

            // // Bumpers status
            // VehicleData.FrontBumperBrokenOff = vehicle.IsFrontBumperBrokenOff;
            // VehicleData.RearBumperBrokenOff = vehicle.IsRearBumperBrokenOff;

            // // Fuel status
            // VehicleData.PetrolTankHealth = vehicle.PetrolTankHealth;
            // VehicleData.FuelLevel = vehicle.FuelLevel;

            // // Doors lock status
            // VehicleData.LockStatus = (int) vehicle.LockStatus;

            // // Alarm status
            // VehicleData.Alarm = vehicle.IsAlarmSet;

            // // Towed vehicle model
            // OutputArgument trailerOutput = new OutputArgument();
            // Function.Call<bool>(Hash.GET_VEHICLE_TRAILER_VEHICLE, vehicle, trailerOutput);
            // VehicleData.TowedVehicle = (uint) trailerOutput.GetResult<Vehicle>().Model.Hash;

            // Windows status
            foreach (VehicleWindowIndex w in windows) {
                try {
                    VehicleData.Windows.Add(new VehicleWindowData(vehicle.Windows[w].Index, vehicle.Windows[w].IsIntact));
                } catch (Exception e) {
                    Logger.Log(e.Message);
                    GTA.UI.Notification.Show(e.Message);
                }
            }

            // // Doors status
            // foreach (VehicleDoor door in vehicle.Doors) {
            //     VehicleData.Doors.Add(door.IsBroken);
            // }

            // // Tires status
            // foreach (VehicleWheel wheel in vehicle.Wheels) {
            //     VehicleData.Tires.Add(wheel.IsBursted);
            // }

            // // Neon status
            // for (int i = 0; i < 4; i++) {
            //     VehicleData.Neon[i] = vehicle.IsNeonLightsOn(neons[i]);
            // }

            // // Mods
            // foreach (VehicleModType m in mods) {
            //     VehicleData.Mods.Add(vehicle.Mods[m]);
            // }

            // // Toggle mods
            // foreach (VehicleToggleModType m in toggleMods) {
            //     VehicleData.ToggleMods.Add(vehicle.Mods[m]);
            // }

            VehicleData.ID = GetHashString($"{VehicleData.VehicleName} {VehicleData.NumberPlate.Trim()}");

            return VehicleData;
        }

        public static Vehicle CreateVehicleFromData(ref VehicleData data) {
            Vehicle vehicle = World.CreateVehicle(new Model((int) data.Vehicle), data.Position);
            data.Handle = vehicle.Handle;

            // // General
            // vehicle.DirtLevel = data.DirtLevel;
            // vehicle.BodyHealth = data.BodyHealth;
            // vehicle.Livery = data.Livery;
            // vehicle.WindowTint = data.WindowTint;
            // vehicle.RoofState = data.RoofState;
            // vehicle.WheelType = data.WheelType;
            // vehicle.IsPersistent = true;

            // // Position
            // vehicle.Position = data.Position;
            // vehicle.Rotation = data.Rotation;

            // // Engine
            // vehicle.EngineHealth = data.EngineHealth;
            // vehicle.EngineRunning = data.EngineRunning;
            // vehicle.IsDriveable = data.IsDriveable;

            // // Colors
            // vehicle.PrimaryColor = data.PrimaryColor;
            // vehicle.SecondaryColor = data.SecondaryColor;
            // vehicle.DashboardColor = data.DashboardColor;
            // vehicle.PearlescentColor = data.PearlescentColor;
            // vehicle.RimColor = data.RimColor;
            // vehicle.TrimColor = data.TrimColor;
            // vehicle.CustomPrimaryColor = data.CustomPrimaryColor;
            // vehicle.CustomSecondaryColor = data.CustomSecondaryColor;
            // vehicle.TireSmokeColor = data.TireSmokeColor;
            // vehicle.NeonLightsColor = data.NeonLightsColor;
            // vehicle.ColorCombination = data.ColorCombination;

            // Number plate
            // vehicle.Mods.LicensePlateType = data.NumberPlateType;
            vehicle.Mods.LicensePlate = data.NumberPlate;

            // // Lights status
            // vehicle.LeftHeadLightBroken = data.LeftHeadlightBroken;
            // vehicle.RightHeadLightBroken = data.RightHeadlightBroken;
            // vehicle.LightsOn = data.LightsOn;
            // vehicle.HighBeamsOn = data.HighBeamsOn;
            // vehicle.SearchLightOn = data.SearchLightOn;

            // // Fuel status
            // vehicle.PetrolTankHealth = data.PetrolTankHealth;
            // vehicle.FuelLevel = data.FuelLevel;

            // // Doors lock status
            // vehicle.LockStatus = (VehicleLockStatus) data.LockStatus;

            // // Alarm status
            // Function.Call<bool>(Hash._0xCDE5E70C1DDB954C, new InputArgument[2] { vehicle, data.Alarm });

            // // Towed vehicle
            // if (data.TowedVehicle != 0) {
            //     Vehicle trailer = World.CreateVehicle(new Model((int) data.TowedVehicle), data.Position + new Vector3(5f, 5f, 0f));
            //     trailer.IsPersistent = true;
            //     Function.Call(Hash.ATTACH_VEHICLE_TO_TRAILER, new InputArgument[2] { vehicle, trailer });
            // }

            vehicle.Mods.InstallModKit();

            // Windows status
            foreach (VehicleWindowData window in data.Windows) {
                try {
                    if (!window.IsIntact) {
                        vehicle.Windows[window.Index].Smash();
                    }
                } catch (Exception e) {
                    Logger.Log(e.Message);
                    GTA.UI.Notification.Show(e.Message);
                }
            }

            // // Doors status
            // int idx = 0;
            // foreach (VehicleDoor door in vehicle.GetDoors()) {
            //     if (data.Doors[idx]) {
            //         vehicle.BreakDoor(door);
            //     }
            //     idx++;
            // }

            // // Tires status
            // for (int i = 0; i < 6; i++) {
            //     if (data.Tires[i]) {
            //         vehicle.BurstTire(i);
            //     }
            // }

            // // Bumpers status
            // Action<string> BreakBumper = x => {
            //     int value = Function.Call<int>(Hash._0xFB71170B7E76ACBA, new InputArgument[2] {
            //         vehicle,
            //         x
            //     });
            //     Vector3 loc = Function.Call<Vector3>(Hash._0x44A8FCB8ED227738, new InputArgument[2] {
            //         vehicle,
            //         value
            //     });
            //     vehicle.ApplyDamage(loc, 1000f, 10f);
            // };
            // if (data.FrontBumperBrokenOff) {
            //     BreakBumper("bumper_f");
            // }
            // if (data.RearBumperBrokenOff) {
            //     BreakBumper("bumper_r");
            // }

            // // Neon status
            // for (int i = 0; i < 4; i++) {
            //     vehicle.SetNeonLightsOn(neons[i], on : data.Neon[i]);
            // }

            // // Mods
            // for (int i = 0; i < mods.Length; i++) {
            //     vehicle.SetMod(mods[i], data.Mods[i], variations : false);
            // }

            // // Toggle mods
            // for (int i = 0; i < toggleMods.Length; i++) {
            //     vehicle.ToggleMod(toggleMods[i], data.ToggleMods[i]);
            // }

            return vehicle;
        }
    }
}