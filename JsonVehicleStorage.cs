using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace VehicleKeeper {
    public static class JsonVehicleStorage {
        private static string BasePath = string.Empty;
        private static string FileName = "preserved-vehicles.json";
        private static List<VehicleData> Cache = new List<VehicleData>();

        public static void Initialize(string path) {
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }

            BasePath = path;

            // Init cache
            GetVehicles();
        }

        public static void SaveVehicle(VehicleData vd) {
            ThrowExceptionWhenBasePathDoesNotExist();
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
            Cache.Remove(vd);
            string json = JsonConvert.SerializeObject(Cache, new JsonSerializerSettings {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            File.WriteAllText(GetFilePath(), json);
        }

        public static void UpdateVehicle(VehicleData vd) {
            ThrowExceptionWhenBasePathDoesNotExist();
            Cache.Remove(vd);
            Cache.Add(vd);

            string json = JsonConvert.SerializeObject(Cache, new JsonSerializerSettings {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            File.WriteAllText(GetFilePath(), json);
        }

        public static VehicleData GetVehicle(string ID) {
            if (Cache.Count == 0) {
                try {
                    string json = File.ReadAllText(GetFilePath());
                    Cache = JsonConvert.DeserializeObject<List<VehicleData>>(json, new JsonSerializerSettings {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    });
                } catch (Exception e) {
                    Logger.LogError(e.ToString());
                    return null;
                }
            }
            return Cache.Find(x => x.ID == ID);
        }

        public static List<VehicleData> GetVehicles() {
            if (Cache.Count == 0) {
                try {
                    string json = File.ReadAllText(GetFilePath());
                    Cache = JsonConvert.DeserializeObject<List<VehicleData>>(json, new JsonSerializerSettings {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    });
                } catch (Exception e) {
                    Logger.LogError(e.ToString());
                    return new List<VehicleData>();
                }
            }
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