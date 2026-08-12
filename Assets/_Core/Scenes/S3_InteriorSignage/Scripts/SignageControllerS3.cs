using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

namespace VISimulation
{
    [ExecuteInEditMode]
    public class SignageControllerS3 : MonoBehaviour, ISimulationController
    {
        [Header("告示牌設置 (Signage Settings)")]

        [Header("1. 垂直高度 (Vertical Height)")]
        [Tooltip("告示牌中心點離地高度 (單位: 公分)")]
        [Range(0f, 350f)] 
        public float heightCm = 150f;
        
        [Header("校正設定 (Calibration)")]
        [Tooltip("高度偏移量 (當 Height=0 時的實際 Y 軸高度，單位: cm)")]
        public float heightOffsetCm = 14f; 

        [Header("2. 字體高度 (Character Height)")]
        [Tooltip("觸摸標示文字高度 (單位: mm)")]
        [Range(5f, 300f)] 
        public float charHeightMm = 50f; // Default 50mm

        [Header("縮放基準 (Scaling Reference)")]
        [Tooltip("原始物件在 Scale=1 時的字體高度 (預設 50mm)。")]
        public float referenceHeightMm = 50f;

        [Tooltip("物件的基礎縮放值 (預設 1,1,1)")]
        public Vector3 baseScale = Vector3.one;

        [Header("3. 獨立對比度與 LRV (Independent Contrast Setup)")]
        
        [Header("牆面 (Wall)")]
        [Range(0f, 100f)] public float wallLrv = 50f;
        [Tooltip("請將牆面物件拖進來 (可以是父物件)")]
        public GameObject[] wallObjects;

        [Header("背板 (Board)")]
        [Range(0f, 100f)] public float boardLrv = 80f;
        [Tooltip("請將背板物件拖進來 (可以是父物件)")]
        public GameObject[] boardObjects;

        [Header("文字 (Text)")]
        [Range(0f, 100f)] public float textLrv = 10f;
        [Tooltip("請將文字物件拖進來 (可以是父物件)")]
        public GameObject[] textObjects; 

        private bool wallEnabled = true;
        private bool boardEnabled = true;
        private bool textEnabled = true;

        // Internal
        private MaterialPropertyBlock propBlock;

        private void Awake()
        {
            if (Application.isPlaying)
            {
                ApplyExistingVariant();
            }
        }

        private void OnEnable()
        {
            if (propBlock == null) propBlock = new MaterialPropertyBlock();
            UpdateAll();
        }

        private void OnValidate()
        {
            if (propBlock == null) propBlock = new MaterialPropertyBlock();
            UpdateAll();
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                FixTunnelingVignette();
                FixGlaucomaCanvas();

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
            Debug.Log($"[SignageControllerS3] Applying Variant: {variant.variantName}");

            var h = variant.GetParameter("Height");
            if (h != null) heightCm = h.value;

            var fs = variant.GetParameter("FontSize");
            if (fs != null) charHeightMm = fs.value;

            var tx = variant.GetParameter("TextLRV");
            if (tx != null) 
            {
                textLrv = tx.value;
                textEnabled = tx.isEnabled;
            }

            var wl = variant.GetParameter("WallLRV");
            if (wl != null) 
            {
                wallLrv = wl.value;
                wallEnabled = wl.isEnabled;
            }

            var bd = variant.GetParameter("BoardLRV");
            if (bd != null) 
            {
                boardLrv = bd.value;
                boardEnabled = bd.isEnabled;
            }

            UpdateAll();

            // Refresh UI if exists
            var menu = FindFirstObjectByType<VRSignageMenuS3>();
            if (menu != null) menu.InitializeUI();
        }

        private void FixTunnelingVignette()
        {
             // [Fix 1] Prevent Tunneling Vignette (Prefab) from blocking UI interactions
            GameObject vignette = GameObject.Find("TunnelingVignette");
            if (vignette != null)
            {
                vignette.layer = 2; // Ignore Raycast
                Collider[] cols = vignette.GetComponentsInChildren<Collider>(true);
                foreach (var col in cols) col.enabled = false;
                Debug.Log($"[SignageControllerS3] Auto-fixed TunnelingVignette: Layer=IgnoreRaycast, Disabled {cols.Length} colliders.");
            }
        }

