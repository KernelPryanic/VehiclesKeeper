using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;

namespace VehicleKeeper {
    public class VehicleKeeper : Script {
        ScriptSettings config;
        int vehicleLimit;
        Keys saveKey;
        Keys unsaveKey;
        Keys menuKey;

        MenuPool menuPool;
        UIMenu mainMenu;
        UIMenu vehicleMenu;
        bool started = false;
        int period = 0;
        List<SaveableVehicle> spawnedVehicles = new List<SaveableVehicle>();
        Vehicle lastVehicle = null;

        public VehicleKeeper() {
            new Model();

            MainMenu();

            Tick += OnTick;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;

            Interval = 10;

            config = ScriptSettings.Load(@"./scripts/VehicleKeeper.ini");
            vehicleLimit = config.GetValue("Configuration", "Vehicle limit", 8);
            saveKey = config.GetValue("Configuration", "Save key", Keys.IMENonconvert);
            unsaveKey = config.GetValue("Configuration", "Unsave key", Keys.IMENonconvert);
            menuKey = config.GetValue("Configuration", "Menu key", Keys.T);

            string basePath = config.GetValue("Configuration", "vehiclePersistencePath", @"./scripts/VehicleKeeper");

            //Initialiseer het pad in de json klasse
            JsonVehicleStorage.InitializeBasePath(basePath);
        }

