using UnityEngine;
using VIVE.OpenXR;
using VIVE.OpenXR.EyeTracker;

public class UpdateLeftGaze : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float fallbackDistance = 3.0f;
    [SerializeField] private float maxRayDistance = 20.0f;
    [SerializeField] private LayerMask hitLayers = ~0;
    
    [Header("Tracking Reference")]
    [Tooltip("The XR Origin (Rig) to transform local eye data to world space.")]
    public Transform xrOrigin;

    private XrSingleEyeGazeDataHTC[] _gazes = new XrSingleEyeGazeDataHTC[2];
    private bool _warnedOnce = false;

    void Start()
    {
        // Auto-find XR Origin if missing (Assume it's the root or a parent of the camera)
        if (xrOrigin == null)
        {
            var cam = Camera.main;
            if (cam != null && cam.transform.parent != null)
            {
                // Usually XR Origin -> Camera Offset -> Camera
                // We want the root 'XR Origin'
                xrOrigin = cam.transform.root; 
                // Or try to find by component if root is not it
                if (xrOrigin.GetComponent("Unity.XR.CoreUtils.XROrigin") == null)
                     xrOrigin = FindFirstObjectByType<UnityEngine.Transform>(); // Fallback logic is tricky without specific type
            }
        }
    }

    void Update()
    {
        // Ensure we have a reference to transform coordinates
        Transform originTrans = xrOrigin;
        if (originTrans == null && Camera.main != null) originTrans = Camera.main.transform.parent; // Fallback to Camera Parent

        // Check OpenXR Sesision State
        if (UnityEngine.XR.Management.XRGeneralSettings.Instance == null ||
            UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager == null ||
            !UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.isInitializationComplete)
        {
            return;
        }

        try
        {
            if (XR_HTC_eye_tracker.Interop.GetEyeGazeData(out _gazes))
            {
                // Reset warning so we know it works
                _warnedOnce = false;
                
                var leftGaze = _gazes[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];

                if (leftGaze.isValid)
                {
                    // 1. Get Local Data (Relavtive to Tracking Origin)
                    Vector3 localOrigin = leftGaze.gazePose.position.ToUnityVector();
                    Quaternion localRot = leftGaze.gazePose.orientation.ToUnityQuaternion();
                    Vector3 localDir = localRot * Vector3.forward;

                    // 2. Convert to World Space (Critical for moving User)
                    Vector3 worldOrigin = localOrigin;
                    Vector3 worldDir = localDir;

                    if (originTrans != null)
                    {
                        worldOrigin = originTrans.TransformPoint(localOrigin);
                        worldDir = originTrans.TransformDirection(localDir);
                    }
                    else
                    {
                         // If no origin found, assume camera parent-relative or just use camera position approximation
                         if (Camera.main != null) 
                         {
                             // Crude fallback: Eye is at Camera Position
                             worldOrigin = Camera.main.transform.position;
                             // Direction is Camera Rotation * Local Rotation? No, local is absolute to Rig.
                             // Let's just use the Local as World if all else fails, but warn.
                         }
                    }

                    // 3. Raycast in World Space
                    if (Physics.Raycast(worldOrigin, worldDir, out RaycastHit hit, maxRayDistance, hitLayers, QueryTriggerInteraction.Ignore))
                    {
                        transform.position = hit.point;
                        transform.rotation = Quaternion.LookRotation(hit.normal * -1f, Vector3.up);
                    }
                    else
                    {
                        transform.position = worldOrigin + worldDir * fallbackDistance;
                        transform.rotation = Quaternion.LookRotation(-worldDir, Vector3.up);
                    }
                    Debug.DrawRay(worldOrigin, worldDir * Mathf.Min(maxRayDistance, fallbackDistance), Color.green);
                }
            }
            else if (!_warnedOnce)
            {
                _warnedOnce = true;
                Debug.LogWarning("[UpdateLeftGaze] GetEyeGazeData() failed. Check device calibration.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UpdateLeftGaze] Error: {ex.Message}");
        }
    }
}
