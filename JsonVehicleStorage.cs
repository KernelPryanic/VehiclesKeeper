using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace VehicleKeeper {
    public static class JsonVehicleStorage {
        private static string BasePath = string.Empty;
        private static readonly string FileName = "preserved-vehicles.json";
        private static List<VehicleData> Cache = new List<VehicleData>();
        // Tracks whether the cache has been hydrated from disk. We can't key off
        // Cache.Count, because a genuinely-empty store (zero saved vehicles, the
        // first-run case) would otherwise re-read the file on every call and throw
        // FileNotFoundException each time.
        private static bool Loaded;

        public static void Initialize(string path) {
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }

            BasePath = path;

            // Reset load state so a re-initialization (e.g. script reload) re-reads
            // from the new path instead of serving a stale cache.
            Loaded = false;
            GetVehicles();
        }

        private static void EnsureLoaded() {
            if (Loaded) {
                return;
            }

            string filePath = GetFilePath();
            if (File.Exists(filePath)) {
                try {
                    string json = File.ReadAllText(filePath);
                    Cache = JsonConvert.DeserializeObject<List<VehicleData>>(json, new JsonSerializerSettings {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    }) ?? new List<VehicleData>();
                } catch (Exception e) {
                    // Corrupt or unreadable file: log and start from empty rather
                    // than re-reading (and re-throwing) on every subsequent call.
                    Logger.LogError(e.ToString());
                    Cache = new List<VehicleData>();
                }
            } else {
                // First run: no file yet. An empty store is the valid initial state.
                Cache = new List<VehicleData>();
            }

            Loaded = true;
        }

        public static void SaveVehicle(VehicleData vd) {
            ThrowExceptionWhenBasePathDoesNotExist();
            EnsureLoaded();
            if (!Cache.Contains(vd)) {
                Cache.Add(vd);
            }

            string json = JsonConvert.SerializeObject(Cache, new JsonSerializerSettings {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            File.WriteAllText(GetFilePath(), json);
        }

        public static void RemoveVehicle(VehicleData vd) {
            ThrowExceptionWhenBasePathDoesNotExist();
            EnsureLoaded();
            Cache.Remove(vd);
            string json = JsonConvert.SerializeObject(Cache, new JsonSerializerSettings {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            File.WriteAllText(GetFilePath(), json);
        }

        public static void UpdateVehicle(VehicleData vd) {
            ThrowExceptionWhenBasePathDoesNotExist();
            EnsureLoaded();
            Cache.Remove(vd);
            Cache.Add(vd);

            string json = JsonConvert.SerializeObject(Cache, new JsonSerializerSettings {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            File.WriteAllText(GetFilePath(), json);
        }

        public static VehicleData GetVehicle(string ID) {
            EnsureLoaded();
            return Cache.Find(x => x.ID == ID);
        }

        public static List<VehicleData> GetVehicles() {
            EnsureLoaded();
            return Cache;
        }

        private static string GetFilePath() {
            return $"{BasePath.TrimEnd('/')}/{FileName}";
        }

        public static void ThrowExceptionWhenBasePathDoesNotExist() {
            if (BasePath == string.Empty) {
                throw new BasePathNotInitialized();
            }
        }

        public class BasePathNotInitialized : Exception { }
    }
}