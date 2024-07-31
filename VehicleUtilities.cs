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
            VehicleData VehicleData = new VehicleData {
                Handle = vehicle.Handle,
                VehicleName = vehicle.DisplayName,
                Vehicle = (uint)vehicle.Model.Hash,

                // General
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
                TireSmokeColor = vehicle.Mods.TireSmokeColor,
                NeonLightsColor = vehicle.Mods.NeonLightsColor,
                ColorCombination = vehicle.Mods.ColorCombination,

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

                // Bumpers
                // VehicleData.FrontBumperBrokenOff = vehicle.IsFrontBumperBrokenOff;
                // VehicleData.RearBumperBrokenOff = vehicle.IsRearBumperBrokenOff;

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
                IsDriveable = vehicle.IsDriveable,

                // Proofs
                IsFireProof = vehicle.IsFireProof,
                IsExplosionProof = vehicle.IsExplosionProof,
                IsCollisionProof = vehicle.IsCollisionProof,
                IsMeleeProof = vehicle.IsMeleeProof,
                IsSteamProof = vehicle.IsSteamProof,
            };

            // Radio
            // if (Game.Player != null && Game.Player.Character.IsInVehicle()) {
            //     VehicleData.RadioStation =  (RadioStation) Function.Call<int>(Hash.GET_PLAYER_RADIO_STATION_INDEX);
            //     GTA.UI.Notification.Show(VehicleData.RadioStation.ToString());
            // }

            // Other
            if (Function.Call<bool>(Hash.IS_VEHICLE_ATTACHED_TO_TRAILER, vehicle)) {
                OutputArgument trailerOutput = new OutputArgument();
                Function.Call<bool>(Hash.GET_VEHICLE_TRAILER_VEHICLE, vehicle, trailerOutput);
                VehicleData.TowedVehicle = (uint)trailerOutput.GetResult<Vehicle>().Model.Hash;
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
            Vehicle vehicle = World.CreateVehicle(new Model((int)data.Vehicle), spawnPosition);
            data.Handle = vehicle.Handle;

            // General
            vehicle.IsPersistent = true;
            vehicle.DirtLevel = data.DirtLevel;
            vehicle.BodyHealth = data.BodyHealth;
            vehicle.Mods.Livery = data.Livery;
            vehicle.Mods.WindowTint = data.WindowTint;
            vehicle.RoofState = data.RoofState;
            vehicle.Mods.WheelType = data.WheelType;

            vehicle.HeliEngineHealth = data.HeliEngineHealth;

            // Location
            vehicle.Rotation = data.Rotation;

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
            vehicle.LightsMultiplier = data.LightsMultiplier;
            vehicle.AreLightsOn = data.AreLightsOn;
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
            vehicle.IsDriveable = true;
            Function.Call(Hash.SET_VEHICLE_ENGINE_ON, vehicle, data.IsEngineRunning, true, true);

            // Proofs
            vehicle.IsBulletProof = data.IsBulletProof;
            vehicle.IsFireProof = data.IsFireProof;
            vehicle.IsCollisionProof = data.IsCollisionProof;
            vehicle.IsExplosionProof = data.IsExplosionProof;
            vehicle.IsMeleeProof = data.IsMeleeProof;
            vehicle.IsSteamProof = data.IsSteamProof;

            // Other
            if (data.TowedVehicle != 0) {
                Vehicle trailer = World.CreateVehicle(new Model((int)data.TowedVehicle), data.Position + new Vector3(5f, 5f, 0f));
                trailer.IsPersistent = true;
                Function.Call(Hash.ATTACH_VEHICLE_TO_TRAILER, vehicle, trailer);
            }

            vehicle.Mods.InstallModKit();

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