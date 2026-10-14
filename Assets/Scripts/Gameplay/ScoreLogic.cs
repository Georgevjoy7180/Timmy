namespace Gameplay
{
    /// <summary>
    /// Pure score and coin calculation logic.
    /// Completely decoupled from UI and MonoBehaviour lifecycles for EditMode unit testing.
    /// </summary>
    public static class ScoreLogic
    {
        /// <summary>
        /// Adds points to an existing score, with an optional minimum clamp at 0.
        /// </summary>
        public static int AddPoints(int currentScore, int pointsToAdd)
        {
            int updated = currentScore + pointsToAdd;
            return updated < 0 ? 0 : updated;
        }

        /// <summary>
        /// Increments coin count by a given amount (defaults to 1).
        /// </summary>
        public static int AddCoin(int currentCoins, int count = 1)
        {
            return currentCoins + count;
        }

        /// <summary>
        /// Formats coin counter text string for display.
        /// </summary>
        public static string FormatCoinDisplay(int coins)
        {
            return $"COINS:{coins}";
        }
    }
}
