using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using GTA;
using GTA.Math;
using GTA.Native;

namespace VehicleKeeper {
    public static class VehicleUtilities {
        static VehicleWindowIndex[] Windows = (VehicleWindowIndex[]) Enum.GetValues(typeof(VehicleWindowIndex));
        static VehicleDoorIndex[] Doors = (VehicleDoorIndex[]) Enum.GetValues(typeof(VehicleDoorIndex));
        static VehicleWheelBoneId[] Wheels = (VehicleWheelBoneId[]) Enum.GetValues(typeof(VehicleWheelBoneId));
        static VehicleNeonLight[] Neon = (VehicleNeonLight[]) Enum.GetValues(typeof(VehicleNeonLight));
        static VehicleModType[] Mods = (VehicleModType[]) Enum.GetValues(typeof(VehicleModType));
        static VehicleToggleModType[] ToggleMods = (VehicleToggleModType[]) Enum.GetValues(typeof(VehicleToggleModType));

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

            // General
            VehicleData.DirtLevel = vehicle.DirtLevel;
            VehicleData.BodyHealth = vehicle.BodyHealth;
            VehicleData.Livery = vehicle.Mods.Livery;
            VehicleData.WindowTint = vehicle.Mods.WindowTint;
            VehicleData.RoofState = vehicle.RoofState;
            VehicleData.WheelType = vehicle.Mods.WheelType;

            VehicleData.HeliEngineHealth = vehicle.HeliEngineHealth;

            // Radio
            // if (Game.Player.Character.IsInVehicle()) {
            //     VehicleData.RadioStation = (RadioStation) Function.Call<int>(Hash.GET_PLAYER_RADIO_STATION_INDEX);
            //     GTA.UI.Notification.Show(VehicleData.RadioStation.ToString());
            // }

            // Location
            VehicleData.Position = vehicle.Position;
            VehicleData.Rotation = vehicle.Rotation;

            // Engine
            VehicleData.EngineHealth = vehicle.EngineHealth;
            VehicleData.IsEngineRunning = vehicle.IsEngineRunning;
            VehicleData.IsDriveable = vehicle.IsDriveable;

            // Coloring
            VehicleData.PrimaryColor = vehicle.Mods.PrimaryColor;
            VehicleData.SecondaryColor = vehicle.Mods.SecondaryColor;
            VehicleData.DashboardColor = vehicle.Mods.DashboardColor;
            VehicleData.PearlescentColor = vehicle.Mods.PearlescentColor;
            VehicleData.RimColor = vehicle.Mods.RimColor;
            VehicleData.TrimColor = vehicle.Mods.TrimColor;
            VehicleData.CustomPrimaryColor = vehicle.Mods.CustomPrimaryColor;
            VehicleData.CustomSecondaryColor = vehicle.Mods.CustomSecondaryColor;
            VehicleData.TireSmokeColor = vehicle.Mods.TireSmokeColor;
            VehicleData.NeonLightsColor = vehicle.Mods.NeonLightsColor;
            VehicleData.ColorCombination = vehicle.Mods.ColorCombination;

            // License plate
            VehicleData.LicensePlate = vehicle.Mods.LicensePlate;
            VehicleData.LicensePlateStyle = vehicle.Mods.LicensePlateStyle;

            // Lights
            VehicleData.IsLeftHeadlightBroken = vehicle.IsLeftHeadLightBroken;
            VehicleData.IsRightHeadlightBroken = vehicle.IsRightHeadLightBroken;
            VehicleData.AreLightsOn = vehicle.AreLightsOn;
            VehicleData.AreHighBeamsOn = vehicle.AreHighBeamsOn;
            VehicleData.IsSearchLightOn = vehicle.IsSearchLightOn;

            // Bumpers
            // VehicleData.FrontBumperBrokenOff = vehicle.IsFrontBumperBrokenOff;
            // VehicleData.RearBumperBrokenOff = vehicle.IsRearBumperBrokenOff;

            // Fuel/Oil
            VehicleData.PetrolTankHealth = vehicle.PetrolTankHealth;
            VehicleData.FuelLevel = vehicle.FuelLevel;
            VehicleData.OilLevel = vehicle.OilLevel;

            // Doors locks
            VehicleData.LockStatus = vehicle.LockStatus;

            // Alarm
            VehicleData.Alarm = vehicle.IsAlarmSet;

            // Other
            OutputArgument trailerOutput = new OutputArgument();
            Function.Call<bool>(Hash.GET_VEHICLE_TRAILER_VEHICLE, vehicle, trailerOutput);
            VehicleData.TowedVehicle = (uint) trailerOutput.GetResult<Vehicle>().Model.Hash;

            // Windows
            foreach (VehicleWindowIndex window in Windows) {
                try {
                    VehicleData.Windows.Add(
                        new VehicleWindowData(vehicle.Windows[window].Index, vehicle.Windows[window].IsIntact)
                    );
                } catch { }
            }

