using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using LemonUI.Menus;

namespace VehicleKeeper {
    // Resolves paths next to the loaded DLL (the scripts/ folder) rather than
    // against the process CWD, which for the game is the root folder where
    // GTA5.exe lives. SHVDN3 loads scripts in place, so Assembly.Location is the
    // real scripts/ path; falling back to the relative name keeps it working if
    // a host ever shadow-copies the assembly to a temp dir.
    public static class ScriptPaths {
        public static readonly string Directory =
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                ?? AppDomain.CurrentDomain.BaseDirectory;

        public static string For(string fileName) => Path.Combine(Directory, fileName);
    }

    public static class Logger {
        static readonly string LogFilePath = ScriptPaths.For("VehicleKeeper.log");

        public static void ClearLog() {
            try {
                if (File.Exists(LogFilePath)) {
                    File.WriteAllText(LogFilePath, string.Empty);
                }
            } catch {
                // Logging must never crash the script; a locked/unwritable log is
                // not worth taking the mod down for.
            }
        }

        public static void LogError(object message) {
            try {
                File.AppendAllText(LogFilePath, DateTime.Now + " [Error] " + message + Environment.NewLine);
            } catch {
                // See ClearLog: swallow file-IO failures by design.
            }
        }
    }

    public class VehicleKeeper : Script {
        readonly ScriptSettings Config;
        readonly int VehicleLimit;
        readonly Keys SaveKey;
        readonly Keys UnsaveKey;
        readonly Keys MenuKey;

        NativeMenu MainMenu;
        NativeMenu VehicleMenu;
        NativeListItem<BlipColor> BlipColorListItem;
        NativeListItem<float> BlipDistanceItem;

        int Period;
        readonly List<VehicleData> SpawnedVehicles = new List<VehicleData>();
        Vehicle LastVehicle;

        BlipColor BlipColor = BlipColor.Blue;
        float BlipDistance;

        public VehicleKeeper() {
            Logger.ClearLog();

            MainMenuInit();

            // Default persistence directory lives next to the DLL (the scripts/
            // folder), not the game root CWD. Resolve it before seeding defaults so
            // the value written to the .ini is the absolute, game-root-independent path.
            string defaultPersistencePath = ScriptPaths.For("VehicleKeeper");

            Config = ScriptSettings.Load(ScriptPaths.For("VehicleKeeper.ini"));

            // Define default values
            SetConfigValueIfNotDefined("Configuration", "VehicleLimit", 8);
            SetConfigValueIfNotDefined("Configuration", "MenuKey", Keys.T);
            SetConfigValueIfNotDefined("Configuration", "SaveKey", Keys.X);
            SetConfigValueIfNotDefined("Configuration", "UnsaveKey", Keys.Z);
            SetConfigValueIfNotDefined("Configuration", "BlipColor", BlipColor.Blue);
            SetConfigValueIfNotDefined("Configuration", "BlipDistance", 500f);
            SetConfigValueIfNotDefined("Configuration", "VehiclePersistencePath", defaultPersistencePath);

            // Read configuration values
            VehicleLimit = Config.GetValue("Configuration", "VehicleLimit", 8);
            MenuKey = Config.GetValue("Configuration", "MenuKey", Keys.T);
            SaveKey = Config.GetValue("Configuration", "SaveKey", Keys.X);
            UnsaveKey = Config.GetValue("Configuration", "UnsaveKey", Keys.Z);
            BlipColor = Config.GetValue("Configuration", "BlipColor", BlipColor.Blue);
            BlipColorListItem.SelectedItem = BlipColorListItem.Items.First(x => x == BlipColor);
            BlipDistance = Config.GetValue("Configuration", "BlipDistance", 500f);
            BlipDistanceItem.SelectedItem = BlipDistanceItem.Items.First(x => x == BlipDistance);

            string basePath = Config.GetValue("Configuration", "VehiclePersistencePath", defaultPersistencePath);
            JsonVehicleStorage.Initialize(basePath);

            // Clear existing blips from previously managed vehicles
            ClearExistingBlips();

            Tick += OnTick;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
        }

        private void SetConfigValueIfNotDefined<T>(string section, string key, T defaultValue) {
            var currentValue = Config.GetValue(section, key, default(T));
            // Only set the default value if the current value is equal to the default value of the type
            if (EqualityComparer<T>.Default.Equals(currentValue, default)) {
                Config.SetValue(section, key, defaultValue);
                Config.Save();
            }
        }

