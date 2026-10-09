using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuScript : MonoBehaviour
{
    [SerializeField] GameObject fadeOut;
    [SerializeField] GameObject BounceText;
    [SerializeField] GameObject bigButton;
    [SerializeField] GameObject animCam;
    [SerializeField] GameObject mainCam;
    [SerializeField] GameObject menuControls;
    [SerializeField] AudioSource buttonSelect;
    public static bool hasClicked;
    [SerializeField] GameObject fadeIn;
   
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(FadeInTurnOff());
        if (hasClicked == true) {
            
            mainCam.SetActive(true);
            animCam.SetActive(false);
            bigButton.SetActive(false);
            BounceText.SetActive(false);
            menuControls.SetActive(true);
            
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void MainMenuControl()
    {
        StartCoroutine(AnimCut());
    }
    IEnumerator AnimCut()
    {
        animCam.GetComponent<Animator>().Play("AnimCam");
        bigButton.SetActive(false);
        BounceText.SetActive(false);
        yield return new WaitForSeconds(2.5f);
        fadeIn.SetActive(false);
        animCam.SetActive(false);
        mainCam.SetActive(true);
        menuControls.SetActive(true);
        hasClicked = true;


    }
    IEnumerator FadeInTurnOff()
    {
        yield return new WaitForSeconds(1);
        fadeIn.SetActive(false);
    }
    public void StartGame()
    {
        StartCoroutine(StartButton());
    }
    IEnumerator StartButton()
    {   buttonSelect.Play();
        fadeOut.SetActive(true);
        yield return new WaitForSeconds(1);
        SceneManager.LoadScene(2);
    }
}
