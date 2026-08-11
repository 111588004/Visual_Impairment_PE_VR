using System.Collections.Generic;
using UnityEngine;

namespace VISimulation
{
    public class ContrastControllerS2 : MonoBehaviour, ISimulationController
    {
        [Header("Scene 2 Lighting Control")]
        [Range(0, 1000)] public float emLux = 300f; // Global Illuminance target
        public float intensityFactor = 0.03f; // Factor to convert Lux to Unity Intensity
        
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
        public string contrastFloorBorder;
        public string contrastFloorWall;
        public string contrastFloorBench;
        
        private bool skirtingEnabled = true;

        // Materials Property Block for efficient color updates
        private MaterialPropertyBlock propBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private const float MIN_DENOMINATOR = 0.01f;
        
        private void Awake()
        {
            if (Application.isPlaying)
            {
                ApplyExistingVariant();
            }
        }

        private void Start()
        {
            propBlock = new MaterialPropertyBlock();
            
            if (floorRenderers.Count == 0 && floorParent) PopulateRenderers(floorParent, ref floorRenderers);
            if (borderFloorRenderers.Count == 0 && borderFloorParent) PopulateRenderers(borderFloorParent, ref borderFloorRenderers);
            if (borderWallRenderers.Count == 0 && borderWallParent) PopulateRenderers(borderWallParent, ref borderWallRenderers);
            if (wallRenderers.Count == 0 && wallParent) PopulateRenderers(wallParent, ref wallRenderers);
            if (benchRenderers.Count == 0 && benchParent) PopulateRenderers(benchParent, ref benchRenderers);
            if (targetLights.Count == 0 && lightParent) PopulateLights(lightParent, ref targetLights);

            ApplyExistingVariant();
            UpdateAll();
        }

        private void ApplyExistingVariant()
        {
            if (SimulationVariantManager.Instance != null && SimulationVariantManager.Instance.ActiveVariant != null)
            {
                var variant = SimulationVariantManager.Instance.ActiveVariant;
                if (variant.sceneName == UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)
                {
                    ApplyVariant(variant);
                }
            }
        }

        public void ApplyVariant(SimulationVariantData variant)
        {
            if (variant == null) return;
            Debug.Log($"[ContrastControllerS2] Applying Variant: {variant.variantName}");

            var fl = variant.GetParameter("FloorLRV");
            if (fl != null) lrvFloor = fl.value;

            var bf = variant.GetParameter("BorderFloorLRV");
            if (bf != null) lrvBorderFloor = bf.value;

            var bw = variant.GetParameter("BorderWallLRV");
            if (bw != null) 
            {
                lrvBorderWall = bw.value;
                skirtingEnabled = bw.isEnabled;
            }

            var wl = variant.GetParameter("WallLRV");
            if (wl != null) lrvWall = wl.value;

            var bn = variant.GetParameter("BenchLRV");
            if (bn != null) lrvBench = bn.value;

            var lx = variant.GetParameter("EmLux");
            if (lx != null) emLux = lx.value;

            UpdateAll();

            var menu = FindFirstObjectByType<VRContrastMenuS2>();
            if (menu != null) menu.InitializeUI();
        }

        private void Update()
        {
            UpdateAll();
        }

        private void OnValidate()
        {
            UpdateAll();
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
            
            // Special Case for Skirting (Border Wall): Hide if explicitly disabled in variant
            if (borderWallParent != null) borderWallParent.gameObject.SetActive(skirtingEnabled);
            if (skirtingEnabled) SetMaterialColor(borderWallRenderers, lrvBorderWall);

            SetMaterialColor(wallRenderers, lrvWall);
            SetMaterialColor(benchRenderers, lrvBench);
        }

        private void UpdateLighting()
        {
            float finalIntensity = emLux * intensityFactor;
            foreach (var light in targetLights)
            {
                if (light != null) light.intensity = finalIntensity;
            }
        }

        private void SetMaterialColor(List<Renderer> renderers, float lrv)
        {
            if (renderers == null) return;
            if (propBlock == null) propBlock = new MaterialPropertyBlock();

            float linearVal = Mathf.Clamp01(lrv / 100f);
            Color color = new Color(linearVal, linearVal, linearVal, 1f);

            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(propBlock);
                propBlock.SetColor(BaseColorId, color);
                propBlock.SetColor(ColorId, color); 
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
            foreach (var r in rends) if (!list.Contains(r)) list.Add(r);
        }
        
        private void PopulateLights(Transform parent, ref List<Light> list, bool forceClear = false)
        {
             if (forceClear || list == null) list = new List<Light>();
             if (parent == null) return;
             var lights = parent.GetComponentsInChildren<Light>(true);
             foreach(var l in lights) if (!list.Contains(l)) list.Add(l);
        }
    }
}
