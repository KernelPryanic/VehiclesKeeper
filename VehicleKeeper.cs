using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using LemonUI.Menus;

namespace VehicleKeeper {
	// Resolves the mod's writable data directory.
	//
	// Runtime files (log, config, snapshot) MUST NOT live anywhere under the game folder.
	// GTA V Enhanced's native ScriptHookV host opens every file present under the game
	// directory at launch — game root AND scripts\ AND its subfolders — with a
	// no-write-share, no-delete, no-rename lock held for the whole session. So any file
	// that already exists at launch can never be rewritten, deleted, or replaced (the
	// long-standing "log won't update / settings don't persist" bug). Proven with handle64
	// (GTA5_Enhanced.exe owns the handles) + live write-probes: only files that did NOT
	// exist at launch are writable, and even the game root's own ScriptHookV.log is locked.
	// A subfolder under scripts\ does NOT escape this.
	//
	// The fix is to write OUTSIDE the game tree entirely: %APPDATA%\...\VehicleKeeper\. The
	// host never scans there, so fixed filenames stay writable every session with normal
	// permissions (verified: an existing file there rewrites/appends fine while the game
	// runs). Init() is kept for API compatibility but the data dir no longer depends on the
	// DLL location.
	public static class ScriptPaths {
		// Group all runtime data under %APPDATA%\GTA V Mods\KernelPryanic\ so this author's
		// GTA V mods share one tree instead of scattering top-level folders.
		const string GameFolderName = "GTA V Mods";
		const string VendorFolderName = "KernelPryanic";
		// This mod's own subfolder under the shared parent.
		const string DataFolderName = "VehicleKeeper";

		// The DLL folder, recorded for reference/diagnostics only — NOT used for writes.
		static string directory = AppDomain.CurrentDomain.BaseDirectory ?? ".";

		// The folder the DLL lives in (scripts\). Diagnostic only; not a write target.
		public static string Directory => directory;

		// The mod's writable data folder: %APPDATA%\GTA V Mods\KernelPryanic\VehicleKeeper\.
		// All runtime writes (log, config, persistence) route through here, outside the
		// game's lock scope. Created on first access.
		public static string DataDirectory {
			get {
				string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string path = Path.Combine(appData, GameFolderName, VendorFolderName, DataFolderName);
				if (!System.IO.Directory.Exists(path)) {
					try {
						System.IO.Directory.CreateDirectory(path);
					} catch {
						// If creation fails, callers' own IO guards handle the fallout;
						// never crash path resolution.
					}
				}
				return path;
			}
		}

		// Kept for call-site compatibility (the Script ctor still passes BaseDirectory).
		// Records the DLL folder for diagnostics; the data dir is %APPDATA%, independent
		// of where the DLL was loaded from.
		public static void Init(string baseDirectory) {
			if (!string.IsNullOrEmpty(baseDirectory)) {
				directory = baseDirectory;
			}
		}

		// Resolve a runtime file inside the writable data folder (log/config/persistence).
		public static string For(string fileName) => Path.Combine(DataDirectory, fileName);
	}

	public static class Logger {
		// Resolved on each use, not cached: ScriptPaths.Directory is finalized only
		// after the Script constructor calls ScriptPaths.Init, which runs after this
		// type is first touched. Caching here would freeze the pre-Init fallback path.
		static string LogFilePath => ScriptPaths.For("VehicleKeeper.log");

		public static void ClearLog() {
			try {
				File.WriteAllText(LogFilePath, string.Empty);
			} catch {
				// Logging must never crash the script.
			}
		}

		public static void Log(object message) {
			Write("INFO", message);
		}

		public static void LogError(object message) {
			Write("ERROR", message);
		}

		static void Write(string level, object message) {
			try {
				File.AppendAllText(LogFilePath, DateTime.Now + " [" + level + "] " + message + Environment.NewLine);
			} catch {
				// Logging must never crash the script.
			}
		}
	}

	public class VehicleKeeper : Script {
		readonly ScriptSettings Config;
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
			// Record the DLL folder for diagnostics. Writes go to %APPDATA% regardless,
			// so this is not load-bearing, but keep it before any logging/config IO.
			ScriptPaths.Init(BaseDirectory);

