using System.Collections.Generic;
using UnityEngine;

namespace VISimulation
{
    public class ContrastControllerS2 : MonoBehaviour
    {
        [Header("Scene 2 Lighting Control")]
        [Range(0, 1000)] public float emLux = 300f; // Global Illuminance target
        
        [Header("Target Lights (Optional)")]
        public Transform lightParent; // Parent object containing lights
        public List<Light> targetLights = new List<Light>();

        [Header("Object 1: Floor (地板)")]
        [Range(0, 100)] public float lrvFloor = 45f;
        public Transform floorParent;
        public List<Renderer> floorRenderers = new List<Renderer>();

        [Header("Object 2: Border Floor (邊界_靠地)")]
        [Range(0, 100)] public float lrvBorderFloor = 10f;
        public Transform borderFloorParent;
        public List<Renderer> borderFloorRenderers = new List<Renderer>();

        [Header("Object 3: Border Wall (邊界_靠牆)")]
        [Range(0, 100)] public float lrvBorderWall = 10f;
        public Transform borderWallParent;
        public List<Renderer> borderWallRenderers = new List<Renderer>();

        [Header("Object 4: Wall (牆面)")]
        [Range(0, 100)] public float lrvWall = 80f;
        public Transform wallParent;
        public List<Renderer> wallRenderers = new List<Renderer>();

        [Header("Object 5: Bench (板凳)")]
        [Range(0, 100)] public float lrvBench = 30f;
        public Transform benchParent;
        public List<Renderer> benchRenderers = new List<Renderer>();
        
        [Header("Calculated Contrast (Theory)")]
        [Tooltip("Floor vs Border Floor")]
        public string contrastFloorBorder;
        [Tooltip("Floor vs Wall")]
        public string contrastFloorWall;
        [Tooltip("Floor vs Bench")]
        public string contrastFloorBench;
        // You can add more pairs if needed (e.g. Wall vs BorderWall)
        
        // Materials Property Block for efficient color updates
        private MaterialPropertyBlock propBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private const float MIN_DENOMINATOR = 0.01f;

        private void Start()
        {
            propBlock = new MaterialPropertyBlock();
            
            // Auto-populate if empty
            if (floorRenderers.Count == 0 && floorParent) PopulateRenderers(floorParent, ref floorRenderers);
            if (borderFloorRenderers.Count == 0 && borderFloorParent) PopulateRenderers(borderFloorParent, ref borderFloorRenderers);
            if (borderWallRenderers.Count == 0 && borderWallParent) PopulateRenderers(borderWallParent, ref borderWallRenderers);
            if (wallRenderers.Count == 0 && wallParent) PopulateRenderers(wallParent, ref wallRenderers);
            if (benchRenderers.Count == 0 && benchParent) PopulateRenderers(benchParent, ref benchRenderers);
            if (targetLights.Count == 0 && lightParent) PopulateLights(lightParent, ref targetLights);

            UpdateAll();
        }

        private void Update()
        {
            // For editor testing
            if (Application.isEditor)
            {
                UpdateAll();
            }
        }

        public void UpdateAll()
        {
            UpdateMaterials();
            UpdateLighting();
            CalculateAllContrasts();
        }

        private void UpdateMaterials()
        {
            SetMaterialColor(floorRenderers, lrvFloor);
            SetMaterialColor(borderFloorRenderers, lrvBorderFloor);
            SetMaterialColor(borderWallRenderers, lrvBorderWall);
            SetMaterialColor(wallRenderers, lrvWall);
            SetMaterialColor(benchRenderers, lrvBench);
        }

        private void UpdateLighting()
        {
            // Simple logic: Lux affects distinct light intensity directly or via a factor
            // Assuming 1 Unity Light Intensity ~= 300 Lux approx factor, or just direct control
            // Here we treat emLux as a direct scalar for Point/Spot lights intensity for simplicity in this simulation 
            // or use a predefined factor. Let's assume EmLux maps to intensity with a factor.
            // Adjust this factor based on your scene calibration.
            float intensityFactor = 0.005f; // Example: 300 lux * 0.005 = 1.5 intensity
            
            float finalIntensity = emLux * intensityFactor;

            foreach (var light in targetLights)
            {
                if (light != null) light.intensity = finalIntensity;
            }
        }

        private void SetMaterialColor(List<Renderer> renderers, float lrv)
        {
            if (renderers == null) return;

            // Convert LRV to Linear RGB
            // Y = LRV/100. In grayscale, Linear RGB = Y.
            float linearVal = Mathf.Clamp01(lrv / 100f);
            Color color = new Color(linearVal, linearVal, linearVal, 1f);

            foreach (var r in renderers)
            {
                if (r == null) continue;
                
                // Try Property Block first
                r.GetPropertyBlock(propBlock);
                propBlock.SetColor(BaseColorId, color);
                propBlock.SetColor(ColorId, color); // Fallback for standard shader
                r.SetPropertyBlock(propBlock);
            }
        }

        private void CalculateAllContrasts()
        {
            contrastFloorBorder = CalculateLRVContrast(lrvFloor, lrvBorderFloor);
            contrastFloorWall = CalculateLRVContrast(lrvFloor, lrvWall);
            contrastFloorBench = CalculateLRVContrast(lrvFloor, lrvBench);
        }

        private string CalculateLRVContrast(float LRV1, float LRV2)
        {
            float B_bright = Mathf.Max(LRV1, LRV2);
            float B_dark = Mathf.Min(LRV1, LRV2);
            float denom = Mathf.Max(B_bright, MIN_DENOMINATOR);
            float c = (B_bright - B_dark) / denom * 100f;
            return $"{c:F1}%";
        }

        // --- Context Menu Helpers ---
        
        [ContextMenu("Refresh Targets")]
        public void RefreshTargets()
        {
             PopulateRenderers(floorParent, ref floorRenderers, true);
             PopulateRenderers(borderFloorParent, ref borderFloorRenderers, true);
             PopulateRenderers(borderWallParent, ref borderWallRenderers, true);
             PopulateRenderers(wallParent, ref wallRenderers, true);
             PopulateRenderers(benchParent, ref benchRenderers, true);
             PopulateLights(lightParent, ref targetLights, true);
        }

        private void PopulateRenderers(Transform parent, ref List<Renderer> list, bool forceClear = false)
        {
            if (forceClear || list == null) list = new List<Renderer>();
            if (parent == null) return;
            
            var rends = parent.GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
            {
                if (!list.Contains(r)) list.Add(r);
            }
        }
        
        private void PopulateLights(Transform parent, ref List<Light> list, bool forceClear = false)
        {
             if (forceClear || list == null) list = new List<Light>();
             if (parent == null) return;
             
             var lights = parent.GetComponentsInChildren<Light>(true);
             foreach(var l in lights)
             {
                 if (!list.Contains(l)) list.Add(l);
             }
        }
    }
}
