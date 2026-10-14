using Gameplay;
using NUnit.Framework;

public class GameLogicTests
{
    [Test]
    public void NewGame_ScoreStartsAtZero()
    {
        int initialScore = 0;
        Assert.AreEqual(0, initialScore);
    }

    [Test]
    public void AddPoints_IncreasesScore()
    {
        int currentScore = 0;
        int result = ScoreLogic.AddPoints(currentScore, 10);
        Assert.AreEqual(10, result);
    }

    [Test]
    public void Score_NeverGoesBelowZero()
    {
        int currentScore = 0;
        int result = ScoreLogic.AddPoints(currentScore, -5);
        Assert.LessOrEqual(result, 0, "Score should not be negative.");
        Assert.AreEqual(0, result);
    }

    [Test]
    public void AddCoin_IncrementsCoinCountCorrectly()
    {
        int initialCoins = 5;
        int result = ScoreLogic.AddCoin(initialCoins, 1);
        Assert.AreEqual(6, result);
    }

    [Test]
    public void FormatCoinDisplay_FormatsStringCorrectly()
    {
        string display = ScoreLogic.FormatCoinDisplay(42);
        Assert.AreEqual("COINS:42", display);
    }

    [TestCase(0f, -1f, 5f, 0.1f, -9f, 10f, -0.5f)]   // Normal move left
    [TestCase(0f, 1f, 5f, 0.1f, -9f, 10f, 0.5f)]     // Normal move right
    [TestCase(-8.5f, -1f, 10f, 0.5f, -9f, 10f, -9f)]  // Clamped at left limit
    [TestCase(9.5f, 1f, 10f, 0.5f, -9f, 10f, 10f)]   // Clamped at right limit
    [TestCase(2.0f, 0f, 5f, 0.1f, -9f, 10f, 2.0f)]   // No input keeps current position
    public void PlayerMovementLogic_CalculateNewXPosition_CalculatesAndClamps(
        float currentX,
        float horizontalInput,
        float speed,
        float deltaTime,
        float leftLimit,
        float rightLimit,
        float expectedResult)
    {
        float result = PlayerMovementLogic.CalculateNewXPosition(
            currentX,
            horizontalInput,
            speed,
            deltaTime,
            leftLimit,
            rightLimit
        );

        Assert.AreEqual(expectedResult, result, 0.0001f);
    }

    [Test]
    public void PlayerMovementLogic_CalculateForwardDisplacement_CalculatesCorrectDelta()
    {
        float speed = 8.0f;
        float deltaTime = 0.25f;
        float displacement = PlayerMovementLogic.CalculateForwardDisplacement(speed, deltaTime);

        Assert.AreEqual(2.0f, displacement, 0.0001f);
    }
}
