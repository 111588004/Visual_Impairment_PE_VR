using UnityEngine;
using VIVE.OpenXR;
using VIVE.OpenXR.EyeTracker;

public class UpdateEyeGaze : MonoBehaviour
{
    public enum EyeTarget
    {
        Left,
        Right
    }

    [Header("Eye Settings")]
    [Tooltip("Which eye to track?")]
    public EyeTarget targetEye = EyeTarget.Left;

    [Header("Raycast Settings")]
    [SerializeField] private float minRayDistance = 1.0f; // Prevent getting too close to eye
    [SerializeField] private float fallbackDistance = 3.0f;
    [SerializeField] private float maxRayDistance = 20.0f;
    [SerializeField] private LayerMask hitLayers = ~0;
    
    [Header("Tracking Reference")]
    [Tooltip("The XR Origin (Rig) to transform local eye data to world space.")]
    public Transform xrOrigin;

    private XrSingleEyeGazeDataHTC[] _gazes = new XrSingleEyeGazeDataHTC[2];
    private bool _warnedOnce = false;
    
    // Public property to check if this tracker is currently valid
    public bool IsTracking { get; private set; } = false;

    void Start()
    {
        // Auto-find XR Origin if missing (Assume it's the root or a parent of the camera)
        if (xrOrigin == null)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                // Try to find the XROrigin component in parent hierarchy
                xrOrigin = cam.transform.root; 
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
                
                // Determine index based on enum
                int eyeIndex = (targetEye == EyeTarget.Left) ? 
                    (int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC : 
                    (int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC;
                
                var eyeGaze = _gazes[eyeIndex];

                if (eyeGaze.isValid)
                {
                    IsTracking = true;

                    // 1. Get Local Data (Relavtive to Tracking Origin)
                    Vector3 localOrigin = eyeGaze.gazePose.position.ToUnityVector();
                    Quaternion localRot = eyeGaze.gazePose.orientation.ToUnityQuaternion();
                    Vector3 localDir = localRot * Vector3.forward;

                    // 2. Convert to World Space (Critical for moving User)
                    Vector3 worldOrigin = localOrigin;
                    Vector3 worldDir = localDir;

                    if (originTrans != null)
                    {
                        worldOrigin = originTrans.TransformPoint(localOrigin);
                        worldDir = originTrans.TransformDirection(localDir);
                    }
                    else if (Camera.main != null)
                    {
                         // Fallback: If absolutely no rig found, assume eye is relative to camera (not ideal but better than nothing)
                         worldOrigin = Camera.main.transform.position;
                    }

                    // 3. Raycast in World Space
                    if (Physics.Raycast(worldOrigin, worldDir, out RaycastHit hit, maxRayDistance, hitLayers, QueryTriggerInteraction.Ignore))
                    {
                        // CLAMP: If too close, force it to min focus distance
                        float finalDistance = Mathf.Max(hit.distance, minRayDistance);
                        transform.position = worldOrigin + worldDir * finalDistance;
                        
                        // If it's a real hit (not clamped), look at normal. Otherwise look at eye.
                        if (hit.distance >= minRayDistance)
                            transform.rotation = Quaternion.LookRotation(hit.normal * -1f, Vector3.up);
                        else
                            transform.rotation = Quaternion.LookRotation(-worldDir, Vector3.up);
                    }
                    else
                    {
                        transform.position = worldOrigin + worldDir * fallbackDistance;
                        transform.rotation = Quaternion.LookRotation(-worldDir, Vector3.up);
                    }
                    Debug.DrawRay(worldOrigin, worldDir * Mathf.Min(maxRayDistance, fallbackDistance), (targetEye == EyeTarget.Left ? Color.green : Color.yellow));
                }
                else
                {
                    IsTracking = false;
                }
            }
            else if (!_warnedOnce)
            {
                _warnedOnce = true;
                Debug.LogWarning($"[UpdateEyeGaze] GetEyeGazeData() failed for {targetEye}. Check device calibration.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UpdateEyeGaze] Error: {ex.Message}");
        }
    }
}
