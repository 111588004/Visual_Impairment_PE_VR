using UnityEngine;
using System.Collections.Generic;

namespace VISimulation
{
    [ExecuteAlways]
    public class ContrastController : MonoBehaviour, ISimulationController
    {
        [Header("Target Object Groups (Drag Parent for Auto-Fill)")]
        public GameObject nosingParent;
        public GameObject treadParent;
        public GameObject wallParent;

        [Header("Target Renderers List")]
        public List<Renderer> nosingRenderers = new List<Renderer>();
        public List<Renderer> treadRenderers = new List<Renderer>();
        public List<Renderer> wallRenderers = new List<Renderer>();

        [Header("Lighting Group (Drag Parent for Auto-Fill)")]
        public GameObject lightParent;
        public List<Light> targetLights = new List<Light>();
        
        [Tooltip("Environment Illuminance in Lux (Em)")]
        [Min(0)]
        public float emLux = 300f;
        [Tooltip("Conversion Factor: Lux to Point Light Intensity. Adjust this until the scene brightness looks correct (~0.03).")]
        public float intensityFactor = 0.03f;

        [Header("LRV Settings (0-100 CIE Y)")]
        [Range(0, 100)] public float lrvNosing = 50f;
        [Range(0, 100)] public float lrvTread = 30f;
        [Range(0, 100)] public float lrvWall = 70f;
        
        [Header("Advanced")]
        [Tooltip("Uncheck if Wall colors don't update (Forces material instance modification).")]
        public bool usePropertyBlock = true;

        [Space]
        [Header("Calculated Contrast (Theory)")]
        [SerializeField] private string contrastNosingTread; 
        [SerializeField] private string contrastWallTread;

        private bool nosingEnabled = true;

        // Scientific Compromise: Black Floor for VR
        private const float MIN_DENOMINATOR = 0.01f;
        
        // Use OnValidate to auto-populate lists when a parent is assigned in Editor
        private void OnValidate()
        {
             // Only auto-populate if empty to avoid overwriting manual changes
             PopulateRenderers(nosingParent, ref nosingRenderers, false);
             PopulateRenderers(treadParent, ref treadRenderers, false);
             PopulateRenderers(wallParent, ref wallRenderers, false);
             
             if (targetLights.Count == 0) PopulateLights(lightParent, ref targetLights, false);
        }

        [ContextMenu("Refresh Targets (Force Update)")]
        public void RefreshTargets()
        {
             PopulateRenderers(nosingParent, ref nosingRenderers, true);
             PopulateRenderers(treadParent, ref treadRenderers, true);
             PopulateRenderers(wallParent, ref wallRenderers, true);
             
             PopulateLights(lightParent, ref targetLights, true);
             
             Debug.Log($"Refreshed Targets: Nosing={nosingRenderers.Count}, Tread={treadRenderers.Count}, Wall={wallRenderers.Count}, Lights={targetLights.Count}");
        }

        private void PopulateRenderers(GameObject parent, ref List<Renderer> list, bool force)
        {
            if (parent != null)
            {
                if (force || list == null || list.Count == 0)
                {
                    list = new List<Renderer>(parent.GetComponentsInChildren<Renderer>());
                }
            }
        }

        private void PopulateLights(GameObject parent, ref List<Light> list, bool force)
        {
            if (parent != null)
            {
                if (force || list == null || list.Count == 0)
                {
                    list = new List<Light>(parent.GetComponentsInChildren<Light>());
                }
            }
        }

        private void Awake()
        {
            if (Application.isPlaying)
            {
                ApplyExistingVariant();
            }
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                ApplyExistingVariant();
            }
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
            Debug.Log($"[ContrastController] Applying Variant: {variant.variantName}");

            var nx = variant.GetParameter("NosingLRV");
            if (nx != null) 
            {
                lrvNosing = nx.value;
                nosingEnabled = nx.isEnabled;
            }

            var tr = variant.GetParameter("TreadLRV");
            if (tr != null) lrvTread = tr.value;

            var wl = variant.GetParameter("WallLRV");
            if (wl != null) lrvWall = wl.value;

            var lx = variant.GetParameter("EmLux");
            if (lx != null) emLux = lx.value;

            UpdateMaterials();
            UpdateLighting();
            CalculateContrast();

            // Refresh VR Menu if it exists
            var menu = FindFirstObjectByType<VRContrastMenu>();
            if (menu != null) menu.InitializeUI();
        }

        public void UpdateAll()
        {
            UpdateMaterials();
            UpdateLighting();
            CalculateContrast();
        }

        private void Update()
        {
            UpdateMaterials();
            UpdateLighting();
            CalculateContrast();
        }

        private void UpdateMaterials()
        {
            // Special Case for Nosing: Hide if isEnabled is false
            if (nosingParent != null) nosingParent.SetActive(nosingEnabled);
            if (nosingEnabled) SetMaterialColor(nosingRenderers, lrvNosing);

            SetMaterialColor(treadRenderers, lrvTread);
            SetMaterialColor(wallRenderers, lrvWall);
        }

        private void SetMaterialColor(List<Renderer> renderers, float lrv)
        {
            if (renderers == null) return;
            
            // Standard Formula: Y = LRV/100
            float linearVal = lrv / 100f;
            Color color = new Color(linearVal, linearVal, linearVal, 1f);
            
            if (usePropertyBlock)
            {
                MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                foreach (var r in renderers)
                {
                    if (r == null) continue;
                    r.GetPropertyBlock(mpb);
                    mpb.SetColor("_BaseColor", color);
                    mpb.SetColor("_Color", color);
                    r.SetPropertyBlock(mpb);
                }
            }
            else
            {
                // Fallback: Modify Material Instance directly
                foreach (var r in renderers)
                {
                    if (r == null) continue;
                    if (Application.isPlaying)
                    {
                        r.material.color = color;
                        if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", color);
                    }
                }
            }
        }

        private void UpdateLighting()
        {
            if (targetLights != null)
            {
                float intensity = emLux * intensityFactor;
                foreach (var light in targetLights)
                {
                    if (light != null) light.intensity = intensity;
                }
            }
        }

        private void CalculateContrast()
        {
            contrastNosingTread = CalculateLRVContrast(lrvNosing, lrvTread);
            contrastWallTread = CalculateLRVContrast(lrvWall, lrvTread);
        }

        private string CalculateLRVContrast(float LRV1, float LRV2)
        {
            float B_bright = Mathf.Max(LRV1, LRV2);
            float B_dark = Mathf.Min(LRV1, LRV2);
            float denomLRV = Mathf.Max(B_bright, MIN_DENOMINATOR);
            float c_lrv = (B_bright - B_dark) / denomLRV * 100f;
            return $"{c_lrv:F1}%";
        }
    }
}