            // // Doors
            foreach (VehicleDoorIndex door in Doors) {
                try {
                    VehicleData.Doors.Add(
                        new VehicleDoorData(vehicle.Doors[door].Index, vehicle.Doors[door].IsBroken)
                    );
                } catch { }
            }

            // Wheels
            foreach (VehicleWheelBoneId wheel in Wheels) {
                try {
                    VehicleData.Wheels.Add(
                        new VehicleWheelData(
                            wheel,
                            vehicle.Wheels[wheel].Health, vehicle.Wheels[wheel].TireHealth,
                            vehicle.Wheels[wheel].IsBursted, vehicle.Wheels[wheel].IsPunctured
                        )
                    );
                } catch { }
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
                } catch { }
            }

            // Toggle mods
            foreach (VehicleToggleModType mod in ToggleMods) {
                try {
                    VehicleData.ToggleMods.Add(
                        new VehicleToggleModData(mod, vehicle.Mods[mod].IsInstalled)
                    );
                } catch { }
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
            Vehicle vehicle = World.CreateVehicle(new Model((int) data.Vehicle), spawnPosition);
            data.Handle = vehicle.Handle;

            // General
            vehicle.DirtLevel = data.DirtLevel;
            vehicle.BodyHealth = data.BodyHealth;
            vehicle.Mods.Livery = data.Livery;
            vehicle.Mods.WindowTint = data.WindowTint;
            vehicle.RoofState = data.RoofState;
            vehicle.Mods.WheelType = data.WheelType;
            vehicle.IsPersistent = true;

            vehicle.HeliEngineHealth = data.HeliEngineHealth;

            // Radio
            // vehicle.RadioStation = data.RadioStation;

            // Location
            vehicle.Rotation = data.Rotation;

            // Engine
            vehicle.EngineHealth = data.EngineHealth;
            if (data.IsEngineRunning) {
                Function.Call(Hash.SET_VEHICLE_ENGINE_ON, vehicle, true, true, false);
            }
            vehicle.IsDriveable = data.IsDriveable;

            // Coloring
            vehicle.Mods.PrimaryColor = data.PrimaryColor;
            vehicle.Mods.SecondaryColor = data.SecondaryColor;
            vehicle.Mods.DashboardColor = data.DashboardColor;
            vehicle.Mods.PearlescentColor = data.PearlescentColor;
            vehicle.Mods.RimColor = data.RimColor;
            vehicle.Mods.TrimColor = data.TrimColor;
            vehicle.Mods.CustomPrimaryColor = data.CustomPrimaryColor;
            vehicle.Mods.CustomSecondaryColor = data.CustomSecondaryColor;
            vehicle.Mods.TireSmokeColor = data.TireSmokeColor;
            vehicle.Mods.NeonLightsColor = data.NeonLightsColor;
            vehicle.Mods.ColorCombination = data.ColorCombination;

            // License plate
            vehicle.Mods.LicensePlate = data.LicensePlate;
            vehicle.Mods.LicensePlateStyle = data.LicensePlateStyle;

            // Lights
            vehicle.IsLeftHeadLightBroken = data.IsLeftHeadlightBroken;
            vehicle.IsRightHeadLightBroken = data.IsRightHeadlightBroken;
            vehicle.AreLightsOn = data.AreLightsOn;
            vehicle.AreHighBeamsOn = data.AreHighBeamsOn;
            vehicle.IsSearchLightOn = data.IsSearchLightOn;

            // Fuel/Oil
            vehicle.PetrolTankHealth = data.PetrolTankHealth;
            vehicle.FuelLevel = data.FuelLevel;
            vehicle.OilLevel = data.OilLevel;

            // Doors locks
            vehicle.LockStatus = data.LockStatus;

            // Alarm
            Function.Call<bool>(Hash.SET_VEHICLE_ALARM, new InputArgument[2] { vehicle, data.Alarm });

            // Other
            if (data.TowedVehicle != 0) {
                Vehicle trailer = World.CreateVehicle(new Model((int) data.TowedVehicle), data.Position + new Vector3(5f, 5f, 0f));
                trailer.IsPersistent = true;
                Function.Call(Hash.ATTACH_VEHICLE_TO_TRAILER, new InputArgument[2] { vehicle, trailer });
            }

            vehicle.Mods.InstallModKit();

            // Windows
            foreach (VehicleWindowData window in data.Windows) {
                try {
                    if (!window.IsIntact) {
                        vehicle.Windows[window.Index].Smash();
                    }
                } catch { }
            }

            // Doors
            foreach (VehicleDoorData door in data.Doors) {
                try {
                    if (door.IsBroken) {
                        vehicle.Doors[door.Index].Break();
                    }
                } catch { }
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
                } catch { }
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
                } catch { }
            }

            // Toggle mods
            foreach (VehicleToggleModData mod in data.ToggleMods) {
                try {
                    vehicle.Mods[mod.Type].IsInstalled = mod.IsInstalled;
                } catch { }
            }

            // // Bumpers
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

            return vehicle;
        }
    }
}