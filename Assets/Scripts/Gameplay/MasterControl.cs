using UnityEngine;
using Gameplay;

public class MasterControl : MonoBehaviour
{
    public static int coinCount = 0;
    [SerializeField] GameObject coinDisplay;

    void Update()
    {
        if (coinDisplay != null)
        {
            var tmpText = coinDisplay.GetComponent<TMPro.TMP_Text>();
            if (tmpText != null)
            {
                tmpText.text = ScoreLogic.FormatCoinDisplay(coinCount);
            }
        }
    }
}