        private void FixGlaucomaCanvas()
        {
            // [Fix 2] Ensure the Glaucoma Screen Space Overlay doesn't block World Space UI
            // The user says "XR Rig internal Canvas Raw Image" blocks the "VR Menu Canvas".
            // If the Glaucoma Canvas is Screen Space - Overlay, it draws on top of everything.
            // We must convert it to Screen Space - Camera, providing it's on the Main Camera 
            // and has a sorting order LOWER than the VR Menu or UI Camera.
            
            // 1. Locate the Glaucoma Canvas within XR Rig
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            Canvas glaucomaCanvas = null;

            foreach(var c in allCanvases)
            {
                // Heuristic: It has a RawImage child and is part of XR Rig/Camera
                if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    RawImage raw = c.GetComponentInChildren<RawImage>();
                    if (raw != null && c.gameObject.name.Contains("Canvas")) // Default name usually
                    {
                        // Check if it's the one under Main Camera
                        if (c.transform.parent != null && c.transform.parent.GetComponent<Camera>() != null)
                        {
                            glaucomaCanvas = c;
                            break;
                        }
                    }
                }
            }

            if (glaucomaCanvas != null)
            {
                Debug.Log($"[SignageControllerS3] Found potential Glaucoma Canvas: {glaucomaCanvas.name}");
                
                // Force it to Screen Space - Camera so it respects depth/sorting
                glaucomaCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                glaucomaCanvas.worldCamera = Camera.main;
                // Set plane distance very close (e.g. 0.31) to be just in front of near clip, 
                // but behind World Space UI which is usually further away (e.g. 1m - 2m).
                // TunnelingVignette usually sets valid range.
                glaucomaCanvas.planeDistance = 0.35f; 
                glaucomaCanvas.sortingOrder = -100; // Draw first (behind other UI)
                
                // Keep GraphicRaycaster enabled as requested by user for mouse testing,
                // BUT disable raycastTarget on the image so clicks pass through the black areas.
                GraphicRaycaster gr = glaucomaCanvas.GetComponent<GraphicRaycaster>();
                
                // Also ensure RawImage itself is not a raycast target
                RawImage[] raws = glaucomaCanvas.GetComponentsInChildren<RawImage>(true);
                foreach(var r in raws) r.raycastTarget = false;
                
                Debug.Log("[SignageControllerS3] Auto-fixed Glaucoma Canvas: Mode=Camera, Order=-100, Image RaycastTarget=False.");
            }
        }

        public void UpdateAll()
        {
            UpdatePosition();
            UpdateObjectScale();
            UpdateColors();
        }

        private void UpdatePosition()
        {
            // Only Y Control now
            Vector3 pos = transform.position;
            pos.y = (heightCm + heightOffsetCm) / 100f; 
            transform.position = pos;
        }

        private void UpdateObjectScale()
        {
            // Scale ROOT transform
            float ratio = 1f;
            if (referenceHeightMm > 0.001f)
            {
                ratio = charHeightMm / referenceHeightMm;
            }
            
            transform.localScale = baseScale * ratio;
        }

        private void UpdateColors()
        {
            // 1. Wall Visibility & Color
            if (wallObjects != null)
            {
                foreach (var obj in wallObjects)
                {
                    if (obj != null) 
                    {
                        obj.SetActive(wallEnabled);
                        if (wallEnabled) ApplyColorToGameObject(obj, LrvToColor(wallLrv));
                    }
                }
            }

            // 2. Board Visibility & Color
            if (boardObjects != null)
            {
                foreach (var obj in boardObjects)
                {
                    if (obj != null) 
                    {
                        obj.SetActive(boardEnabled);
                        if (boardEnabled) ApplyColorToGameObject(obj, LrvToColor(boardLrv));
                    }
                }
            }

            // 3. Text Visibility & Color
            if (textObjects != null)
            {
                foreach (var obj in textObjects)
                {
                    if (obj != null) 
                    {
                        obj.SetActive(textEnabled);
                        if (textEnabled) ApplyColorToGameObject(obj, LrvToColor(textLrv));
                    }
                }
            }
        }

        private void ApplyColorToGameObject(GameObject obj, Color c)
        {
            if (obj == null) return;
            
            // Renderers (Mesh)
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (var rnd in renderers)
            {
                rnd.GetPropertyBlock(propBlock);
                propBlock.SetColor("_BaseColor", c); 
                rnd.SetPropertyBlock(propBlock);
            }
            
            // TMP Text
            TMP_Text[] tmps = obj.GetComponentsInChildren<TMP_Text>();
            foreach (var tmp in tmps)
            {
                tmp.color = c;
            }
        }

        private Color LrvToColor(float lrv)
        {
            // Linear RGB approximation
            float v = Mathf.Clamp01(lrv / 100f);
            return new Color(v, v, v, 1f);
        }
    }
}
