using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;

namespace VehicleKeeper {
    public static class Logger {
        public static void LogError(object message) {
            File.AppendAllText("VehicleKeeper.log", DateTime.Now + " [Error] " + message + Environment.NewLine);
        }
    }

    public class VehicleKeeper : Script {
        ScriptSettings config;
        int vehicleLimit;
        Keys saveKey;
        Keys unsaveKey;
        Keys menuKey;

        MenuPool menuPool;
        UIMenu mainMenu;
        UIMenu vehicleMenu;
        int period = 0;
        List<VehicleData> spawnedVehicles = new List<VehicleData>();
        Vehicle lastVehicle = null;

        public VehicleKeeper() {
            new Model();

            MainMenu();

            Tick += OnTick;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;

            config = ScriptSettings.Load(@"./scripts/VehicleKeeper.ini");
            vehicleLimit = config.GetValue("Configuration", "VehicleLimit", 8);
            menuKey = config.GetValue("Configuration", "MenuKey", Keys.T);
            saveKey = config.GetValue("Configuration", "SaveKey", Keys.X);
            unsaveKey = config.GetValue("Configuration", "UnsaveKey", Keys.Z);

            string basePath = config.GetValue("Configuration", "VehiclePersistencePath", @"./scripts/VehicleKeeper");

            JsonVehicleStorage.InitializeBasePath(basePath);

            // LoadVehicles();
        }

        public void OnTick(object sender, EventArgs eventArgs) {
            if (menuPool != null && menuPool.IsAnyMenuOpen()) {
                menuPool.ProcessMenus(); // Loads all the menus
            }

            if (period < 25) {
                period++;
                return;
            } else {
                period = 0;
            }

            if (lastVehicle != null && Game.Player != null &&
                // lastVehicle != Game.Player.Character.CurrentVehicle &&
                // !Game.Player.Character.IsGettingIntoVehicle &&
                // !Game.Player.Character.IsJumpingOutOfVehicle &&
                World.GetZoneDisplayName(Game.Player.Character.Position) != "San Andreas"
            ) {
                VehicleData vd = VehicleUtilities.CreateInfo(lastVehicle);
                VehicleData[] savedVehicles = JsonVehicleStorage.GetVehicles();
                if (savedVehicles.Contains(vd)) {
                    UnsaveVehicle(lastVehicle, vd);
                    SaveVehicle(lastVehicle, vd);
                }
            }

            for (int i = 0; i < spawnedVehicles.Count; i++) {
                Vehicle v = (Vehicle) Entity.FromHandle(spawnedVehicles[i].Handle);
                if (v.IsConsideredDestroyed) {
                    GTA.UI.Notification.Show($"Vehicle {v.DisplayName} is destroyed");
                    DespawnVehicle(v, spawnedVehicles[i]);
                }
            }

            if (Game.Player != null && Game.Player.Character.IsInVehicle() &&
                World.GetZoneDisplayName(Game.Player.Character.Position) != "San Andreas"
            ) {
                lastVehicle = Game.Player.Character.CurrentVehicle;
            }
        }

        void OnMainMenuItemSelect(UIMenu sender, UIMenuItem item, int index) {
            if (item.Text == "Save current vehicle") SaveCurrentVehicle();
            if (item.Text == "Spawn All") LoadVehicles();
            if (item.Text == "Despawn All") DespawnVehicles();
            if (item.Text == "Unsave All") UnsaveVehicles();
            if (item.Text == "Exit") mainMenu.GoBack();
        }