			Logger.ClearLog();
			Logger.Log("VehicleKeeper started. Files dir: " + ScriptPaths.DataDirectory);

			MainMenuInit();

			// Persistence and config live in the mod's writable data folder under
			// %APPDATA% (outside the game tree the native host locks).
			string defaultPersistencePath = ScriptPaths.DataDirectory;

			Config = ScriptSettings.Load(ScriptPaths.For("VehicleKeeper.ini"));

			// Define default values
			SetConfigValueIfNotDefined("Configuration", "MenuKey", Keys.T);
			SetConfigValueIfNotDefined("Configuration", "SaveKey", Keys.X);
			SetConfigValueIfNotDefined("Configuration", "UnsaveKey", Keys.Z);
			SetConfigValueIfNotDefined("Configuration", "BlipColor", BlipColor.Blue);
			SetConfigValueIfNotDefined("Configuration", "BlipDistance", 500f);
			SetConfigValueIfNotDefined("Configuration", "VehiclePersistencePath", defaultPersistencePath);

			// Read configuration values
			MenuKey = Config.GetValue("Configuration", "MenuKey", Keys.T);
			SaveKey = Config.GetValue("Configuration", "SaveKey", Keys.X);
			UnsaveKey = Config.GetValue("Configuration", "UnsaveKey", Keys.Z);
			BlipColor = Config.GetValue("Configuration", "BlipColor", BlipColor.Blue);
			BlipColorListItem.SelectedItem = BlipColorListItem.Items.First(x => x == BlipColor);
			BlipDistance = Config.GetValue("Configuration", "BlipDistance", 500f);
			BlipDistanceItem.SelectedItem = BlipDistanceItem.Items.First(x => x == BlipDistance);

