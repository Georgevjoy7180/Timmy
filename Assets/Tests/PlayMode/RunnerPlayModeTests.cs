using System.Collections;
using System.Collections.Generic;
using Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class RunnerPlayModeTests
    {
        private List<GameObject> spawnedObjects;
        private GameManager gameManager;

        [SetUp]
        public void SetUp()
        {
            spawnedObjects = new List<GameObject>();

            // Setup a GameManager instance for gameplay scenario tests
            GameObject gmObj = new GameObject("GameManager");
            spawnedObjects.Add(gmObj);
            gameManager = gmObj.AddComponent<GameManager>();

            MasterControl.coinCount = 0;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in spawnedObjects)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }
            spawnedObjects.Clear();
            MasterControl.coinCount = 0;
        }

        /// <summary>
        /// Scenario 1: Movement handling - Verifies forward translation over frames.
        /// </summary>
        [UnityTest]
        public IEnumerator Gameplay_Movement_MovesForwardOverFrames()
        {
            GameObject player = new GameObject("Player");
            spawnedObjects.Add(player);

            PlayerMovement movement = player.AddComponent<PlayerMovement>();
            movement.PlayerSpeed = 10f;
            player.transform.position = Vector3.zero;

            float initialZ = player.transform.position.z;

            // Wait 5 frames
            for (int i = 0; i < 5; i++)
            {
                yield return null;
            }

            Assert.Greater(player.transform.position.z, initialZ,
                "Player should have moved forward along the Z axis.");
        }

        /// <summary>
        /// Scenario 2: Movement handling - Boundary limits prevent player from escaping lane.
        /// </summary>
        [UnityTest]
        public IEnumerator Gameplay_Movement_ClampBoundaries_KeepsPositionInsideLimits()
        {
            GameObject player = new GameObject("Player");
            spawnedObjects.Add(player);

            PlayerMovement movement = player.AddComponent<PlayerMovement>();
            movement.LeftLimit = -9f;
            movement.RightLimit = 10f;

            player.transform.position = new Vector3(-9f, 0, 0);
            yield return null;

            Assert.GreaterOrEqual(player.transform.position.x, -9f,
                "Player position X should not be less than LeftLimit.");
        }

        /// <summary>
        /// Scenario 3: Confirm score increases during play upon collecting coins.
        /// </summary>
        [UnityTest]
        public IEnumerator Gameplay_ScoreIncreasesDuringPlay_WhenCollectingCoins()
        {
            GameObject coinObj = new GameObject("Coin");
            spawnedObjects.Add(coinObj);
            coinObj.transform.position = new Vector3(0, 0, 5);

            BoxCollider coinCol = coinObj.AddComponent<BoxCollider>();
            coinCol.isTrigger = true;
            CollectCoin coin = coinObj.AddComponent<CollectCoin>();

            GameObject playerObj = new GameObject("Player");
            spawnedObjects.Add(playerObj);
            playerObj.transform.position = new Vector3(0, 0, 5);
            BoxCollider playerCol = playerObj.AddComponent<BoxCollider>();

            // Directly invoke collection event with the player collider
            coin.TriggerCollection(playerCol);
            yield return null;

            Assert.AreEqual(1, MasterControl.coinCount, "Coin count should increase to 1.");
            Assert.AreEqual(1, gameManager.Score, "GameManager score should increase to 1.");
            Assert.IsFalse(coinObj.activeSelf, "Coin object should be deactivated after collection.");
        }

        /// <summary>
        /// Scenario 4: Collision handling - Obstacle collision stops movement and triggers GameOver.
        /// </summary>
        [UnityTest]
        public IEnumerator Gameplay_CollisionHandling_ObstacleCollision_DisablesMovementAndTriggersGameOver()
        {
            GameObject playerObj = new GameObject("Player");
            spawnedObjects.Add(playerObj);
            PlayerMovement movement = playerObj.AddComponent<PlayerMovement>();
            Assert.IsTrue(movement.enabled, "PlayerMovement should be initially enabled.");

            GameObject obstacleObj = new GameObject("Obstacle");
            spawnedObjects.Add(obstacleObj);
            CollisionDetection collision = obstacleObj.AddComponent<CollisionDetection>();
            collision.InitializeForTest(playerObj);

            // Trigger collision
            collision.TriggerCollision();
            yield return null;

            Assert.IsTrue(collision.HasCollided, "CollisionDetection should mark HasCollided = true.");
            Assert.IsTrue(gameManager.IsGameOver, "GameManager.IsGameOver should be true.");
            Assert.IsFalse(movement.enabled, "PlayerMovement component should be disabled on collision.");
        }

        /// <summary>
        /// Scenario 5: Check that scoring stops after game over.
        /// </summary>
        [UnityTest]
        public IEnumerator Gameplay_ScoringStops_AfterGameOver()
        {
            // Set game over
            gameManager.GameOver();
            Assert.IsTrue(gameManager.IsGameOver);

            // Attempt to collect coin
            GameObject coinObj = new GameObject("Coin");
            spawnedObjects.Add(coinObj);
            coinObj.transform.position = new Vector3(0, 0, 2);

            BoxCollider coinCol = coinObj.AddComponent<BoxCollider>();
            coinCol.isTrigger = true;
            CollectCoin coin = coinObj.AddComponent<CollectCoin>();

            GameObject playerObj = new GameObject("Player");
            spawnedObjects.Add(playerObj);
            playerObj.transform.position = new Vector3(0, 0, 2);
            BoxCollider playerCol = playerObj.AddComponent<BoxCollider>();

            // Attempt collection
            coin.TriggerCollection(playerCol);
            yield return null;

            // Score and coins must NOT increase
            Assert.AreEqual(0, MasterControl.coinCount, "Coin count should remain 0 after game over.");
            Assert.AreEqual(0, gameManager.Score, "GameManager score should remain 0 after game over.");
            Assert.IsTrue(coinObj.activeSelf, "Coin should not be deactivated when game is over.");
        }

        /// <summary>
        /// Scenario 6: Verify restart resets the required state (score, game over flag, coin count).
        /// </summary>
        [UnityTest]
        public IEnumerator Gameplay_Restart_ResetsRequiredState()
        {
            // Simulate active session with score and game over
            gameManager.AddScore(50);
            MasterControl.coinCount = 10;
            gameManager.GameOver();

            Assert.AreEqual(50, gameManager.Score);
            Assert.AreEqual(10, MasterControl.coinCount);
            Assert.IsTrue(gameManager.IsGameOver);

            // Invoke restart/reset
            gameManager.ResetGame();
            yield return null;

            Assert.AreEqual(0, gameManager.Score, "Score should reset to 0.");
            Assert.AreEqual(0, MasterControl.coinCount, "Coin count should reset to 0.");
            Assert.IsFalse(gameManager.IsGameOver, "IsGameOver should reset to false.");
        }

        /// <summary>
        /// Scenario 7: Test edge cases - Repeated collisions handled gracefully without duplicate execution.
        /// </summary>
        [UnityTest]
        public IEnumerator Gameplay_EdgeCase_RepeatedCollisions_ExecutedOnce()
        {
            GameObject playerObj = new GameObject("Player");
            spawnedObjects.Add(playerObj);
            playerObj.AddComponent<PlayerMovement>();

            GameObject obstacleObj = new GameObject("Obstacle");
            spawnedObjects.Add(obstacleObj);
            CollisionDetection collision = obstacleObj.AddComponent<CollisionDetection>();
            collision.InitializeForTest(playerObj);

            // Trigger multiple times rapidly
            collision.TriggerCollision();
            collision.TriggerCollision();
            collision.TriggerCollision();

            yield return null;

            Assert.IsTrue(collision.HasCollided, "HasCollided should remain true.");
            Assert.IsTrue(gameManager.IsGameOver, "GameManager should be game over.");
        }
    }
}