        public void OnTick(object sender, EventArgs eventArgs) {
            if (!started) {
                started = true;
                LoadVehicles();
            }

            if (menuPool != null && menuPool.IsAnyMenuOpen()) {
                menuPool.ProcessMenus(); // Loads all the menus
            }

            if (period < 5) {
                period++;
                return;
            } else {
                period = 0;
            }

            if (lastVehicle != null && Game.Player != null &&
                lastVehicle != Game.Player.Character.CurrentVehicle &&
                !Game.Player.Character.IsGettingIntoAVehicle &&
                World.GetZoneName(Game.Player.Character.Position) != "San Andreas"
            ) {
                SaveableVehicle sv = VehicleUtilities.CreateInfo(lastVehicle);
                SaveableVehicle[] savedVehicles = JsonVehicleStorage.GetVehicles();
                if (savedVehicles.Contains(sv)) {
                    UnsaveVehicle(sv, lastVehicle);
                    SaveVehicle(sv, lastVehicle);
                }
            }

            for (int i = 0; i < spawnedVehicles.Count; i++) {
                Vehicle v = new Vehicle(spawnedVehicles[i].Handle);
                if (v.Health == 0 || v.IsDead) {
                    DespawnVehicle(spawnedVehicles[i], v);
                    UI.Notify($"Vehicle {v.DisplayName} is destroyed");
                }
            }

            if (Game.Player != null && Game.Player.Character.IsInVehicle() &&
                World.GetZoneName(Game.Player.Character.Position) != "San Andreas"
            ) {
                lastVehicle = Game.Player.Character.CurrentVehicle;
            } else {
                lastVehicle = null;
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
            if ((string) listItem.Items[listItem.Index] == "Spawn") {
                SaveableVehicle sv = JsonVehicleStorage.GetVehicle(VehicleUtilities.GetHashString(listItem.Text));
                if (sv == null) {
                    UI.Notify($"This vehicle doesn't exist in your list");
                    return;
                }

                if (!spawnedVehicles.Contains(sv)) {
                    if (SpawnVehicle(sv)) {
                        UI.Notify($"Vehicle {sv.VehicleName} {sv.NumberPlate.Trim()} spawned successully");
                    }
                } else {
                    UI.Notify($"Vehicle {sv.VehicleName} {sv.NumberPlate.Trim()} is already spawned");
                }
            }
            if ((string) listItem.Items[listItem.Index] == "Despawn") {
                SaveableVehicle sv = JsonVehicleStorage.GetVehicle(VehicleUtilities.GetHashString(listItem.Text));
                if (sv == null) {
                    UI.Notify($"This vehicle doesn't exist in your list");
                    return;
                }

                if (!spawnedVehicles.Contains(sv)) {
                    UI.Notify($"Vehicle {sv.VehicleName} {sv.NumberPlate.Trim()} is not spawned");
                } else {
                    Vehicle v;
                    (v, sv) = GetSpawnedIfPossible(sv);
                    if (DespawnVehicle(sv, v)) {
                        UI.Notify($"Vehicle {sv.VehicleName} {sv.NumberPlate.Trim()} despawned successully");
                    }
                }
            }
            if ((string) listItem.Items[listItem.Index] == "Save") {
                SaveableVehicle sv = JsonVehicleStorage.GetVehicle(VehicleUtilities.GetHashString(listItem.Text));
                if (sv == null) {
                    UI.Notify($"This vehicle doesn't exist in your list");
                    return;
                }

                if (!spawnedVehicles.Contains(sv)) {
                    UI.Notify($"Vehicle {sv.VehicleName} {sv.NumberPlate.Trim()} is not spawned");
                    return;
                }

                Vehicle v;
                (v, sv) = GetSpawnedIfPossible(sv);
                UnsaveVehicle(sv, v);
                if (SaveVehicle(sv, v)) {
                    UI.Notify($"Vehicle {sv.VehicleName} {sv.NumberPlate.Trim()} has been overridden");
                } else {
                    UI.Notify($"Vehicle {sv.VehicleName} {sv.NumberPlate.Trim()} can't be overridden");
                }
            }
            if ((string) listItem.Items[listItem.Index] == "Unsave") {
                SaveableVehicle sv = JsonVehicleStorage.GetVehicle(VehicleUtilities.GetHashString(listItem.Text));
                if (sv == null) {
                    UI.Notify($"This vehicle doesn't exist in your list");
                    return;
                }
                Vehicle v;
                (v, sv) = GetSpawnedIfPossible(sv);

                if (UnsaveVehicle(sv, v)) {
                    UI.Notify($"Vehicle {sv.VehicleName} {sv.NumberPlate.Trim()} has been unsaved");
                } else {
                    UI.Notify($"Vehicle {sv.VehicleName} {sv.NumberPlate.Trim()} can't be unsaved");
                }
            }
            if ((string) listItem.Items[listItem.Index] == "Set spawn location") {
                Vector3 waypoint = World.GetWaypointPosition();
                if (waypoint.Length() == 0) {
                    UI.Notify("Please set waypoint to identify target position");
                } else {
                    SaveableVehicle sv = JsonVehicleStorage.GetVehicle(VehicleUtilities.GetHashString(listItem.Text));
                    if (sv == null) {
                        UI.Notify($"This vehicle doesn't exist in your list");
                        return;
                    }

                    Vehicle v;
                    (v, sv) = GetSpawnedIfPossible(sv);
                    sv.Position = waypoint;
                    UnsaveVehicle(sv, v);
                    SaveVehicle(sv, v);
                    UI.Notify($"Spawn position was refreshed for {sv.VehicleName} {sv.NumberPlate.Trim()}");
                }
            }
        }

        public void OnKeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == menuKey && !menuPool.IsAnyMenuOpen()) {
                SaveableVehicle[] savedVehicles = JsonVehicleStorage.GetVehicles();
                mainMenu.Visible = !mainMenu.Visible;
                vehicleMenu.MenuItems.Clear();

                foreach (SaveableVehicle sv in savedVehicles) {
                    if (vehicleMenu.MenuItems.Count <= savedVehicles.Count()) {
                        UIMenuListItem vehicleItem = new UIMenuListItem($"{sv.VehicleName} {sv.NumberPlate.Trim()}",
                            new List<object> { "Spawn", "Despawn", "Save", "Unsave", "Set spawn location" }, 0);
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
            mainMenu = new UIMenu("Vehicle Keeper", "Version 2.1.0");

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
                Blip blip = Function.Call<Blip>(Hash.ADD_BLIP_FOR_ENTITY, v); // Sets blip on vehicle
                blip.IsShortRange = true;
                Function.Call(Hash.SET_BLIP_SPRITE, blip, 225); // Sets vehicle blip icon
                Function.Call(Hash.SET_BLIP_DISPLAY, blip, 2); // Displays the blip icon
                Function.Call(Hash.SET_BLIP_COLOUR, blip, 7);
                Function.Call(Hash.SET_BLIP_PRIORITY, blip, 13);
            } catch (Exception e) {
                UI.Notify(e.Message);
            }
        }

        (Vehicle, SaveableVehicle) GetSpawnedIfPossible(SaveableVehicle sv) {
            int spawned = spawnedVehicles.IndexOf(sv);
            if (spawned > -1) {
                return (new Vehicle(spawnedVehicles[spawned].Handle), spawnedVehicles[spawned]);
            }

            return (new Vehicle(sv.Handle), sv);
        }

        bool SaveVehicle(SaveableVehicle sv, Vehicle v) {
            try {
                SetBlipOnVehicle(v);
                v.IsPersistent = true;
                spawnedVehicles.Add(sv);
                JsonVehicleStorage.SaveVehicle(sv);
            } catch (Exception e) {
                UI.Notify(e.Message);
                return false;
            }

            return true;
        }

        bool UnsaveVehicle(SaveableVehicle sv, Vehicle v) {
            try {
                v.IsPersistent = false;
                v.CurrentBlip.Remove();
                spawnedVehicles.Remove(sv);
                JsonVehicleStorage.RemoveVehicle(sv);
            } catch (Exception e) {
                UI.Notify(e.Message);
                return false;
            }

            return true;
        }

        bool DespawnVehicle(SaveableVehicle sv, Vehicle v) {
            try {
                v.IsPersistent = false;
                v.CurrentBlip.Remove();
                spawnedVehicles.Remove(sv);
                v.Delete();
            } catch (Exception e) {
                UI.Notify(e.Message);
                return false;
            }

            return true;
        }

        bool SpawnVehicle(SaveableVehicle sv) {
            Vehicle vehicle = null;
            try {
                vehicle = VehicleUtilities.CreateVehicleFromData(ref sv);
            } catch (Exception e) {
                UI.Notify(e.Message);
                return false;
            }

            if (vehicle == null) {
                return false;
            }

            SetBlipOnVehicle(vehicle);
            spawnedVehicles.Add(sv);
            return true;
        }

        void LoadVehicles() {
            SaveableVehicle[] jsonVehicles = new SaveableVehicle[0];

            try {
                //Haal opgeslagen auto's op
                jsonVehicles = JsonVehicleStorage.GetVehicles();
            } catch (Exception e) {
                UI.Notify(e.Message);
            }

            try {
                for (int i = 0; i < jsonVehicles.Count(); i++) {
                    if (!spawnedVehicles.Contains(jsonVehicles[i])) {
                        SpawnVehicle(jsonVehicles[i]);
                    } else {
                        UI.Notify($"Vehicle {jsonVehicles[i].VehicleName} {jsonVehicles[i].NumberPlate.Trim()} is already loaded");
                    }
                }
            } catch (Exception e) {
                UI.Notify(e.Message);
            }
        }

        void UnsaveVehicles() {
            SaveableVehicle[] savedVehicles = JsonVehicleStorage.GetVehicles();
            for (int i = 0; i < savedVehicles.Count(); i++) {
                SaveableVehicle sv = savedVehicles[i];
                Vehicle v = new Vehicle(sv.Handle);

                // Check if vehicle is spawned to remove persistency and blip correctly
                (v, sv) = GetSpawnedIfPossible(sv);
                UnsaveVehicle(sv, v);
            }

            UI.Notify("All vehicles have been unsaved");
        }

        void DespawnVehicles() {
            while (spawnedVehicles.Count > 0) {
                SaveableVehicle sv = spawnedVehicles[spawnedVehicles.Count - 1];
                Vehicle v = new Vehicle(sv.Handle);
                DespawnVehicle(sv, v);
            }

            UI.Notify("All vehicles have been removed");
        }

        void SaveCurrentVehicle() {
            Ped player = Game.Player.Character;

            if (player.IsInVehicle()) {
                Vehicle currentVeh = player.CurrentVehicle;
                SaveableVehicle sv = VehicleUtilities.CreateInfo(currentVeh);
                SaveableVehicle[] savedVehicles = JsonVehicleStorage.GetVehicles();
                if (!savedVehicles.Contains(sv)) {
                    if (savedVehicles.Count() < vehicleLimit) {
                        SaveVehicle(sv, currentVeh);
                        UI.Notify($"Vehicle {currentVeh.DisplayName} {currentVeh.NumberPlate.Trim()} saved");
                    } else {
                        UI.Notify($"You can't save more than {vehicleLimit} vehicles");
                    }
                } else {
                    UnsaveVehicle(sv, currentVeh);
                    SaveVehicle(sv, currentVeh);
                    UI.Notify($"Vehicle {sv.VehicleName} {sv.NumberPlate.Trim()} has been overridden");
                }
            } else if (player.IsOnFoot) {
                UI.Notify("Player is not in a vehicle");
            }
        }

        void UnsaveCurrentVehicle() {
            Ped player = Game.Player.Character;

            if (player.IsInVehicle()) {
                Vehicle currentVeh = player.CurrentVehicle;
                SaveableVehicle sv = VehicleUtilities.CreateInfo(currentVeh);
                SaveableVehicle[] savedVehicles = JsonVehicleStorage.GetVehicles();

                if (savedVehicles.Contains(sv)) {
                    UnsaveVehicle(sv, currentVeh);
                    UI.Notify($"Vehicle {sv.VehicleName} {sv.NumberPlate.Trim()} is unsaved");
                }
            } else if (player.IsOnFoot) {
                UI.Notify("Player is not in a vehicle");
            }
        }
    }
}