        void OnListItemSelect(UIMenu sender, UIMenuItem item, int index) {
            UIMenuListItem listItem = (UIMenuListItem) item;
            VehicleData vd = JsonVehicleStorage.GetVehicle(VehicleUtilities.GetHashString(listItem.Text));
            if (vd == null) {
                GTA.UI.Notification.Show($"This vehicle doesn't exist in your list");
                return;
            }
            if ((string) listItem.Items[listItem.Index] == "Spawn") {
                if (!spawnedVehicles.Contains(vd)) {
                    if (SpawnVehicle(vd)) {
                        GTA.UI.Notification.Show($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} spawned successully");
                    }
                } else {
                    GTA.UI.Notification.Show($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} is already spawned");
                }
            }
            if ((string) listItem.Items[listItem.Index] == "Spawn nearby") {
                if (!spawnedVehicles.Contains(vd)) {
                    if (SpawnVehicle(vd, true)) {
                        GTA.UI.Notification.Show($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} spawned successully");
                    }
                } else {
                    GTA.UI.Notification.Show($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} is already spawned");
                }
            }
            if ((string) listItem.Items[listItem.Index] == "Despawn") {
                if (!spawnedVehicles.Contains(vd)) {
                    GTA.UI.Notification.Show($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} is not spawned");
                } else {
                    Vehicle v;
                    (v, vd) = GetSpawnedIfPossible(vd);
                    if (DespawnVehicle(v, vd)) {
                        GTA.UI.Notification.Show($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} despawned successully");
                    }
                }
            }
            if ((string) listItem.Items[listItem.Index] == "Save") {
                if (!spawnedVehicles.Contains(vd)) {
                    GTA.UI.Notification.Show($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} is not spawned");
                    return;
                }

                Vehicle v;
                (v, vd) = GetSpawnedIfPossible(vd);
                UnsaveVehicle(v, vd);
                if (SaveVehicle(v, vd)) {
                    GTA.UI.Notification.Show($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} has been overridden");
                } else {
                    GTA.UI.Notification.Show($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} can't be overridden");
                }
            }
            if ((string) listItem.Items[listItem.Index] == "Unsave") {
                Vehicle v;
                (v, vd) = GetSpawnedIfPossible(vd);

                if (UnsaveVehicle(v, vd)) {
                    GTA.UI.Notification.Show($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} has been unsaved");
                } else {
                    GTA.UI.Notification.Show($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} can't be unsaved");
                }
            }
            if ((string) listItem.Items[listItem.Index] == "Set spawn location") {
                Vector3 waypoint = World.WaypointPosition;
                if (waypoint.Length() == 0) {
                    GTA.UI.Notification.Show("Please set waypoint to identify target position");
                } else {
                    Vehicle v;
                    (v, vd) = GetSpawnedIfPossible(vd);
                    vd.Position = waypoint;
                    UnsaveVehicle(v, vd);
                    SaveVehicle(v, vd);
                    GTA.UI.Notification.Show($"Spawn position was refreshed for {vd.VehicleName} {vd.LicensePlate.Trim()}");
                }
            }
        }