			string basePath = Config.GetValue("Configuration", "VehiclePersistencePath", defaultPersistencePath);
			XmlVehicleStorage.Initialize(basePath);

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
			List<VehicleData> savedVehicles = XmlVehicleStorage.GetVehicles();
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
					// Only persist drift for a vehicle that is still tracked. Without
					// this, unsaving the car you are sitting in would be undone on the
					// next tick (UpdateVehicleData re-adds it to the store).
					if (SpawnedVehicles.Contains(vd)) {
						UpdateVehicleData(LastVehicle, vd);
					}
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
				// A handle can be non-null yet point to a despawned entity; treat that
				// as gone too, otherwise the .IsConsideredDestroyed/.Position access
				// below throws and aborts the rest of the blip loop for this tick.
				if (v == null || !v.Exists()) {
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
			// A handle can be non-null yet point to a despawned entity; .Exists()
			// guards the .AttachedBlip/.AddBlip access from throwing on a dead vehicle.
			if (v == null || !v.Exists() || v.AttachedBlip != null) {
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
			// See SetBlipOnVehicle: a non-null handle may be a despawned entity, so
			// .Exists() must gate the .AttachedBlip access.
			if (v != null && v.Exists() && v.AttachedBlip != null) {
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
				XmlVehicleStorage.SaveVehicle(vd);
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
					// Stop the per-tick auto-save from resurrecting this vehicle: if
					// it is the one being tracked for drift, clear the target.
					if (v == LastVehicle) {
						LastVehicle = null;
					}
				}
				SpawnedVehicles.Remove(vd);
				XmlVehicleStorage.RemoveVehicle(vd);
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
				XmlVehicleStorage.UpdateVehicle(vd);
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
			List<VehicleData> savedVehicles = new List<VehicleData>();

			try {
				savedVehicles = XmlVehicleStorage.GetVehicles();
			} catch (Exception e) {
				Logger.LogError(e.ToString());
			}

			try {
				for (int i = 0; i < savedVehicles.Count(); i++) {
					if (!SpawnedVehicles.Contains(savedVehicles[i])) {
						SpawnVehicle(savedVehicles[i]);
					} else {
						GTA.UI.Notification.PostTicker($"Vehicle {savedVehicles[i].VehicleName} {savedVehicles[i].LicensePlate.Trim()} is already loaded", false);
					}
				}
			} catch (Exception e) {
				Logger.LogError(e.ToString());
			}
		}

		void UnsaveVehicles() {
			// Drain a snapshot of the saved list: UnsaveVehicle removes from the
			// underlying store, so iterate a copy rather than the live list.
			foreach (VehicleData saved in XmlVehicleStorage.GetVehicles().ToList()) {
				// If the vehicle is currently spawned, use its tracked instance so
				// persistency and its blip are cleared correctly.
				(Vehicle v, VehicleData vd) = GetSpawnedIfPossible(saved);
				UnsaveVehicle(v, vd);
			}

			RebuildVehicleMenu();
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
				List<VehicleData> savedVehicles = XmlVehicleStorage.GetVehicles();
				if (!savedVehicles.Contains(vd)) {
					SaveVehicle(currentVeh, vd);
					RebuildVehicleMenu();
					GTA.UI.Notification.PostTicker($"Vehicle {currentVeh.DisplayName} {currentVeh.Mods.LicensePlate.Trim()} saved", false);
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
				List<VehicleData> savedVehicles = XmlVehicleStorage.GetVehicles();

				if (savedVehicles.Contains(vd)) {
					UnsaveVehicle(currentVeh, vd);
					RebuildVehicleMenu();
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
							// Snapshot the list (GetSpawnedIfPossible touches no shared
							// state here, but match the file's iterate-a-copy convention)
							// and guard each handle with .Exists(): a despawned vehicle's
							// handle is non-null but throws on .AttachedBlip.
							foreach (VehicleData vd in SpawnedVehicles.ToList()) {
								(Vehicle v, _) = GetSpawnedIfPossible(vd);
								if (v != null && v.Exists() && v.AttachedBlip != null) {
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
			VehicleData vd = XmlVehicleStorage.GetVehicle(VehicleUtilities.GetHashString(item.Title));
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
						// Refresh the list so the unsaved vehicle's row disappears
						// immediately instead of lingering until the menu is reopened.
						RebuildVehicleMenu();
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

		// A binding of Keys.None means "disabled" - never act on it. Set a key to
		// None in the INI (e.g. SaveKey=None) to free it up. A blank/unparseable
		// value does NOT disable; it reverts to the default for that key.
		static bool IsDisabled(Keys key) => key == Keys.None;

		public void OnKeyDown(object sender, KeyEventArgs e) {
			if (IsDisabled(MenuKey) || e.KeyCode != MenuKey) {
				return;
			}

			// Same key toggles the menu. Closing must also dismiss the submenu, or
			// pressing the key while it's open would leave the submenu on screen.
			if (MainMenu.Visible || VehicleMenu.Visible) {
				MainMenu.Visible = false;
				VehicleMenu.Visible = false;
				return;
			}

			RebuildVehicleMenu();
			MainMenu.Visible = true;
		}

		// Rebuild the saved-vehicle submenu from the store. This is the single source
		// of truth for the list: call it whenever the saved set changes (save/unsave,
		// from any path) so the menu never shows a stale entry or misses a new one.
		void RebuildVehicleMenu() {
			VehicleMenu.Clear();
			foreach (VehicleData vd in XmlVehicleStorage.GetVehicles()) {
				NativeListItem<string> vehicleItem = new NativeListItem<string>($"{vd.VehicleName} {vd.LicensePlate.Trim()}",
					"Spawn", "Spawn Nearby", "Despawn", "Save", "Unsave", "Set Spawn Location");
				VehicleMenu.Add(vehicleItem);
			}
		}

		public void OnKeyUp(object sender, KeyEventArgs e) {
			if (!IsDisabled(SaveKey) && e.KeyCode == SaveKey) {
				SaveCurrentVehicle();
			} else if (!IsDisabled(UnsaveKey) && e.KeyCode == UnsaveKey) {
				UnsaveCurrentVehicle();
			}
		}

		void MainMenuInit() {
			MainMenu = new NativeMenu("Vehicle Keeper", "Version 4.2.0");

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
