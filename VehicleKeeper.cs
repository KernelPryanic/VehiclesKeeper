using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using NativeUI;

namespace VehicleKeeper {
    public static class Logger {
        public static void LogError(object message) {
            File.AppendAllText("VehicleKeeper.log", DateTime.Now + " [Error] " + message + Environment.NewLine);
        }
    }

    public class VehicleKeeper : Script {
        ScriptSettings Config;
        int VehicleLimit;
        Keys SaveKey;
        Keys UnsaveKey;
        Keys MenuKey;

        MenuPool MenuPool;
        UIMenu MainMenu;
        UIMenu VehicleMenu;
        UIMenuListItem BlipColorListItem;
        int Period = 0;
        List<VehicleData> SpawnedVehicles = new List<VehicleData>();
        Vehicle LastVehicle = null;

        BlipColor BlipColor = BlipColor.Blue;

        public VehicleKeeper() {
            new Model();

            MainMenuInit();

            Config = ScriptSettings.Load(@"./scripts/VehicleKeeper.ini");
            VehicleLimit = Config.GetValue("Configuration", "VehicleLimit", 8);
            MenuKey = Config.GetValue("Configuration", "MenuKey", Keys.T);
            SaveKey = Config.GetValue("Configuration", "SaveKey", Keys.X);
            UnsaveKey = Config.GetValue("Configuration", "UnsaveKey", Keys.Z);
            BlipColor = Config.GetValue("Configuration", "BlipColor", BlipColor.Blue);
            BlipColorListItem.Index = BlipColorListItem.Items.FindIndex(x => (BlipColor) x == BlipColor);

            string basePath = Config.GetValue("Configuration", "VehiclePersistencePath", @"./scripts/VehicleKeeper");

            JsonVehicleStorage.Initialize(basePath);

            Tick += OnTick;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
        }

        public void OnTick(object sender, EventArgs eventArgs) {
            if (MenuPool != null && MenuPool.IsAnyMenuOpen()) {
                MenuPool.ProcessMenus(); // Loads all the menus
            }

            if (Period < 30) {
                Period++;
                return;
            } else {
                Period = 0;
            }

            if (LastVehicle != null && Game.Player != null &&
                // LastVehicle != Game.Player.Character.CurrentVehicle &&
                // !Game.Player.Character.IsGettingIntoVehicle &&
                // !Game.Player.Character.IsJumpingOutOfVehicle &&
                World.GetZoneDisplayName(Game.Player.Character.Position) != "San Andreas"
            ) {
                VehicleData vd = VehicleUtilities.CreateInfo(LastVehicle);
                List<VehicleData> savedVehicles = JsonVehicleStorage.GetVehicles();
                if (savedVehicles.Contains(vd)) {
                    UpdateVehicleData(LastVehicle, vd);
                }
            }

            for (int i = 0; i < SpawnedVehicles.Count; i++) {
                Vehicle v = (Vehicle) Entity.FromHandle(SpawnedVehicles[i].Handle);
                if (v == null) {
                    GTA.UI.Notification.Show($"Vehicle {SpawnedVehicles[i].VehicleName} {SpawnedVehicles[i].LicensePlate.Trim()} is despawned");
                    DespawnVehicle(v, SpawnedVehicles[i]);
                } else if (v.IsConsideredDestroyed) {
                    GTA.UI.Notification.Show($"Vehicle {SpawnedVehicles[i].VehicleName} {SpawnedVehicles[i].LicensePlate.Trim()} is destroyed");
                    DespawnVehicle(v, SpawnedVehicles[i]);
                }
            }

            if (Game.Player != null && Game.Player.Character.IsInVehicle() &&
                World.GetZoneDisplayName(Game.Player.Character.Position) != "San Andreas"
            ) {
                LastVehicle = Game.Player.Character.CurrentVehicle;
            }
        }

        void SetBlipOnVehicle(Vehicle v) {
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

        (Vehicle, VehicleData) GetSpawnedIfPossible(VehicleData vd) {
            int spawned = SpawnedVehicles.IndexOf(vd);
            if (spawned > -1) {
                return ((Vehicle) Entity.FromHandle(SpawnedVehicles[spawned].Handle), SpawnedVehicles[spawned]);
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
                    if (v.AttachedBlip != null) {
                        v.AttachedBlip.Delete();
                    }
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
                    if (v.AttachedBlip == null) {
                        SetBlipOnVehicle(v);
                    }
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
                    if (v.AttachedBlip != null) {
                        v.AttachedBlip.Delete();
                    }
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
                        GTA.UI.Notification.Show($"Vehicle {jsonVehicles[i].VehicleName} {jsonVehicles[i].LicensePlate.Trim()} is already loaded");
                    }
                }
            } catch (Exception e) {
                Logger.LogError(e.ToString());
            }
        }

