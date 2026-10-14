
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PerformanceCollector : MonoBehaviour
{
    [Header("Capture Settings")]
    [SerializeField] private float captureDuration = 60f;
    [SerializeField] private float warmupDuration = 5f;
    [SerializeField] private bool startCaptureOnPlay = true;

    private ProfilerRecorder totalMemoryRecorder;
    private ProfilerRecorder gcMemoryRecorder;

    private StringBuilder csv;
    private float elapsed;
    private float captureElapsed;
    private bool isCapturing;
    private int frameCount;
    private float totalFrameTime;
    private float minFrameTime = float.MaxValue;
    private float maxFrameTime;
    private int sceneLoadStartFrame;

    private string sceneName;
    private float sceneLoadSeconds = -1f;

    private readonly List<float> elapsedSecSamples = new List<float>();
    private readonly List<float> frameTimeMsSamples = new List<float>();
    private readonly List<float> fpsSamples = new List<float>();
    private readonly List<long> totalMemSamples = new List<long>();
    private readonly List<long> gcMemSamples = new List<long>();

    public float AverageFPS =>
        totalFrameTime > 0f ? frameCount / totalFrameTime : 0f;

    private void Awake()
    {
        sceneName = SceneManager.GetActiveScene().name;

        csv = new StringBuilder();
        csv.AppendLine(
            "ElapsedSeconds,FrameTimeMs,FPS,TotalMemoryBytes,GCUsedMemoryBytes");

        // Record the frame on which this component initializes.
        sceneLoadStartFrame = Time.frameCount;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        totalMemoryRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Memory,
            "Total Used Memory",
            15);

        gcMemoryRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Memory,
            "GC Used Memory",
            15);

        if (startCaptureOnPlay)
        {
            StartCoroutine(BeginCaptureAfterWarmup());
        }
    }

    private IEnumerator BeginCaptureAfterWarmup()
    {
        yield return new WaitForSecondsRealtime(warmupDuration);

        StartCapture();
    }

    public void StartCapture()
    {
        csv.Clear();
        csv.AppendLine(
            "ElapsedSeconds,FrameTimeMs,FPS,TotalMemoryBytes,GCUsedMemoryBytes");

        elapsedSecSamples.Clear();
        frameTimeMsSamples.Clear();
        fpsSamples.Clear();
        totalMemSamples.Clear();
        gcMemSamples.Clear();

        elapsed = 0f;
        captureElapsed = 0f;
        frameCount = 0;
        totalFrameTime = 0f;
        minFrameTime = float.MaxValue;
        maxFrameTime = 0f;
        isCapturing = true;

        Debug.Log("Performance capture started.");
    }

    private void Update()
    {
        if (!isCapturing)
            return;

        float deltaTime = Time.unscaledDeltaTime;

        if (deltaTime <= 0f)
            return;

        elapsed += deltaTime;
        captureElapsed += deltaTime;

        float frameTimeMs = deltaTime * 1000f;
        float fps = 1f / deltaTime;

        frameCount++;
        totalFrameTime += deltaTime;
        minFrameTime = Mathf.Min(minFrameTime, frameTimeMs);
        maxFrameTime = Mathf.Max(maxFrameTime, frameTimeMs);

        long totalMemory = totalMemoryRecorder.Valid
            ? totalMemoryRecorder.LastValue
            : -1;

        long gcMemory = gcMemoryRecorder.Valid
            ? gcMemoryRecorder.LastValue
            : -1;

        elapsedSecSamples.Add(elapsed);
        frameTimeMsSamples.Add(frameTimeMs);
        fpsSamples.Add(fps);
        totalMemSamples.Add(totalMemory);
        gcMemSamples.Add(gcMemory);

        csv.Append(elapsed.ToString("F3", CultureInfo.InvariantCulture))
            .Append(',')
            .Append(frameTimeMs.ToString("F3", CultureInfo.InvariantCulture))
            .Append(',')
            .Append(fps.ToString("F2", CultureInfo.InvariantCulture))
            .Append(',')
            .Append(totalMemory)
            .Append(',')
            .Append(gcMemory)
            .AppendLine();

        if (captureElapsed >= captureDuration)
        {
            StopCapture();
        }
    }

    public void StopCapture()
    {
        if (!isCapturing)
            return;

        isCapturing = false;

        string filename =
            $"Performance_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

        string path = Path.Combine(
            Application.persistentDataPath,
            filename);

        try
        {
            File.WriteAllText(path, csv.ToString());

            Debug.Log($"Performance report saved: {path}");
            Debug.Log(
                $"Average FPS: {AverageFPS:F2}, " +
                $"Min frame time: {minFrameTime:F2} ms, " +
                $"Max frame time: {maxFrameTime:F2} ms");

            // Also export reports/performance.json with SceneLoadDurationSec from SceneLoadProfiler if available
            ExportPerformanceJson();
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Failed to save performance report: {exception.Message}");
        }
    }

    private void ExportPerformanceJson()
    {
        var sb = new StringBuilder();
        sb.Append("{\n");
        sb.Append("  \"ElapsedSec\": [").Append(string.Join(",", elapsedSecSamples.ConvertAll(v => v.ToString("F3", CultureInfo.InvariantCulture)))).Append("],\n");
        sb.Append("  \"FrameTim\": [").Append(string.Join(",", frameTimeMsSamples.ConvertAll(v => v.ToString("F3", CultureInfo.InvariantCulture)))).Append("],\n");
        sb.Append("  \"FPS\": [").Append(string.Join(",", fpsSamples.ConvertAll(v => v.ToString("F2", CultureInfo.InvariantCulture)))).Append("],\n");
        sb.Append("  \"TotalMem\": [").Append(string.Join(",", totalMemSamples)).Append("],\n");
        sb.Append("  \"GCUsedMemoryBytes\": [").Append(string.Join(",", gcMemSamples)).Append("],\n");

        float sceneLoadDuration = -1f;
        if (SceneLoadProfiler.Instance != null && SceneLoadProfiler.Instance.LastRecordedDurationSec >= 0f)
        {
            sceneLoadDuration = SceneLoadProfiler.Instance.LastRecordedDurationSec;
        }

        if (sceneLoadDuration >= 0f)
        {
            sb.Append("  \"SceneLoadDurationSec\": [").Append(sceneLoadDuration.ToString("F3", CultureInfo.InvariantCulture)).Append("]\n");
        }
        else
        {
            sb.Append("  \"SceneLoadDurationSec\": []\n");
        }
        sb.Append("}\n");

        string jsonContent = sb.ToString();

        // Save to persistentDataPath/performance.json
        string persistentJsonPath = Path.Combine(Application.persistentDataPath, "performance.json");
        File.WriteAllText(persistentJsonPath, jsonContent);

        // Also save directly to reports/performance.json in project if accessible
        string projectReportsPath = Path.Combine(Application.dataPath, "..", "reports", "performance.json");
        try
        {
            string dir = Path.GetDirectoryName(projectReportsPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(projectReportsPath, jsonContent);
            Debug.Log($"performance.json exported to: {projectReportsPath}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Could not write to project reports folder directly: {ex.Message}");
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        sceneName = scene.name;

        // This records a frame reference, not the full scene-load duration.
        sceneLoadStartFrame = Time.frameCount;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (isCapturing)
            StopCapture();

        totalMemoryRecorder.Dispose();
        gcMemoryRecorder.Dispose();
    }
}
