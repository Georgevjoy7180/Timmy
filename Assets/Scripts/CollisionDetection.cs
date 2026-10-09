using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CollisionDetection : MonoBehaviour
{
    [SerializeField] GameObject Player;
    [SerializeField] GameObject PlayerAnim;
    [SerializeField] AudioSource collisionFX;
    [SerializeField] GameObject mainCam;
    [SerializeField] GameObject FadeOut;
    void OnTriggerEnter(Collider other)
    {
        StartCoroutine(CollisionEnd());
    }
    IEnumerator CollisionEnd()
    {
        collisionFX.Play();
        Player.GetComponent<PlayerMovement>().enabled = false;
        PlayerAnim.GetComponent<Animator>().Play("Stumble Backwards");
        mainCam.GetComponent<Animator>().Play("CollisionCam");
        yield return new WaitForSeconds(3);
        FadeOut.SetActive(true);
        yield return new WaitForSeconds(3);
        SceneManager.LoadScene(0);
    }
}
