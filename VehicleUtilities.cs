using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using GTA;
using GTA.Math;
using GTA.Native;

namespace VehicleKeeper {
    public static class VehicleUtilities {
        static VehicleNeonLight[] neons = (VehicleNeonLight[]) Enum.GetValues(typeof(VehicleNeonLight));
        static VehicleMod[] mods = (VehicleMod[]) Enum.GetValues(typeof(VehicleMod));
        static VehicleToggleMod[] toggleMods = (VehicleToggleMod[]) Enum.GetValues(typeof(VehicleToggleMod));

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

        public static SaveableVehicle CreateInfo(Vehicle vehicle) {
            SaveableVehicle saveableVehicle = new SaveableVehicle();

            saveableVehicle.Handle = vehicle.Handle;
            saveableVehicle.VehicleName = vehicle.DisplayName;
            saveableVehicle.Vehicle = (uint) vehicle.Model.Hash;

            // General
            saveableVehicle.DirtLevel = vehicle.DirtLevel;
            saveableVehicle.BodyHealth = vehicle.BodyHealth;
            saveableVehicle.Livery = vehicle.Livery;
            saveableVehicle.WindowTint = vehicle.WindowTint;
            saveableVehicle.RoofState = vehicle.RoofState;
            saveableVehicle.WheelType = vehicle.WheelType;

            // Position
            saveableVehicle.Position = vehicle.Position;
            saveableVehicle.Rotation = vehicle.Rotation;

            // Engine
            saveableVehicle.EngineHealth = vehicle.EngineHealth;
            saveableVehicle.EngineRunning = vehicle.EngineRunning;
            saveableVehicle.IsDriveable = vehicle.IsDriveable;

            // Colors
            saveableVehicle.PrimaryColor = vehicle.PrimaryColor;
            saveableVehicle.SecondaryColor = vehicle.SecondaryColor;
            saveableVehicle.DashboardColor = vehicle.DashboardColor;
            saveableVehicle.PearlescentColor = vehicle.PearlescentColor;
            saveableVehicle.RimColor = vehicle.RimColor;
            saveableVehicle.TrimColor = vehicle.TrimColor;
            saveableVehicle.CustomPrimaryColor = vehicle.CustomPrimaryColor;
            saveableVehicle.CustomSecondaryColor = vehicle.CustomSecondaryColor;
            saveableVehicle.TireSmokeColor = vehicle.TireSmokeColor;
            saveableVehicle.NeonLightsColor = vehicle.NeonLightsColor;
            saveableVehicle.ColorCombination = vehicle.ColorCombination;

            // Number plate
            saveableVehicle.NumberPlateType = vehicle.NumberPlateType;
            saveableVehicle.NumberPlate = vehicle.NumberPlate;

            // Lights status
            saveableVehicle.LeftHeadlightBroken = vehicle.LeftHeadLightBroken;
            saveableVehicle.RightHeadlightBroken = vehicle.RightHeadLightBroken;
            saveableVehicle.LightsOn = vehicle.LightsOn;
            saveableVehicle.HighBeamsOn = vehicle.HighBeamsOn;
            saveableVehicle.SearchLightOn = vehicle.SearchLightOn;

            // Bumpers status
            saveableVehicle.FrontBumperBrokenOff = vehicle.IsFrontBumperBrokenOff;
            saveableVehicle.RearBumperBrokenOff = vehicle.IsRearBumperBrokenOff;

            // Fuel status
            saveableVehicle.PetrolTankHealth = vehicle.PetrolTankHealth;
            saveableVehicle.FuelLevel = vehicle.FuelLevel;

            // Doors lock status
            saveableVehicle.LockStatus = (int) vehicle.LockStatus;

            // Alarm status
            saveableVehicle.Alarm = Function.Call<bool>(Hash._0x4319E335B71FFF34, new InputArgument[1] { vehicle });

            // Towed vehicle model
            OutputArgument trailerOutput = new OutputArgument();
            Function.Call<bool>(Hash.GET_VEHICLE_TRAILER_VEHICLE, vehicle, trailerOutput);
            saveableVehicle.TowedVehicle = (uint) trailerOutput.GetResult<Vehicle>().Model.Hash;

            // Windows status
            for (int i = 0; i < 4; i++) {
                saveableVehicle.Windows[i] = !Function.Call<bool>(Hash._0x46E571A0E20D01F1, new InputArgument[2] { vehicle, i });
            }

            // Doors status
            foreach (VehicleDoor door in vehicle.GetDoors()) {
                saveableVehicle.Doors.Add(vehicle.IsDoorBroken(door));
            }

            // Tires status
            for (int i = 0; i < 6; i++) {
                saveableVehicle.Tires[i] = vehicle.IsTireBurst(i);
            }

            // Neon status
            for (int i = 0; i < 4; i++) {
                saveableVehicle.Neon[i] = vehicle.IsNeonLightsOn(neons[i]);
            }

            // Mods
            foreach (VehicleMod m in mods) {
                saveableVehicle.Mods.Add(vehicle.GetMod(m));
            }

            // Toggle mods
            foreach (VehicleToggleMod m in toggleMods) {
                saveableVehicle.ToggleMods.Add(vehicle.IsToggleModOn(m));
            }

            saveableVehicle.ID = GetHashString($"{saveableVehicle.VehicleName} {saveableVehicle.NumberPlate.Trim()}");

            return saveableVehicle;
        }

