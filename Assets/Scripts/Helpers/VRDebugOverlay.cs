using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class VRDebugOverlay : MonoBehaviour
{
    [Header("Settings")]
    public float distance = 1.0f;     // Distance from camera
    public float scale = 0.001f;      // Text scale
    public int maxLines = 15;         // Max log lines to keep
    public bool followCamera = true;  // Simple look-at behavior (Head-Locked)
    public bool dontDestroy = true;   // Persist across scenes

    // Internal
    private TextMeshProUGUI _textMesh;
    private GameObject _canvasObj;
    private Queue<string> _logQueue = new Queue<string>();
    private Camera _mainCam;

    void Awake()
    {
        if (dontDestroy) DontDestroyOnLoad(gameObject);
        CreateDebugCanvas();
    }

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void Update()
    {
        // 1. Ensure Camera
        if (_mainCam == null) _mainCam = Camera.main;
        if (_mainCam == null) return;

        // 2. Head-Locked Movement (Smooth)
        if (followCamera && _canvasObj != null)
        {
            Vector3 targetPos = _mainCam.transform.position + (_mainCam.transform.forward * distance);
            // Height adjust: Keep it slightly below eye level so it doesn't block view
            targetPos -= _mainCam.transform.up * 0.2f; 

            _canvasObj.transform.position = Vector3.Lerp(_canvasObj.transform.position, targetPos, Time.deltaTime * 5f);
            
            // Face Camera
            _canvasObj.transform.rotation = Quaternion.LookRotation(_canvasObj.transform.position - _mainCam.transform.position);
        }
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // FILTER: Ignore URP RenderGraph warnings spamming the view
        if (logString.Contains("RenderGraph") || logString.Contains("RecordRenderGraph")) return;

        string color = "white";
        if (type == LogType.Warning) color = "yellow";
        if (type == LogType.Error || type == LogType.Exception) color = "red";

        // Append Trace for Errors
        string entry = $"<color={color}>{logString}</color>";
        // if (type == LogType.Error || type == LogType.Exception) entry += $"\n<size=80%>{stackTrace}</size>";

        _logQueue.Enqueue(entry);

        while (_logQueue.Count > maxLines)
        {
            _logQueue.Dequeue();
        }

        UpdateText();
    }

    void UpdateText()
    {
        if (_textMesh != null)
        {
            _textMesh.text = string.Join("\n", _logQueue);
        }
    }

    void CreateDebugCanvas()
    {
        // Create Canvas Object
        _canvasObj = new GameObject("VR_Debug_HUD");
        _canvasObj.transform.SetParent(this.transform);
        
        Canvas c = _canvasObj.AddComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        c.worldCamera = Camera.main;

        // Add sorting to be on top
        c.sortingOrder = 9999; 

        // CanvasScaler
        CanvasScaler cs = _canvasObj.AddComponent<CanvasScaler>();
        cs.dynamicPixelsPerUnit = 10;
        
        // RectTransform
        RectTransform rt = _canvasObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(800, 600);
        rt.localScale = Vector3.one * scale;

        // Text Object
        GameObject txtObj = new GameObject("Debug_Text");
        txtObj.transform.SetParent(_canvasObj.transform, false);
        
        _textMesh = txtObj.AddComponent<TextMeshProUGUI>();
        _textMesh.alignment = TextAlignmentOptions.BottomLeft;
        _textMesh.fontSize = 24;
        _textMesh.color = Color.white;
        _textMesh.raycastTarget = false; // Important: Don't block Raycasts!
        
        // Background (RawImage for readability)
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(_canvasObj.transform, false);
        bgObj.transform.SetAsFirstSibling();
        
        RawImage img = bgObj.AddComponent<RawImage>();
        img.color = new Color(0, 0, 0, 0.5f);
        img.raycastTarget = false;
        
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        
        Debug.Log("[VRDebugOverlay] HUD initialized.");
        HandleLog("Debug Console Ready...", "", LogType.Log);
    }
}
