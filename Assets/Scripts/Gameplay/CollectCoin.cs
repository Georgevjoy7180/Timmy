using UnityEngine;
using Gameplay;

public class CollectCoin : MonoBehaviour
{
    [SerializeField] AudioSource CoinFX;

    public void TriggerCollection(Collider other = null)
    {
        OnTriggerEnter(other);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Don't collect or score if game is already over
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            return;
        }

        if (CoinFX != null)
        {
            CoinFX.Play();
        }

        gameObject.SetActive(false);
        MasterControl.coinCount = ScoreLogic.AddCoin(MasterControl.coinCount, 1);
        GameManager.NotifyCoinsChanged(MasterControl.coinCount);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(1);
        }
    }
}