        public static Vehicle CreateVehicleFromData(ref SaveableVehicle data) {
            Vehicle vehicle = World.CreateVehicle(new Model((int) data.Vehicle), data.Position);
            data.Handle = vehicle.Handle;

            // General
            vehicle.DirtLevel = data.DirtLevel;
            vehicle.BodyHealth = data.BodyHealth;
            vehicle.Livery = data.Livery;
            vehicle.WindowTint = data.WindowTint;
            vehicle.RoofState = data.RoofState;
            vehicle.WheelType = data.WheelType;
            vehicle.IsPersistent = true;

            // Position
            vehicle.Position = data.Position;
            vehicle.Rotation = data.Rotation;

            // Engine
            vehicle.EngineHealth = data.EngineHealth;
            vehicle.EngineRunning = data.EngineRunning;
            vehicle.IsDriveable = data.IsDriveable;

            // Colors
            vehicle.PrimaryColor = data.PrimaryColor;
            vehicle.SecondaryColor = data.SecondaryColor;
            vehicle.DashboardColor = data.DashboardColor;
            vehicle.PearlescentColor = data.PearlescentColor;
            vehicle.RimColor = data.RimColor;
            vehicle.TrimColor = data.TrimColor;
            vehicle.CustomPrimaryColor = data.CustomPrimaryColor;
            vehicle.CustomSecondaryColor = data.CustomSecondaryColor;
            vehicle.TireSmokeColor = data.TireSmokeColor;
            vehicle.NeonLightsColor = data.NeonLightsColor;
            vehicle.ColorCombination = data.ColorCombination;

            // Number plate
            vehicle.NumberPlateType = data.NumberPlateType;
            vehicle.NumberPlate = data.NumberPlate;

            // Lights status
            vehicle.LeftHeadLightBroken = data.LeftHeadlightBroken;
            vehicle.RightHeadLightBroken = data.RightHeadlightBroken;
            vehicle.LightsOn = data.LightsOn;
            vehicle.HighBeamsOn = data.HighBeamsOn;
            vehicle.SearchLightOn = data.SearchLightOn;

            // Fuel status
            vehicle.PetrolTankHealth = data.PetrolTankHealth;
            vehicle.FuelLevel = data.FuelLevel;

            // Doors lock status
            vehicle.LockStatus = (VehicleLockStatus) data.LockStatus;

            // Alarm status
            Function.Call<bool>(Hash._0xCDE5E70C1DDB954C, new InputArgument[2] { vehicle, data.Alarm });

            // Towed vehicle
            if (data.TowedVehicle != 0) {
                Vehicle trailer = World.CreateVehicle(new Model((int) data.TowedVehicle), data.Position + new Vector3(5f, 5f, 0f));
                trailer.IsPersistent = true;
                Function.Call(Hash.ATTACH_VEHICLE_TO_TRAILER, new InputArgument[2] { vehicle, trailer });
            }

            vehicle.InstallModKit();

            // Windows status
            for (int i = 0; i < 4; i++) {
                if (data.Windows[i]) {
                    vehicle.SmashWindow((VehicleWindow) i);
                }
            }

            // Doors status
            int idx = 0;
            foreach (VehicleDoor door in vehicle.GetDoors()) {
                if (data.Doors[idx]) {
                    vehicle.BreakDoor(door);
                }
                idx++;
            }

            // Tires status
            for (int i = 0; i < 6; i++) {
                if (data.Tires[i]) {
                    vehicle.BurstTire(i);
                }
            }

            // Bumpers status
            Action<string> BreakBumper = x => {
                int value = Function.Call<int>(Hash._0xFB71170B7E76ACBA, new InputArgument[2] {
                    vehicle,
                    x
                });
                Vector3 loc = Function.Call<Vector3>(Hash._0x44A8FCB8ED227738, new InputArgument[2] {
                    vehicle,
                    value
                });
                vehicle.ApplyDamage(loc, 1000f, 10f);
            };
            if (data.FrontBumperBrokenOff) {
                BreakBumper("bumper_f");
            }
            if (data.RearBumperBrokenOff) {
                BreakBumper("bumper_r");
            }

            // Neon status
            for (int i = 0; i < 4; i++) {
                vehicle.SetNeonLightsOn(neons[i], on : data.Neon[i]);
            }

            // Mods
            for (int i = 0; i < mods.Length; i++) {
                vehicle.SetMod(mods[i], data.Mods[i], variations : false);
            }

            // Toggle mods
            for (int i = 0; i < toggleMods.Length; i++) {
                vehicle.ToggleMod(toggleMods[i], data.ToggleMods[i]);
            }

            return vehicle;
        }
    }
}