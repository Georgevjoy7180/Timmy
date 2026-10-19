using UnityEngine;

namespace Gameplay
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public static event System.Action<int> OnCoinsUpdated;
        public static event System.Action<int> OnScoreUpdated;

        public int Score { get; private set; }
        public bool IsGameOver { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void AddScore(int points)
        {
            if (IsGameOver) return;
            Score = ScoreLogic.AddPoints(Score, points);
            OnScoreUpdated?.Invoke(Score);
        }

        public static void NotifyCoinsChanged(int currentCoins)
        {
            OnCoinsUpdated?.Invoke(currentCoins);
        }

        public void GameOver()
        {
            IsGameOver = true;
        }

        public void ResetGame()
        {
            Score = 0;
            IsGameOver = false;
            MasterControl.coinCount = 0;
            OnScoreUpdated?.Invoke(0);
            OnCoinsUpdated?.Invoke(0);
        }
    }
}
