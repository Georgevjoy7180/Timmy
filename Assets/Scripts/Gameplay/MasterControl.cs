using UnityEngine;
using TMPro;
using Gameplay;

/// <summary>
/// Controls the UI display for collected coins.
/// Uses an event-driven observer pattern to eliminate per-frame Update() polling,
/// native GetComponent interop overhead, and redundant string allocations.
/// </summary>
public class MasterControl : MonoBehaviour
{
    public static int coinCount = 0;

    [Tooltip("Reference to the GameObject or UI component containing the TMP_Text display.")]
    [SerializeField] private GameObject coinDisplay;

    private TMP_Text cachedTmpText;

    private void Awake()
    {
        // 1. Cache the TMP_Text component reference once at initialization
        if (coinDisplay != null)
        {
            cachedTmpText = coinDisplay.GetComponent<TMP_Text>();
        }
    }

    private void Start()
    {
        // 2. Render initial display state
        UpdateDisplay(coinCount);
    }

    private void OnEnable()
    {
        // 3. Subscribe to event-driven updates from GameManager
        GameManager.OnCoinsUpdated += UpdateDisplay;
    }

    private void OnDisable()
    {
        // 4. Always unsubscribe on disable/destroy to prevent memory leaks
        GameManager.OnCoinsUpdated -= UpdateDisplay;
    }

    /// <summary>
    /// Updates the TMP text only when the coin count actually changes.
    /// </summary>
    public void UpdateDisplay(int newCount)
    {
        if (cachedTmpText != null)
        {
            cachedTmpText.text = ScoreLogic.FormatCoinDisplay(newCount);
        }
    }
}