        private void ClearExistingBlips() {
            List<VehicleData> savedVehicles = JsonVehicleStorage.GetVehicles();
            foreach (var vehicleData in savedVehicles) {
                Vehicle vehicle = (Vehicle)Entity.FromHandle(vehicleData.Handle);
                if (vehicle != null) {
                    RemoveBlipFromVehicle(vehicle);
                }
            }
        }

        public void OnTick(object sender, EventArgs eventArgs) {
            try {
                if (MainMenu.Visible || VehicleMenu.Visible) {
                    MainMenu.Process();
                    VehicleMenu.Process();
                }

                if (Period <= 60) {
                    Period++;
                } else {
                    Period = 0;
                }

                Ped player = Game.Player?.Character;
                if (player == null) {
                    return;
                }

                if (Period == 30) {
                    UpdateBlips(player);
                }

                if (Period == 60 && LastVehicle != null &&
                    World.GetZoneDisplayName(player.Position) != "San Andreas"
                ) {
                    VehicleData vd = VehicleUtilities.CreateInfo(LastVehicle);
                    UpdateVehicleData(LastVehicle, vd);
                }
            } catch (Exception e) {
                // A held entity may despawn between ticks and throw on access. Log
                // and bail for this frame rather than crashing the script mid-game.
                Logger.LogError(e.ToString());
            }
        }

        void UpdateBlips(Ped player) {
            // Iterate over a snapshot: DespawnVehicle mutates SpawnedVehicles, so
            // walking the live list by index would skip entries after a removal.
            foreach (VehicleData vd in SpawnedVehicles.ToList()) {
                Vehicle v = (Vehicle)Entity.FromHandle(vd.Handle);
                if (v == null) {
                    GTA.UI.Notification.PostTicker($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} is despawned", false);
                    DespawnVehicle(v, vd);
                } else if (v.IsConsideredDestroyed) {
                    GTA.UI.Notification.PostTicker($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} is destroyed", false);
                    DespawnVehicle(v, vd);
                } else if (player.IsInVehicle(v) &&
                    World.GetZoneDisplayName(player.Position) != "San Andreas") {
                    LastVehicle = player.CurrentVehicle;
                    RemoveBlipFromVehicle(LastVehicle);
                } else if (BlipDistance < 0f || v.Position.DistanceTo(player.Position) <= BlipDistance) {
                    // BlipDistance < 0 is the "always show" sentinel (the -1 menu option).
                    SetBlipOnVehicle(v);
                } else {
                    RemoveBlipFromVehicle(v);
                }
            }
        }

        void SetBlipOnVehicle(Vehicle v) {
            if (v == null || v.AttachedBlip != null) {
                return;
            }

            try {
                Blip blip = v.AddBlip();
                blip.IsShortRange = true;
                blip.Sprite = BlipSprite.PersonalVehicleCar;
                blip.DisplayType = BlipDisplayType.BothMapSelectable;
                blip.Color = BlipColor;
                blip.Priority = 13;
            } catch (Exception e) {
                Logger.LogError(e.ToString());
            }
        }

        void RemoveBlipFromVehicle(Vehicle v) {
            if (v != null && v.AttachedBlip != null) {
                v.AttachedBlip.Delete();
            }
        }

        (Vehicle, VehicleData) GetSpawnedIfPossible(VehicleData vd) {
            int spawned = SpawnedVehicles.IndexOf(vd);
            if (spawned > -1) {
                return ((Vehicle)Entity.FromHandle(SpawnedVehicles[spawned].Handle), SpawnedVehicles[spawned]);
            }

            return (null, vd);
        }

        bool SaveVehicle(Vehicle v, VehicleData vd) {
            try {
                SetBlipOnVehicle(v);
                v.IsPersistent = true;
                SpawnedVehicles.Add(vd);
                JsonVehicleStorage.SaveVehicle(vd);
            } catch (Exception e) {
                Logger.LogError(e.ToString());
                return false;
            }

            return true;
        }

