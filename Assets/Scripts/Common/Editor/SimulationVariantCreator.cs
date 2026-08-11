#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace VISimulation
{
    public class SimulationVariantCreator : Editor
    {
        [MenuItem("VI Simulation/Create ALL Sample Variants")]
        public static void CreateAllVariants()
        {
            CreateS1Variants();
            CreateS2Variants();
            CreateS3Variants();
        }

        [MenuItem("VI Simulation/Create S1 Variants")]
        public static void CreateS1Variants()
        {
            string folderPath = "Assets/Settings/Variants/S1";
            EnsureFolder(folderPath);
            string scene = "S1_Stairway";

            CreateVariant("S1_Task1_NosingContrast", scene, new List<SimulationVariantData.ParameterSetting>
            {
                new SimulationVariantData.ParameterSetting { parameterId = "NosingLRV", value = 50f, isAdjustable = true, isEnabled = true },
                new SimulationVariantData.ParameterSetting { parameterId = "WallLRV", value = 50f, isAdjustable = false, isEnabled = true },
                new SimulationVariantData.ParameterSetting { parameterId = "TreadLRV", value = 30f, isAdjustable = false, isEnabled = true },
                new SimulationVariantData.ParameterSetting { parameterId = "EmLux", value = 150f, isAdjustable = false, isEnabled = true }
            }, "S1");

            // Task 2: Wall Contrast
            CreateVariant("S1_Task2_WallContrast", scene, new List<SimulationVariantData.ParameterSetting>
            {
                new SimulationVariantData.ParameterSetting { parameterId = "NosingLRV", value = 50f, isAdjustable = false, isEnabled = false }, // Disable Nosing
                new SimulationVariantData.ParameterSetting { parameterId = "WallLRV", value = 50f, isAdjustable = true, isEnabled = true },
                new SimulationVariantData.ParameterSetting { parameterId = "TreadLRV", value = 30f, isAdjustable = false, isEnabled = true },
                new SimulationVariantData.ParameterSetting { parameterId = "EmLux", value = 150f, isAdjustable = false, isEnabled = true }
            }, "S1");

            // Task 3: Illuminance
            CreateVariant("S1_Task3_Illuminance", scene, new List<SimulationVariantData.ParameterSetting>
            {
                new SimulationVariantData.ParameterSetting { parameterId = "NosingLRV", value = 50f, isAdjustable = false },
                new SimulationVariantData.ParameterSetting { parameterId = "WallLRV", value = 50f, isAdjustable = false },
                new SimulationVariantData.ParameterSetting { parameterId = "TreadLRV", value = 30f, isAdjustable = false },
                new SimulationVariantData.ParameterSetting { parameterId = "EmLux", value = 150f, isAdjustable = true }
            }, "S1");

            AssetDatabase.SaveAssets();
            Debug.Log("[VariantCreator] S1 Variants updated from Excel.");
        }

        [MenuItem("VI Simulation/Create S2 Variants")]
        public static void CreateS2Variants()
        {
            string folderPath = "Assets/Settings/Variants/S2";
            EnsureFolder(folderPath);
            string scene = "S2_Corridor";

            // Task 1: Bench Contrast
            CreateVariant("S2_Task1_BenchContrast", scene, new List<SimulationVariantData.ParameterSetting>
            {
                new SimulationVariantData.ParameterSetting { parameterId = "BenchLRV", value = 40f, isAdjustable = true, isEnabled = true },
                new SimulationVariantData.ParameterSetting { parameterId = "WallLRV", value = 40f, isAdjustable = false, isEnabled = true },
                new SimulationVariantData.ParameterSetting { parameterId = "BorderWallLRV", value = 40f, isAdjustable = false, isEnabled = false }, // Skirting Disable
                new SimulationVariantData.ParameterSetting { parameterId = "FloorLRV", value = 60f, isAdjustable = false, isEnabled = true },
                new SimulationVariantData.ParameterSetting { parameterId = "EmLux", value = 150f, isAdjustable = false, isEnabled = true }
            }, "S2");

            // Task 2: Wall Contrast
            CreateVariant("S2_Task2_WallContrast", scene, new List<SimulationVariantData.ParameterSetting>
            {
                new SimulationVariantData.ParameterSetting { parameterId = "BenchLRV", value = 40f, isAdjustable = false, isEnabled = true },
                new SimulationVariantData.ParameterSetting { parameterId = "WallLRV", value = 40f, isAdjustable = true, isEnabled = true },
                new SimulationVariantData.ParameterSetting { parameterId = "BorderWallLRV", value = 40f, isAdjustable = false, isEnabled = false },
                new SimulationVariantData.ParameterSetting { parameterId = "FloorLRV", value = 60f, isAdjustable = false, isEnabled = true },
                new SimulationVariantData.ParameterSetting { parameterId = "EmLux", value = 150f, isAdjustable = false, isEnabled = true }
            }, "S2");

            // Task 3: Skirting Contrast
            CreateVariant("S2_Task3_SkirtingContrast", scene, new List<SimulationVariantData.ParameterSetting>
            {
                new SimulationVariantData.ParameterSetting { parameterId = "BenchLRV", value = 40f, isAdjustable = false, isEnabled = true },
                new SimulationVariantData.ParameterSetting { parameterId = "WallLRV", value = 30f, isAdjustable = false, isEnabled = true },
                new SimulationVariantData.ParameterSetting { parameterId = "BorderWallLRV", value = 40f, isAdjustable = true, isEnabled = true },
                new SimulationVariantData.ParameterSetting { parameterId = "FloorLRV", value = 60f, isAdjustable = false, isEnabled = true },
                new SimulationVariantData.ParameterSetting { parameterId = "EmLux", value = 150f, isAdjustable = false, isEnabled = true }
            }, "S2");

            // Task 4: Illuminance
            CreateVariant("S2_Task4_Illuminance", scene, new List<SimulationVariantData.ParameterSetting>
            {
                new SimulationVariantData.ParameterSetting { parameterId = "BenchLRV", value = 40f, isAdjustable = false, isEnabled = true },
                new SimulationVariantData.ParameterSetting { parameterId = "WallLRV", value = 40f, isAdjustable = false, isEnabled = true },
                new SimulationVariantData.ParameterSetting { parameterId = "BorderWallLRV", value = 40f, isAdjustable = false, isEnabled = false },
                new SimulationVariantData.ParameterSetting { parameterId = "FloorLRV", value = 60f, isAdjustable = false, isEnabled = true },
                new SimulationVariantData.ParameterSetting { parameterId = "EmLux", value = 150f, isAdjustable = true, isEnabled = true }
            }, "S2");

            AssetDatabase.SaveAssets();
            Debug.Log("[VariantCreator] S2 Variants updated from Excel.");
        }

        [MenuItem("VI Simulation/Create S3 Variants")]
        public static void CreateS3Variants()
        {
            string folderPath = "Assets/Settings/Variants/S3";
            EnsureFolder(folderPath);
            string scene = "S3_InteriorSignage";

            // Task 1: Height
            CreateVariant("S3_Task1_Height", scene, new List<SimulationVariantData.ParameterSetting>
            {
                new SimulationVariantData.ParameterSetting { parameterId = "Height", value = 160f, isAdjustable = true },
                new SimulationVariantData.ParameterSetting { parameterId = "FontSize", value = 50f, isAdjustable = false },
                new SimulationVariantData.ParameterSetting { parameterId = "BoardLRV", value = 50f, isAdjustable = false },
                new SimulationVariantData.ParameterSetting { parameterId = "TextLRV", value = 20f, isAdjustable = false },
                new SimulationVariantData.ParameterSetting { parameterId = "WallLRV", value = 80f, isAdjustable = false }
            }, "S3");

            // Task 2: FontSize
            CreateVariant("S3_Task2_FontSize", scene, new List<SimulationVariantData.ParameterSetting>
            {
                new SimulationVariantData.ParameterSetting { parameterId = "Height", value = 150f, isAdjustable = false },
                new SimulationVariantData.ParameterSetting { parameterId = "FontSize", value = 50f, isAdjustable = true },
                new SimulationVariantData.ParameterSetting { parameterId = "BoardLRV", value = 90f, isAdjustable = false },
                new SimulationVariantData.ParameterSetting { parameterId = "TextLRV", value = 20f, isAdjustable = false },
                new SimulationVariantData.ParameterSetting { parameterId = "WallLRV", value = 80f, isAdjustable = false }
            }, "S3");

            // Task 3: Board Contrast
            CreateVariant("S3_Task3_BoardContrast", scene, new List<SimulationVariantData.ParameterSetting>
            {
                new SimulationVariantData.ParameterSetting { parameterId = "Height", value = 150f, isAdjustable = false, isEnabled = true },
                new SimulationVariantData.ParameterSetting { parameterId = "FontSize", value = 50f, isAdjustable = false, isEnabled = true },
                new SimulationVariantData.ParameterSetting { parameterId = "BoardLRV", value = 50f, isAdjustable = true, isEnabled = true },
                new SimulationVariantData.ParameterSetting { parameterId = "TextLRV", value = 20f, isAdjustable = false, isEnabled = false }, // Disable Text
                new SimulationVariantData.ParameterSetting { parameterId = "WallLRV", value = 80f, isAdjustable = false, isEnabled = true }
            }, "S3");

            // Task 4: Text Contrast
            CreateVariant("S3_Task4_TextContrast", scene, new List<SimulationVariantData.ParameterSetting>
            {
                new SimulationVariantData.ParameterSetting { parameterId = "Height", value = 150f, isAdjustable = false },
                new SimulationVariantData.ParameterSetting { parameterId = "FontSize", value = 50f, isAdjustable = false },
                new SimulationVariantData.ParameterSetting { parameterId = "BoardLRV", value = 20f, isAdjustable = false },
                new SimulationVariantData.ParameterSetting { parameterId = "TextLRV", value = 50f, isAdjustable = true },
                new SimulationVariantData.ParameterSetting { parameterId = "WallLRV", value = 80f, isAdjustable = false }
            }, "S3");

            AssetDatabase.SaveAssets();
            Debug.Log("[VariantCreator] S3 Variants updated from Excel.");
        }

        private static void EnsureFolder(string path)
        {
            string[] folders = path.Split('/');
            string current = folders[0];
            for (int i = 1; i < folders.Length; i++)
            {
                string next = current + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, folders[i]);
                }
                current = next;
            }
        }

        private static void CreateVariant(string name, string sceneName, List<SimulationVariantData.ParameterSetting> settings, string subFolder)
        {
            SimulationVariantData asset = ScriptableObject.CreateInstance<SimulationVariantData>();
            asset.variantName = name;
            asset.sceneName = sceneName;
            asset.parameters = settings;

            string path = $"Assets/Settings/Variants/{subFolder}/{name}.asset";
            AssetDatabase.CreateAsset(asset, path);
        }
    }
}
#endif
