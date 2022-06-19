using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace VehicleKeeper {
    public static class JsonVehicleStorage {
        private static string BasePath = string.Empty;
        private static string FileName = "saved-vehicles.json";

        public static void InitializeBasePath(string path) {
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }

            BasePath = path;
        }

        public static void SaveVehicle(VehicleData vd) {
            ThrowExceptionWhenBasePathDoesNotExist();
            List<VehicleData> vehicles = GetVehicles().ToList();
            vehicles.Add(vd);

            string json = JsonConvert.SerializeObject(vehicles, new JsonSerializerSettings {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            File.WriteAllText(GetFilePath(), json);
        }

        public static void RemoveVehicle(VehicleData vd) {
            ThrowExceptionWhenBasePathDoesNotExist();
            List<VehicleData> vehicles = GetVehicles().ToList();
            vehicles.Remove(vd);
            ConvertAndWriteVehiclesToFile(vehicles);
        }

        public static void ConvertAndWriteVehiclesToFile(IEnumerable<VehicleData> vehicles) {
            string json = JsonConvert.SerializeObject(vehicles, new JsonSerializerSettings {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            File.WriteAllText(GetFilePath(), json);
        }

        public static void ThrowExceptionWhenBasePathDoesNotExist() {
            if (BasePath == string.Empty) {
                throw new BasePathNotInitialized();
            }
        }

        public static VehicleData GetVehicle(string ID) {
            try {
                string json = File.ReadAllText(GetFilePath());

                VehicleData[] allVehicles = JsonConvert.DeserializeObject<VehicleData[]>(json, new JsonSerializerSettings {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
                return Array.Find(allVehicles, x => x.ID == ID);
            } catch {
                return null;
            }
        }

        public static VehicleData[] GetVehicles() {
            try {
                string json = File.ReadAllText(GetFilePath());

                return JsonConvert.DeserializeObject<VehicleData[]>(json, new JsonSerializerSettings {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
            } catch {
                return new VehicleData[0];
            }
        }

        private static string GetFilePath() {
            return $"{BasePath.TrimEnd('/')}/{FileName}";
        }

        public class BasePathNotInitialized : Exception { }
    }
}