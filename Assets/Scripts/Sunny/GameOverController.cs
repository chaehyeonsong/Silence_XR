using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverController : MonoBehaviour
{
    public GameObject gameOverRig;          // parent under camera
    public Transform leftArm, rightArm;
    public Vector3 leftStartLocalPos, leftEndLocalPos;
    public Vector3 rightStartLocalPos, rightEndLocalPos;

    public GameObject gameOverUI;
    public float armMoveDuration = 1.2f;


    public void TriggerGameOver()
    {
        StartCoroutine(GameOverSequence());
    }

    IEnumerator GameOverSequence()
    {

        // turn on rig
        gameOverRig.SetActive(true);

        // initialise positions
        leftArm.localPosition  = leftStartLocalPos;
        rightArm.localPosition = rightStartLocalPos;
        gameOverUI.SetActive(false);

        // move arms in
        float t = 0f;
        while (t < armMoveDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / armMoveDuration);
            leftArm.localPosition  = Vector3.Lerp(leftStartLocalPos,  leftEndLocalPos,  k);
            rightArm.localPosition = Vector3.Lerp(rightStartLocalPos, rightEndLocalPos, k);
            yield return null;
        }


        gameOverUI.SetActive(true);
    }

    public void OnPlayAgain()
    {
        // however you restart – reload scene or tell GameManager to go back to Playing
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))   // DEBUG: press G to test
        {
            TriggerGameOver();
        }
    }
}   