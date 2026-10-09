using UnityEngine;

public class CollectCoin : MonoBehaviour
{
    [SerializeField] AudioSource CoinFX;

    private void OnTriggerEnter(Collider other)
    {
        CoinFX.Play();
        this.gameObject.SetActive(false);
        MasterControl.coinCount += 1;
    }
}
