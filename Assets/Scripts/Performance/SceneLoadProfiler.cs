
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public class SceneLoadProfiler : MonoBehaviour
{
    public static SceneLoadProfiler Instance { get; private set; }

    public float LastRecordedDurationSec { get; private set; } = -1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadSceneAndMeasure(string sceneName)
    {
        StartCoroutine(MeasureSceneLoad(sceneName));
    }

    private IEnumerator MeasureSceneLoad(string sceneName)
    {
        var stopwatch = Stopwatch.StartNew();

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

        if (operation == null)
        {
            Debug.LogError($"Failed to start loading scene: {sceneName}");
            yield break;
        }

        // Wait until Unity finishes loading and activates the scene.
        while (!operation.isDone)
        {
            yield return null;
        }

        stopwatch.Stop();

        LastRecordedDurationSec = (float)stopwatch.Elapsed.TotalSeconds;
        double loadDurationMs = stopwatch.Elapsed.TotalMilliseconds;

        Debug.Log(
            $"[QA Performance] Scene: {sceneName} | " +
            $"Async load duration: {loadDurationMs:F2} ms"
        );
    }
}