        bool UnsaveVehicle(Vehicle v, VehicleData vd) {
            try {
                if (v != null) {
                    RemoveBlipFromVehicle(v);
                    v.IsPersistent = false;
                }
                SpawnedVehicles.Remove(vd);
                JsonVehicleStorage.RemoveVehicle(vd);
            } catch (Exception e) {
                Logger.LogError(e.ToString());
                return false;
            }

            return true;
        }

        bool UpdateVehicleData(Vehicle v, VehicleData vd) {
            try {
                if (v != null) {
                    v.IsPersistent = true;
                }
                if (!SpawnedVehicles.Contains(vd)) {
                    SpawnedVehicles.Add(vd);
                }
                JsonVehicleStorage.UpdateVehicle(vd);
            } catch (Exception e) {
                Logger.LogError(e.ToString());
                return false;
            }

            return true;
        }

        bool DespawnVehicle(Vehicle v, VehicleData vd) {
            try {
                LastVehicle = null;
                if (v != null) {
                    RemoveBlipFromVehicle(v);
                    v.IsPersistent = false;
                    v.Delete();
                }
                SpawnedVehicles.Remove(vd);
            } catch (Exception e) {
                Logger.LogError(e.ToString());
                return false;
            }

            return true;
        }

        bool SpawnVehicle(VehicleData vd, bool nearby = false) {
            Vehicle vehicle;
            try {
                vehicle = VehicleUtilities.CreateVehicleFromData(ref vd, nearby);
            } catch (Exception e) {
                Logger.LogError(e.ToString());
                return false;
            }

            if (vehicle == null) {
                return false;
            }

            SetBlipOnVehicle(vehicle);
            SpawnedVehicles.Add(vd);
            return true;
        }

        void LoadVehicles() {
            List<VehicleData> jsonVehicles = new List<VehicleData>();

            try {
                jsonVehicles = JsonVehicleStorage.GetVehicles();
            } catch (Exception e) {
                Logger.LogError(e.ToString());
            }

            try {
                for (int i = 0; i < jsonVehicles.Count(); i++) {
                    if (!SpawnedVehicles.Contains(jsonVehicles[i])) {
                        SpawnVehicle(jsonVehicles[i]);
                    } else {
                        GTA.UI.Notification.PostTicker($"Vehicle {jsonVehicles[i].VehicleName} {jsonVehicles[i].LicensePlate.Trim()} is already loaded", false);
                    }
                }
            } catch (Exception e) {
                Logger.LogError(e.ToString());
            }
        }

        void UnsaveVehicles() {
            // Drain a snapshot of the saved list: UnsaveVehicle removes from the
            // underlying store, so iterate a copy rather than the live list.
            foreach (VehicleData saved in JsonVehicleStorage.GetVehicles().ToList()) {
                // If the vehicle is currently spawned, use its tracked instance so
                // persistency and its blip are cleared correctly.
                (Vehicle v, VehicleData vd) = GetSpawnedIfPossible(saved);
                UnsaveVehicle(v, vd);
            }

            GTA.UI.Notification.PostTicker("All vehicles have been unsaved", false);
        }

        void DespawnVehicles() {
            // Snapshot: DespawnVehicle removes from SpawnedVehicles as it goes.
            foreach (VehicleData vd in SpawnedVehicles.ToList()) {
                Vehicle v = (Vehicle)Entity.FromHandle(vd.Handle);
                DespawnVehicle(v, vd);
            }

            GTA.UI.Notification.PostTicker("All vehicles have been despawned", false);
        }

        void SaveCurrentVehicle() {
            Ped player = Game.Player.Character;

            if (player.IsInVehicle()) {
                Vehicle currentVeh = player.CurrentVehicle;
                VehicleData vd = VehicleUtilities.CreateInfo(currentVeh);
                List<VehicleData> savedVehicles = JsonVehicleStorage.GetVehicles();
                if (!savedVehicles.Contains(vd)) {
                    if (savedVehicles.Count() < VehicleLimit) {
                        SaveVehicle(currentVeh, vd);
                        GTA.UI.Notification.PostTicker($"Vehicle {currentVeh.DisplayName} {currentVeh.Mods.LicensePlate.Trim()} saved", false);
                    } else {
                        GTA.UI.Notification.PostTicker($"You can't save more than {VehicleLimit} vehicles", false);
                    }
                } else {
                    UpdateVehicleData(currentVeh, vd);
                    GTA.UI.Notification.PostTicker($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} has been overridden", false);
                }
            } else if (player.IsOnFoot) {
                GTA.UI.Notification.PostTicker("Player is not in a vehicle", false);
            }
        }

