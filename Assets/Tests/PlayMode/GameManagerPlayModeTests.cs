using System.Collections;
using Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class GameManagerPlayModeTests
    {
        private GameObject gameManagerObject;
        private GameManager gameManager;

        [SetUp]
        public void SetUp()
        {
            gameManagerObject = new GameObject("GameManager");
            gameManager = gameManagerObject.AddComponent<GameManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (gameManagerObject != null)
            {
                Object.DestroyImmediate(gameManagerObject);
            }
        }

        [UnityTest]
        public IEnumerator GameManager_InitializesWithZeroScoreAndNotGameOver()
        {
            yield return null;

            Assert.AreEqual(0, gameManager.Score);
            Assert.IsFalse(gameManager.IsGameOver);
        }

        [UnityTest]
        public IEnumerator GameManager_AddScore_IncreasesScore()
        {
            yield return null;

            gameManager.AddScore(100);

            Assert.AreEqual(100, gameManager.Score);
        }

        [UnityTest]
        public IEnumerator GameManager_GameOver_PreventsScoreAddition()
        {
            yield return null;

            gameManager.GameOver();
            gameManager.AddScore(50);

            Assert.IsTrue(gameManager.IsGameOver);
            Assert.AreEqual(0, gameManager.Score);
        }
    }
}
