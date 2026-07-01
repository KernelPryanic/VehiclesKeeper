using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace VehicleKeeper {
	// Persists saved vehicles as one XML file PER vehicle, under a Vehicles\
	// subfolder of the data dir (Vehicles\<ID>.xml). The per-tick update while
	// driving rewrites only the one car's small file instead of re-serializing the
	// whole collection, so write cost stays flat no matter how many are saved.
	// XmlSerializer is a framework built-in, so the mod ships as a single DLL with
	// no third-party JSON dependency.
	//
	// The ID is a SHA-256 hex string (see VehicleUtilities.GetHashString), so it is
	// already a safe, unique filename — no sanitization needed.
	//
	// Persistence tolerates first run and corruption: a missing folder is the valid
	// empty state, a corrupt per-vehicle file is logged and skipped (the rest still
	// load). Reads never throw. An explicit Loaded flag tracks load-state so a
	// genuinely-empty store doesn't re-scan the disk on every call.
	public static class XmlVehicleStorage {
		private static string BasePath = string.Empty;
		private const string VehiclesFolder = "Vehicles";
		// The pre-4.x single-file store; imported once into per-vehicle files.
		private const string LegacyFileName = "preserved-vehicles.xml";
		private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(VehicleData));
		private static readonly XmlSerializer LegacySerializer = new XmlSerializer(typeof(List<VehicleData>));
		// Keyed by VehicleData.ID. A dictionary keeps GetVehicle(ID) O(1) and makes
		// "rewrite just this one" the natural operation.
		private static Dictionary<string, VehicleData> Cache = new Dictionary<string, VehicleData>();
		private static bool Loaded;

		public static void Initialize(string path) {
			if (!Directory.Exists(path)) {
				Directory.CreateDirectory(path);
			}

			BasePath = path;

			// Reset load state so a re-initialization (e.g. script reload) re-reads
			// from disk instead of serving a stale cache.
			Loaded = false;
			GetVehicles();
		}

		private static void EnsureLoaded() {
			if (Loaded) {
				return;
			}

			Cache = new Dictionary<string, VehicleData>();
			MigrateLegacyFile();

			string dir = VehiclesDir();
			if (Directory.Exists(dir)) {
				foreach (string file in Directory.GetFiles(dir, "*.xml")) {
					VehicleData vd = ReadFile(file);
					if (vd != null && !string.IsNullOrEmpty(vd.ID)) {
						Cache[vd.ID] = vd;
					}
				}
			}

			Loaded = true;
			Logger.Log($"Storage loaded {Cache.Count} vehicle(s) from {dir}.");
		}

		public static void SaveVehicle(VehicleData vd) {
			ThrowExceptionWhenBasePathDoesNotExist();
			EnsureLoaded();
			Cache[vd.ID] = vd;
			WriteFile(vd);
		}

		public static void RemoveVehicle(VehicleData vd) {
			ThrowExceptionWhenBasePathDoesNotExist();
			EnsureLoaded();
			Cache.Remove(vd.ID);
			string file = FilePathFor(vd.ID);
			if (File.Exists(file)) {
				File.Delete(file);
			}
		}

		public static void UpdateVehicle(VehicleData vd) {
			// Same as save: write only this vehicle's file, not the whole set.
			SaveVehicle(vd);
		}

		public static VehicleData GetVehicle(string ID) {
			EnsureLoaded();
			return Cache.TryGetValue(ID, out VehicleData vd) ? vd : null;
		}

		public static List<VehicleData> GetVehicles() {
			EnsureLoaded();
			return Cache.Values.ToList();
		}

		// Read one per-vehicle file, returning null on a missing/corrupt file so one
		// bad entry never aborts loading the rest.
		private static VehicleData ReadFile(string file) {
			try {
				using (var reader = new StreamReader(file)) {
					return (VehicleData)Serializer.Deserialize(reader);
				}
			} catch (Exception e) {
				Logger.LogError($"Failed to read {file}: {e}");
				return null;
			}
		}

		private static void WriteFile(VehicleData vd) {
			string dir = VehiclesDir();
			if (!Directory.Exists(dir)) {
				Directory.CreateDirectory(dir);
			}
			using (var writer = new StreamWriter(FilePathFor(vd.ID))) {
				Serializer.Serialize(writer, vd);
			}
		}

		// One-time import of the pre-4.x single-file store into per-vehicle files,
		// then rename it to .migrated so it is not re-imported on the next launch.
		// Best-effort: any failure is logged and skipped (a fresh empty store is the
		// fallback), never thrown.
		private static void MigrateLegacyFile() {
			string legacy = Path.Combine(BasePath, LegacyFileName);
			if (!File.Exists(legacy)) {
				return;
			}

			try {
				List<VehicleData> vehicles;
				using (var reader = new StreamReader(legacy)) {
					vehicles = (List<VehicleData>)LegacySerializer.Deserialize(reader)
						?? new List<VehicleData>();
				}
				foreach (VehicleData vd in vehicles) {
					if (vd != null && !string.IsNullOrEmpty(vd.ID)) {
						WriteFile(vd);
					}
				}
				File.Move(legacy, legacy + ".migrated");
				Logger.Log($"Migrated {vehicles.Count} vehicle(s) from {LegacyFileName} to {VehiclesFolder}\\");
			} catch (Exception e) {
				Logger.LogError($"Failed to migrate {LegacyFileName}: {e}");
			}
		}

		private static string VehiclesDir() {
			return Path.Combine(BasePath, VehiclesFolder);
		}

		private static string FilePathFor(string id) {
			return Path.Combine(VehiclesDir(), id + ".xml");
		}

		public static void ThrowExceptionWhenBasePathDoesNotExist() {
			if (BasePath == string.Empty) {
				throw new BasePathNotInitialized();
			}
		}

		public class BasePathNotInitialized : Exception { }
	}
}