        void UnsaveCurrentVehicle() {
            Ped player = Game.Player.Character;

            if (player.IsInVehicle()) {
                Vehicle currentVeh = player.CurrentVehicle;
                VehicleData vd = VehicleUtilities.CreateInfo(currentVeh);
                List<VehicleData> savedVehicles = JsonVehicleStorage.GetVehicles();

                if (savedVehicles.Contains(vd)) {
                    UnsaveVehicle(currentVeh, vd);
                    GTA.UI.Notification.PostTicker($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} is unsaved", false);
                }
            } else if (player.IsOnFoot) {
                GTA.UI.Notification.PostTicker("Player is not in a vehicle", false);
            }
        }

        private void OnMainMenuItemSelect(object sender, ItemActivatedArgs e) {
            if (sender == MainMenu) {
                var item = MainMenu.Items[MainMenu.SelectedIndex];
                if (item is NativeItem nativeItem) {
                    switch (nativeItem.Title) {
                        case "Save Current Vehicle":
                            SaveCurrentVehicle();
                            break;
                        case "Spawn All":
                            LoadVehicles();
                            break;
                        case "Despawn All":
                            DespawnVehicles();
                            break;
                        case "Unsave All":
                            UnsaveVehicles();
                            break;
                        case "Blip Color":
                            BlipColor = BlipColorListItem.SelectedItem;
                            Config.SetValue("Configuration", "BlipColor", BlipColor);
                            Config.Save();
                            foreach (VehicleData vd in SpawnedVehicles) {
                                (Vehicle v, _) = GetSpawnedIfPossible(vd);
                                if (v != null && v.AttachedBlip != null) {
                                    v.AttachedBlip.Color = BlipColor;
                                }
                            }
                            break;
                        case "Blip Distance":
                            BlipDistance = BlipDistanceItem.SelectedItem;
                            Config.SetValue("Configuration", "BlipDistance", BlipDistance);
                            Config.Save();
                            break;
                        case "Exit":
                            MainMenu.Visible = false;
                            break;
                    }
                }
            }
        }

        private void OnVehicleMenuItemSelect(object sender, ItemActivatedArgs e) {
            if (!(VehicleMenu.Items[VehicleMenu.SelectedIndex] is NativeListItem<string> item)) {
                return;
            }
            VehicleData vd = JsonVehicleStorage.GetVehicle(VehicleUtilities.GetHashString(item.Title));
            if (vd == null) {
                GTA.UI.Notification.PostTicker($"This vehicle doesn't exist in your list", false);
                return;
            }
            Vehicle v;
            switch (item.SelectedItem) {
                case "Spawn":
                    if (!SpawnedVehicles.Contains(vd)) {
                        if (SpawnVehicle(vd)) {
                            GTA.UI.Notification.PostTicker($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} spawned successfully", false);
                        }
                    } else {
                        GTA.UI.Notification.PostTicker($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} is already spawned", false);
                    }
                    break;
                case "Spawn Nearby":
                    if (!SpawnedVehicles.Contains(vd)) {
                        if (SpawnVehicle(vd, true)) {
                            GTA.UI.Notification.PostTicker($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} spawned successfully", false);
                        }
                    } else {
                        GTA.UI.Notification.PostTicker($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} is already spawned", false);
                    }
                    break;
                case "Despawn":
                    if (!SpawnedVehicles.Contains(vd)) {
                        GTA.UI.Notification.PostTicker($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} is not spawned", false);
                    } else {
                        (v, vd) = GetSpawnedIfPossible(vd);
                        if (DespawnVehicle(v, vd)) {
                            GTA.UI.Notification.PostTicker($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} despawned successfully", false);
                        }
                    }
                    break;
                case "Save":
                    if (!SpawnedVehicles.Contains(vd)) {
                        GTA.UI.Notification.PostTicker($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} is not spawned", false);
                        return;
                    }

                    (v, vd) = GetSpawnedIfPossible(vd);
                    UnsaveVehicle(v, vd);
                    if (SaveVehicle(v, vd)) {
                        GTA.UI.Notification.PostTicker($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} has been overridden", false);
                    } else {
                        GTA.UI.Notification.PostTicker($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} can't be overridden", false);
                    }
                    break;
                case "Unsave":
                    (v, vd) = GetSpawnedIfPossible(vd);

                    if (UnsaveVehicle(v, vd)) {
                        GTA.UI.Notification.PostTicker($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} has been unsaved", false);
                    } else {
                        GTA.UI.Notification.PostTicker($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} can't be unsaved", false);
                    }
                    break;
                case "Set Spawn Location":
                    Vector3 waypoint = World.WaypointPosition;
                    if (waypoint.Length() == 0) {
                        GTA.UI.Notification.PostTicker("Please set waypoint to identify target position", false);
                    } else if (Game.Player.Character.IsInVehicle(LastVehicle)) {
                        GTA.UI.Notification.PostTicker("You should leave the vehicle first as the spawn location is being overridden while you're in the car", false);
                    } else {
                        (v, vd) = GetSpawnedIfPossible(vd);
                        vd.Position = waypoint;
                        UnsaveVehicle(v, vd);
                        SaveVehicle(v, vd);
                        GTA.UI.Notification.PostTicker($"Spawn position was refreshed for {vd.VehicleName} {vd.LicensePlate.Trim()}", false);
                    }
                    break;
            }
        }

