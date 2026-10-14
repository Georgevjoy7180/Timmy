using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gameplay
{
    public class CollisionDetection : MonoBehaviour
    {
        [SerializeField] GameObject Player;
        [SerializeField] GameObject PlayerAnim;
        [SerializeField] AudioSource collisionFX;
        [SerializeField] GameObject mainCam;
        [SerializeField] GameObject FadeOut;

        public void InitializeForTest(GameObject playerObj)
        {
            Player = playerObj;
        }

        private bool hasCollided = false;

        public bool HasCollided => hasCollided;

        public void TriggerCollision(Collider other = null)
        {
            OnTriggerEnter(other);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Guard against repeated or simultaneous collision triggers
            if (hasCollided)
            {
                return;
            }

            hasCollided = true;

            // Notify GameManager that the game is over
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }

            StartCoroutine(CollisionEnd());
        }

        private IEnumerator CollisionEnd()
        {
            if (collisionFX != null)
            {
                collisionFX.Play();
            }

            if (Player != null)
            {
                var movement = Player.GetComponent<PlayerMovement>();
                if (movement != null)
                {
                    movement.enabled = false;
                }
            }

            if (PlayerAnim != null)
            {
                var animator = PlayerAnim.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.Play("Stumble Backwards");
                }
            }

            if (mainCam != null)
            {
                var camAnimator = mainCam.GetComponent<Animator>();
                if (camAnimator != null)
                {
                    camAnimator.Play("CollisionCam");
                }
            }

            yield return new WaitForSeconds(3);

            if (FadeOut != null)
            {
                FadeOut.SetActive(true);
            }

            if (SceneLoadProfiler.Instance != null)
            {
                SceneLoadProfiler.Instance.LoadSceneAndMeasure("MainMenu");
            }
            else
            {
                SceneManager.LoadScene("MainMenu");
            }
        }
    }
}
