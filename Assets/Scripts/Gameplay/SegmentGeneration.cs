using System.Collections.Generic;
using UnityEngine;

public class SegmentGeneration : MonoBehaviour
{
    [Header("Segment Configuration")]
    [Tooltip("Collection of segment prefabs/templates to cycle through.")]
    [SerializeField] private GameObject[] segment;

    [Tooltip("Length of each track segment along the Z-axis in meters.")]
    [SerializeField] private float segmentLength = 50f;

    [Tooltip("Number of track segments maintained active ahead of the player at all times.")]
    [SerializeField] private int initialSegmentsCount = 4;

    [Tooltip("Distance behind the player before an old segment is recycled to the front.")]
    [SerializeField] private float despawnDistanceBehindPlayer = 60f;

    [Header("Player Tracking")]
    [Tooltip("Optional direct reference to the player transform. If unassigned, automatically finds PlayerMovement.")]
    [SerializeField] private Transform playerTransform;

    // Track active segments in spatial chronological order
    private readonly Queue<GameObject> activeSegments = new Queue<GameObject>();

    // Memory buffer caching child CollectCoin components for fast zero-allocation reactivation
    private readonly Dictionary<GameObject, CollectCoin[]> cachedCoinsMap = new Dictionary<GameObject, CollectCoin[]>();

    private float nextSpawnZ = 50f;

    private void Start()
    {
        // 1. Resolve player transform reference if not explicitly wired in the inspector
        if (playerTransform == null)
        {
            var playerMovement = Object.FindAnyObjectByType<PlayerMovement>();
            if (playerMovement != null)
            {
                playerTransform = playerMovement.transform;
            }
        }

        // 2. Pre-warm the initial track segments ahead of the player
        for (int i = 0; i < initialSegmentsCount; i++)
        {
            SpawnNextSegment();
        }
    }

    private void Update()
    {
        // Guard if segments have not been configured
        if (segment == null || segment.Length == 0)
        {
            return;
        }

        // Fallback lazy resolution for player transform (e.g. if spawned dynamically)
        if (playerTransform == null)
        {
            var playerMovement = Object.FindAnyObjectByType<PlayerMovement>();
            if (playerMovement != null)
            {
                playerTransform = playerMovement.transform;
            }
            else
            {
                return;
            }
        }

        // 3. Check if the oldest segment has passed behind the player beyond the despawn boundary
        if (activeSegments.Count > 0)
        {
            GameObject oldestSegment = activeSegments.Peek();
            if (oldestSegment != null)
            {
                // If the player has traveled past the end of this segment
                if (playerTransform.position.z - oldestSegment.transform.position.z > despawnDistanceBehindPlayer)
                {
                    RecycleOldestSegment();
                }
            }
        }
    }

    /// <summary>
    /// Spawns a new segment at the leading edge of the track during startup.
    /// </summary>
    private void SpawnNextSegment()
    {
        int randomIndex = Random.Range(0, segment.Length);
        GameObject prefab = segment[randomIndex];
        if (prefab == null) return;

        Vector3 spawnPosition = new Vector3(0f, 0f, nextSpawnZ);
        GameObject newSegment = Instantiate(prefab, spawnPosition, Quaternion.identity, transform);

        // Cache child components on first instantiation so recycling never calls GetComponentInChildren
        CollectCoin[] coins = newSegment.GetComponentsInChildren<CollectCoin>(true);
        cachedCoinsMap[newSegment] = coins;

        activeSegments.Enqueue(newSegment);
        nextSpawnZ += segmentLength;
    }

    /// <summary>
    /// Recycles the oldest segment from behind the player to the front of the track,
    /// avoiding Destroy/Instantiate cycles and restoring all collectible objects.
    /// </summary>
    private void RecycleOldestSegment()
    {
        GameObject recycledSegment = activeSegments.Dequeue();
        if (recycledSegment == null) return;

        // 1. Teleport segment to the leading edge of the track
        recycledSegment.transform.position = new Vector3(0f, 0f, nextSpawnZ);
        nextSpawnZ += segmentLength;

        // 2. Reactivate any collected coins or disabled child props without GC allocations
        if (cachedCoinsMap.TryGetValue(recycledSegment, out CollectCoin[] coins))
        {
            for (int i = 0; i < coins.Length; i++)
            {
                if (coins[i] != null && !coins[i].gameObject.activeSelf)
                {
                    coins[i].gameObject.SetActive(true);
                }
            }
        }

        // 3. Re-queue at the front of the conveyor belt
        activeSegments.Enqueue(recycledSegment);
    }
}
