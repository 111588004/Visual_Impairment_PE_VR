using System;
using System.IO;
using UnityEngine;

public class GazeDataRecorder : MonoBehaviour
{
    [Header("Gaze Sources")]
    [Tooltip("Drag the 'LeftGaze' object here")]
    public UpdateEyeGaze leftGaze;
    [Tooltip("Drag the 'RightGaze' object here")]
    public UpdateEyeGaze rightGaze;

    [Header("Recording Settings")]
    [SerializeField] private float sampleInterval = 0.1f;
    [SerializeField] private int flushEveryNSamples = 20;

    private StreamWriter _writer;
    private float _timer;
    private int _samplesSinceFlush;

    void Start()
    {
        string dir;
#if UNITY_EDITOR
        dir = Path.Combine(Application.dataPath, "Data/GazeLogs");
#else
        dir = Path.Combine(Application.persistentDataPath, "GazeLogs");
#endif
        Directory.CreateDirectory(dir);

        string fileName = $"gaze_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        string path = Path.Combine(dir, fileName);

        _writer = new StreamWriter(path, false) { AutoFlush = false };
        _writer.WriteLine("Timestamp,Eye,PosX,PosY,PosZ,IsTracking,HitSurface,HitObjectName,LocalOriginX,LocalOriginY,LocalOriginZ,LocalDirX,LocalDirY,LocalDirZ");

        Debug.Log($"[GazeDataRecorder] Recording to {path}");
    }

    void Update()
    {
        if (_writer == null) return;

        _timer += Time.deltaTime;
        if (_timer < sampleInterval) return;
        _timer = 0f;

        WriteSample("Left", leftGaze);
        WriteSample("Right", rightGaze);

        _samplesSinceFlush++;
        if (_samplesSinceFlush >= flushEveryNSamples)
        {
            _writer.Flush();
            _samplesSinceFlush = 0;
        }
    }

    private void WriteSample(string eyeLabel, UpdateEyeGaze gaze)
    {
        if (gaze == null) return;

        Vector3 pos = gaze.transform.position;
        Vector3 lo = gaze.LastLocalGazeOrigin;
        Vector3 ld = gaze.LastLocalGazeDir;
        _writer.WriteLine(string.Join(",",
            Time.time.ToString("F3"),
            eyeLabel,
            pos.x.ToString("F4"),
            pos.y.ToString("F4"),
            pos.z.ToString("F4"),
            gaze.IsTracking,
            gaze.LastHitSurface,
            gaze.LastHitObjectName,
            lo.x.ToString("F5"),
            lo.y.ToString("F5"),
            lo.z.ToString("F5"),
            ld.x.ToString("F5"),
            ld.y.ToString("F5"),
            ld.z.ToString("F5")));
    }

    private void CloseWriter()
    {
        if (_writer == null) return;
        _writer.Flush();
        _writer.Close();
        _writer = null;
    }

    void OnApplicationQuit()
    {
        CloseWriter();
    }

    void OnDestroy()
    {
        CloseWriter();
    }
}