        void UnsaveVehicles() {
            List<VehicleData> savedVehicles = JsonVehicleStorage.GetVehicles();
            while (savedVehicles.Count > 0) {
                VehicleData vd = savedVehicles[0];
                Vehicle v = (Vehicle) Entity.FromHandle(vd.Handle);

                // Check if vehicle is spawned to remove persistency and blip correctly
                (v, vd) = GetSpawnedIfPossible(vd);
                UnsaveVehicle(v, vd);
            }

            GTA.UI.Notification.Show("All vehicles have been unsaved");
        }

        void DespawnVehicles() {
            while (SpawnedVehicles.Count > 0) {
                VehicleData vd = SpawnedVehicles[0];
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
                List<VehicleData> savedVehicles = JsonVehicleStorage.GetVehicles();
                if (!savedVehicles.Contains(vd)) {
                    if (savedVehicles.Count() < VehicleLimit) {
                        SaveVehicle(currentVeh, vd);
                        GTA.UI.Notification.Show($"Vehicle {currentVeh.DisplayName} {currentVeh.Mods.LicensePlate.Trim()} saved");
                    } else {
                        GTA.UI.Notification.Show($"You can't save more than {VehicleLimit} vehicles");
                    }
                } else {
                    UpdateVehicleData(currentVeh, vd);
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
                List<VehicleData> savedVehicles = JsonVehicleStorage.GetVehicles();

                if (savedVehicles.Contains(vd)) {
                    UnsaveVehicle(currentVeh, vd);
                    GTA.UI.Notification.Show($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} is unsaved");
                }
            } else if (player.IsOnFoot) {
                GTA.UI.Notification.Show("Player is not in a vehicle");
            }
        }

        void OnMainMenuItemSelect(UIMenu sender, UIMenuItem item, int index) {
            if (item.Text == "Save current vehicle") SaveCurrentVehicle();
            if (item.Text == "Spawn all") LoadVehicles();
            if (item.Text == "Despawn all") DespawnVehicles();
            if (item.Text == "Unsave all") UnsaveVehicles();
            if (item.Text == "Blip color") {
                UIMenuListItem listItem = (UIMenuListItem) item;
                BlipColor = (BlipColor) listItem.Items[listItem.Index];
                Config.SetValue("Configuration", "BlipColor", BlipColor);
                Config.Save();
                foreach (VehicleData vd in SpawnedVehicles) {
                    Vehicle v;
                    (v, _) = GetSpawnedIfPossible(vd);
                    if (v.AttachedBlip != null) {
                        v.AttachedBlip.Color = BlipColor;
                    }
                }
            }
            if (item.Text == "Exit") MainMenu.GoBack();
        }

        void OnVehicleMenuItemSelect(UIMenu sender, UIMenuItem item, int index) {
            UIMenuListItem listItem = (UIMenuListItem) item;
            VehicleData vd = JsonVehicleStorage.GetVehicle(VehicleUtilities.GetHashString(listItem.Text));
            if (vd == null) {
                GTA.UI.Notification.Show($"This vehicle doesn't exist in your list");
                return;
            }
            Vehicle v;
            switch ((string) listItem.Items[listItem.Index]) {
                case "Spawn":
                    if (!SpawnedVehicles.Contains(vd)) {
                        if (SpawnVehicle(vd)) {
                            GTA.UI.Notification.Show($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} spawned successully");
                        }
                    } else {
                        GTA.UI.Notification.Show($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} is already spawned");
                    }
                    break;
                case "Spawn nearby":
                    if (!SpawnedVehicles.Contains(vd)) {
                        if (SpawnVehicle(vd, true)) {
                            GTA.UI.Notification.Show($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} spawned successully");
                        }
                    } else {
                        GTA.UI.Notification.Show($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} is already spawned");
                    }
                    break;
                case "Despawn":
                    if (!SpawnedVehicles.Contains(vd)) {
                        GTA.UI.Notification.Show($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} is not spawned");
                    } else {
                        (v, vd) = GetSpawnedIfPossible(vd);
                        if (DespawnVehicle(v, vd)) {
                            GTA.UI.Notification.Show($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} despawned successully");
                        }
                    }
                    break;
                case "Save":
                    if (!SpawnedVehicles.Contains(vd)) {
                        GTA.UI.Notification.Show($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} is not spawned");
                        return;
                    }

                    (v, vd) = GetSpawnedIfPossible(vd);
                    UnsaveVehicle(v, vd);
                    if (SaveVehicle(v, vd)) {
                        GTA.UI.Notification.Show($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} has been overridden");
                    } else {
                        GTA.UI.Notification.Show($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} can't be overridden");
                    }
                    break;
                case "Unsave":
                    (v, vd) = GetSpawnedIfPossible(vd);

                    if (UnsaveVehicle(v, vd)) {
                        GTA.UI.Notification.Show($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} has been unsaved");
                    } else {
                        GTA.UI.Notification.Show($"Vehicle {vd.VehicleName} {vd.LicensePlate.Trim()} can't be unsaved");
                    }
                    break;
                case "Set spawn location":
                    Vector3 waypoint = World.WaypointPosition;
                    if (waypoint.Length() == 0) {
                        GTA.UI.Notification.Show("Please set waypoint to identify target position");
                    } else if (Game.Player.Character.IsInVehicle(LastVehicle)) {
                        GTA.UI.Notification.Show("You should leave the vehicle first as the spawn location is being overridden while you're in the car");
                    } else {
                        (v, vd) = GetSpawnedIfPossible(vd);
                        vd.Position = waypoint;
                        UnsaveVehicle(v, vd);
                        SaveVehicle(v, vd);
                        GTA.UI.Notification.Show($"Spawn position was refreshed for {vd.VehicleName} {vd.LicensePlate.Trim()}");
                    }
                    break;
            }
        }

        public void OnKeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == MenuKey && !MenuPool.IsAnyMenuOpen()) {
                List<VehicleData> savedVehicles = JsonVehicleStorage.GetVehicles();
                MainMenu.Visible = !MainMenu.Visible;
                VehicleMenu.MenuItems.Clear();

                foreach (VehicleData vd in savedVehicles) {
                    if (VehicleMenu.MenuItems.Count <= savedVehicles.Count()) {
                        UIMenuListItem vehicleItem = new UIMenuListItem($"{vd.VehicleName} {vd.LicensePlate.Trim()}",
                            new List<object> { "Spawn", "Spawn nearby", "Despawn", "Save", "Unsave", "Set spawn location" }, 0);
                        VehicleMenu.AddItem(vehicleItem);
                    }
                }
            }
        }

        public void OnKeyUp(object sender, KeyEventArgs e) {
            Keys[] keys = new [] { SaveKey, UnsaveKey };

            if (keys.Any(x => x == e.KeyCode)) {
                if (e.KeyCode == SaveKey) {
                    SaveCurrentVehicle();
                } else if (e.KeyCode == UnsaveKey) {
                    UnsaveCurrentVehicle();
                }
            }
        }

        void MainMenuInit() {
            MenuPool = new MenuPool();
            MainMenu = new UIMenu("Vehicle Keeper", "Version 3.4.0");

            MenuPool.Add(MainMenu); // Adds MainMenu to the pool
            VehicleMenu = MenuPool.AddSubMenu(MainMenu, "Saved vehicles"); // Submenu options
            UIMenuItem saveCurrent = new UIMenuItem("Save current vehicle");
            MainMenu.AddItem(saveCurrent);
            UIMenuItem spawnVehicles = new UIMenuItem("Spawn all");
            MainMenu.AddItem(spawnVehicles);
            UIMenuItem despawnVehicles = new UIMenuItem("Despawn all");
            MainMenu.AddItem(despawnVehicles);
            UIMenuItem unsaveButton = new UIMenuItem("Unsave all");
            MainMenu.AddItem(unsaveButton);
            BlipColorListItem = new UIMenuListItem("Blip color",
                new List<object> {
                    BlipColor.Blue,
                    BlipColor.Green,
                    BlipColor.Purple,
                    BlipColor.Red,
                    BlipColor.Orange,
                    BlipColor.Yellow,
                    BlipColor.Pink,
                    BlipColor.White
                },
                0
            );
            MainMenu.AddItem(BlipColorListItem);
            UIMenuItem exitButton = new UIMenuItem("Exit");
            MainMenu.AddItem(exitButton);

            MainMenu.OnItemSelect += OnMainMenuItemSelect;
            VehicleMenu.OnItemSelect += OnVehicleMenuItemSelect;
        }
    }
}