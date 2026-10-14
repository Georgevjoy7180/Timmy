using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void clickButton()
    {
        if (SceneLoadProfiler.Instance != null)
        {
            SceneLoadProfiler.Instance.LoadSceneAndMeasure("Loading");
        }
        else
        {
            SceneManager.LoadScene("Loading");
        }
    }
}
