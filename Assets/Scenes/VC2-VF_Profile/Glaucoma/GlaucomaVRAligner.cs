using UnityEngine;
using UnityEngine.UI; // Added for RawImage/Graphic
using UnityEngine.XR;
using System.Collections.Generic;

namespace VISimulation
{
    [ExecuteAlways]
    [RequireComponent(typeof(GlaucomaFieldGenerator))]
    public class GlaucomaVRAligner : MonoBehaviour
    {
        private GlaucomaFieldGenerator generator;
        
        [Header("Target Rendering")]
        public Object targetRenderer; // Can be Renderer or RawImage/Graphic
        private Material targetMaterial;
        
        [Header("Settings")]
        public bool autoSyncFov = true;
        public Camera referenceCamera;

        private void OnEnable()
        {
            generator = GetComponent<GlaucomaFieldGenerator>();
            if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
            if (targetRenderer == null) targetRenderer = GetComponent<Graphic>();
            
            UpdateMaterialRef();
        }

        private void UpdateMaterialRef()
        {
            if (targetRenderer is Renderer r) targetMaterial = r.sharedMaterial;
            else if (targetRenderer is Graphic g) targetMaterial = g.material;
        }

        private void Update()
        {
            if (referenceCamera == null) referenceCamera = Camera.main;
            if (referenceCamera == null) referenceCamera = FindFirstObjectByType<Camera>();
            if (referenceCamera == null) return;

            // 1. Sync FOV
            if (autoSyncFov && generator != null)
            {
                if (Mathf.Abs(generator.referenceFov - referenceCamera.fieldOfView) > 0.01f)
                {
                    generator.referenceFov = referenceCamera.fieldOfView;
                    generator.GenerateTexture();
                }
            }

            // 2. Set Shader Properties
            if (targetMaterial == null) UpdateMaterialRef();
            if (targetMaterial == null) return;

            // Sync Reference FOV to Shader
            targetMaterial.SetFloat("_ReferenceFOV", generator.referenceFov);

            // Gaze Centers
            targetMaterial.SetVector("_GazeCenter", new Vector4(0.5f, 0.5f, 0, 0));
            
            if (XRSettings.enabled && Application.isPlaying)
            {
                targetMaterial.SetVector("_GazeCenterLeft", new Vector4(0.5f, 0.5f, 0, 0));
                targetMaterial.SetVector("_GazeCenterRight", new Vector4(0.5f, 0.5f, 0, 0));
            }
        }
    }
}