        public void OnKeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode != MenuKey) {
                return;
            }

            // Same key toggles the menu. Closing must also dismiss the submenu, or
            // pressing the key while it's open would leave the submenu on screen.
            if (MainMenu.Visible || VehicleMenu.Visible) {
                MainMenu.Visible = false;
                VehicleMenu.Visible = false;
                return;
            }

            // Opening: rebuild the saved-vehicle list from the store.
            List<VehicleData> savedVehicles = JsonVehicleStorage.GetVehicles();
            VehicleMenu.Clear();
            foreach (VehicleData vd in savedVehicles) {
                NativeListItem<string> vehicleItem = new NativeListItem<string>($"{vd.VehicleName} {vd.LicensePlate.Trim()}",
                    "Spawn", "Spawn Nearby", "Despawn", "Save", "Unsave", "Set Spawn Location");
                VehicleMenu.Add(vehicleItem);
            }
            MainMenu.Visible = true;
        }

        public void OnKeyUp(object sender, KeyEventArgs e) {
            Keys[] keys = new[] { SaveKey, UnsaveKey };

            if (keys.Any(x => x == e.KeyCode)) {
                if (e.KeyCode == SaveKey) {
                    SaveCurrentVehicle();
                } else if (e.KeyCode == UnsaveKey) {
                    UnsaveCurrentVehicle();
                }
            }
        }

        void MainMenuInit() {
            MainMenu = new NativeMenu("Vehicle Keeper", "Version 3.7.0");

            VehicleMenu = new NativeMenu("Saved Vehicles", "Saved Vehicles");
            MainMenu.AddSubMenu(VehicleMenu);
            NativeItem saveCurrent = new NativeItem("Save Current Vehicle");
            MainMenu.Add(saveCurrent);
            NativeItem spawnVehicles = new NativeItem("Spawn All");
            MainMenu.Add(spawnVehicles);
            NativeItem despawnVehicles = new NativeItem("Despawn All");
            MainMenu.Add(despawnVehicles);
            NativeItem unsaveButton = new NativeItem("Unsave All");
            MainMenu.Add(unsaveButton);
            BlipColorListItem = new NativeListItem<BlipColor>("Blip Color", BlipColor.Blue, BlipColor.Green, BlipColor.Purple, BlipColor.Red, BlipColor.Orange, BlipColor.Yellow, BlipColor.Pink, BlipColor.White);
            MainMenu.Add(BlipColorListItem);
            BlipDistanceItem = new NativeListItem<float>("Blip Distance", -1f, 10f, 50f, 100f, 200f, 500f, 1000f, 2000f, 5000f, 10000f, 20000f);
            MainMenu.Add(BlipDistanceItem);
            NativeItem exitButton = new NativeItem("Exit");
            MainMenu.Add(exitButton);

            MainMenu.ItemActivated += OnMainMenuItemSelect;
            VehicleMenu.ItemActivated += OnVehicleMenuItemSelect;
        }
    }
}