        public void OnKeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == menuKey && !menuPool.IsAnyMenuOpen()) {
                VehicleData[] savedVehicles = JsonVehicleStorage.GetVehicles();
                mainMenu.Visible = !mainMenu.Visible;
                vehicleMenu.MenuItems.Clear();

                foreach (VehicleData vd in savedVehicles) {
                    if (vehicleMenu.MenuItems.Count <= savedVehicles.Count()) {
                        UIMenuListItem vehicleItem = new UIMenuListItem($"{vd.VehicleName} {vd.LicensePlate.Trim()}",
                            new List<object> { "Spawn", "Spawn nearby", "Despawn", "Save", "Unsave", "Set spawn location" }, 0);
                        vehicleMenu.AddItem(vehicleItem);
                    }
                }
            }
        }

        public void OnKeyUp(object sender, KeyEventArgs e) {
            Keys[] keys = new [] { saveKey, unsaveKey };

            if (keys.Any(x => x == e.KeyCode)) {
                if (e.KeyCode == saveKey) {
                    SaveCurrentVehicle();
                } else if (e.KeyCode == unsaveKey) {
                    UnsaveCurrentVehicle();
                }
            }
        }

        void MainMenu() {
            menuPool = new MenuPool();
            mainMenu = new UIMenu("Vehicle Keeper", "Version 3.1.0");

            menuPool.Add(mainMenu); // Adds mainMenu to the pool
            vehicleMenu = menuPool.AddSubMenu(mainMenu, "Saved Vehicles"); // Submenu options
            UIMenuItem saveCurrent = new UIMenuItem("Save current vehicle");
            mainMenu.AddItem(saveCurrent);
            UIMenuItem spawnVehicles = new UIMenuItem("Spawn All");
            mainMenu.AddItem(spawnVehicles);
            UIMenuItem despawnVehicles = new UIMenuItem("Despawn All");
            mainMenu.AddItem(despawnVehicles);
            UIMenuItem unsaveButton = new UIMenuItem("Unsave All");
            mainMenu.AddItem(unsaveButton);
            UIMenuItem exitButton = new UIMenuItem("Exit");
            mainMenu.AddItem(exitButton);

            mainMenu.OnItemSelect += OnMainMenuItemSelect;
            vehicleMenu.OnItemSelect += OnListItemSelect;
        }

        void SetBlipOnVehicle(Vehicle v) {
            try {
                Blip blip = v.AddBlip();
                blip.IsShortRange = true;
                blip.Sprite = BlipSprite.PersonalVehicleCar;
                blip.DisplayType = BlipDisplayType.BothMapSelectable;
                blip.Color = BlipColor.Blue;
                blip.Priority = 13;
            } catch (Exception e) {
                Logger.LogError(e.ToString().ToString());
            }
        }

        (Vehicle, VehicleData) GetSpawnedIfPossible(VehicleData vd) {
            int spawned = spawnedVehicles.IndexOf(vd);
            if (spawned > -1) {
                return ((Vehicle) Entity.FromHandle(spawnedVehicles[spawned].Handle), spawnedVehicles[spawned]);
            }

            return ((Vehicle) Entity.FromHandle(vd.Handle), vd);
        }

        bool SaveVehicle(Vehicle v, VehicleData vd) {
            try {
                SetBlipOnVehicle(v);
                v.IsPersistent = true;
                spawnedVehicles.Add(vd);
                JsonVehicleStorage.SaveVehicle(vd);
            } catch (Exception e) {
                Logger.LogError(e.ToString());
                return false;
            }

            return true;
        }

        bool UnsaveVehicle(Vehicle v, VehicleData vd) {
            try {
                if (v.AttachedBlip != null) {
                    v.AttachedBlip.DisplayType = BlipDisplayType.NoDisplay;
                    v.AttachedBlip.Delete();
                }
                v.IsPersistent = false;
                spawnedVehicles.Remove(vd);
                JsonVehicleStorage.RemoveVehicle(vd);
            } catch (Exception e) {
                Logger.LogError(e.ToString());
                return false;
            }

            return true;
        }

        bool DespawnVehicle(Vehicle v, VehicleData vd) {
            try {
                lastVehicle = null;
                if (v.AttachedBlip != null) {
                    v.AttachedBlip.DisplayType = BlipDisplayType.NoDisplay;
                    v.AttachedBlip.Delete();
                }
                v.IsPersistent = false;
                v.Delete();
                spawnedVehicles.Remove(vd);
            } catch (Exception e) {
                Logger.LogError(e.ToString());
                return false;
            }

            return true;
        }

        bool SpawnVehicle(VehicleData vd, bool nearby = false) {
            Vehicle vehicle = null;
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
            spawnedVehicles.Add(vd);
            return true;
        }

        void LoadVehicles() {
            VehicleData[] jsonVehicles = new VehicleData[0];

            try {
                jsonVehicles = JsonVehicleStorage.GetVehicles();
            } catch (Exception e) {
                Logger.LogError(e.ToString());
            }

            try {
                for (int i = 0; i < jsonVehicles.Count(); i++) {
                    if (!spawnedVehicles.Contains(jsonVehicles[i])) {
                        SpawnVehicle(jsonVehicles[i]);
                    } else {
                        GTA.UI.Notification.Show($"Vehicle {jsonVehicles[i].VehicleName} {jsonVehicles[i].LicensePlate.Trim()} is already loaded");
                    }
                }
            } catch (Exception e) {
                Logger.LogError(e.ToString());
            }
        }

        void UnsaveVehicles() {
            VehicleData[] savedVehicles = JsonVehicleStorage.GetVehicles();
            for (int i = 0; i < savedVehicles.Count(); i++) {
                VehicleData vd = savedVehicles[i];
                Vehicle v = (Vehicle) Entity.FromHandle(vd.Handle);

                // Check if vehicle is spawned to remove persistency and blip correctly
                (v, vd) = GetSpawnedIfPossible(vd);
                UnsaveVehicle(v, vd);
            }

            GTA.UI.Notification.Show("All vehicles have been unsaved");
        }

        void DespawnVehicles() {
            while (spawnedVehicles.Count > 0) {
                VehicleData vd = spawnedVehicles[spawnedVehicles.Count - 1];
                Vehicle v = (Vehicle) Entity.FromHandle(vd.Handle);
                DespawnVehicle(v, vd);
            }

            GTA.UI.Notification.Show("All vehicles have been despawned");
        }

        void SaveCurrentVehicle() {
            Ped player = Game.Player.Character;

            if (player.IsInVehicle()) {
                Vehicle currentVeh = player.CurrentVehicle;
                VehicleData vd = VehicleUtilities.CreateInfo(currentVeh);
                VehicleData[] savedVehicles = JsonVehicleStorage.GetVehicles();
                if (!savedVehicles.Contains(vd)) {
                    if (savedVehicles.Count() < vehicleLimit) {
                        SaveVehicle(currentVeh, vd);
                        GTA.UI.Notification.Show($"Vehicle {currentVeh.DisplayName} {currentVeh.Mods.LicensePlate.Trim()} saved");
                    } else {
                        GTA.UI.Notification.Show($"You can't save more than {vehicleLimit} vehicles");
                    }
                } else {
                    UnsaveVehicle(currentVeh, vd);
                    SaveVehicle(currentVeh, vd);
                    GTA.UI.Notification.Show($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} has been overridden");
                }
            } else if (player.IsOnFoot) {
                GTA.UI.Notification.Show("Player is not in a vehicle");
            }
        }

        void UnsaveCurrentVehicle() {
            Ped player = Game.Player.Character;

            if (player.IsInVehicle()) {
                Vehicle currentVeh = player.CurrentVehicle;
                VehicleData vd = VehicleUtilities.CreateInfo(currentVeh);
                VehicleData[] savedVehicles = JsonVehicleStorage.GetVehicles();

                if (savedVehicles.Contains(vd)) {
                    UnsaveVehicle(currentVeh, vd);
                    GTA.UI.Notification.Show($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} is unsaved");
                }
            } else if (player.IsOnFoot) {
                GTA.UI.Notification.Show("Player is not in a vehicle");
            }
        }
    }
}