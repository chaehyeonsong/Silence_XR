using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameClearController : MonoBehaviour
{
    public GameObject gameClearRig;        
    public Transform leftArm, rightArm;    
    public Vector3 leftStartLocalPos, leftEndLocalPos;
    public Vector3 rightStartLocalPos, rightEndLocalPos;
    CanvasGroup clearCanvasGroup; // fade in

    public GameObject clearUI;
    // Testing effect
    public GameObject bloodSplatter;
    //
    public float armMoveDuration = 1.2f;

    bool running = false;

    // Fade in

    void Awake()
    {
        if (clearUI != null)
        {
            clearCanvasGroup = clearUI.GetComponent<CanvasGroup>();
            if (clearCanvasGroup == null)
                clearCanvasGroup = clearUI.AddComponent<CanvasGroup>();

            clearCanvasGroup.alpha = 0f;   // Start invisible
        }
    }


    public void TriggerGameClear()
    {
        if (!running)
            StartCoroutine(GameClearSequence());

        // Play sounds if you have clear audio
        var audio = FindObjectOfType<GameClearAudio>();
        if (audio != null)
            audio.PlayGameClearSounds();
    }

    public void HideGameClearRig()
    {
        running = false;

        if (gameClearRig)
            gameClearRig.SetActive(false);

        if (clearUI)
            clearUI.SetActive(false);

        if (bloodSplatter)
            bloodSplatter.SetActive(false); // effect

    }

    IEnumerator GameClearSequence()
    {
        running = true;

        // Turn on rig
        gameClearRig.SetActive(true);

        if (gameClearRig)
            gameClearRig.SetActive(true);

        if (clearUI)
            clearUI.SetActive(false);


        if (bloodSplatter)
            bloodSplatter.SetActive(false); // effect
        

        // Reset arms to starting points
        leftArm.localPosition  = leftStartLocalPos;
        rightArm.localPosition = rightStartLocalPos;
        // Slide arms inward
        
        float t = 0f;
        while (t < armMoveDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / armMoveDuration);

            leftArm.localPosition  = Vector3.Lerp(leftStartLocalPos,  leftEndLocalPos,  k);
            rightArm.localPosition = Vector3.Lerp(rightStartLocalPos, rightEndLocalPos, k);

            yield return null;
        }


        // 1. Show UI text
        clearUI.SetActive(true);
        // Start fade-in
        StartCoroutine(FadeInCanvas(clearCanvasGroup, 1.0f));  // fade in over 1 second
        // 2. Wait before blood appears
        float bloodDelay = 0.3f; // change this if you want more/less delay
        yield return new WaitForSeconds(bloodDelay);


        // Prepare blood effect
        bloodSplatter.transform.localScale = Vector3.zero;
        // bloodSplatter.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-30f, 30f)); // (Random rotation)
        bloodSplatter.SetActive(true);

        // Animate scale
        float splashTime = 0.2f;
        float t2 = 0f;

        while (t2 < splashTime)
        {
            t2 += Time.deltaTime;
            float k2 = Mathf.Clamp01(t2 / splashTime);
            bloodSplatter.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, k2);
            yield return null;
        }

        // After animation → show win UI
        //if (clearUI)
            //clearUI.SetActive(true);

        running = false;
    }

    // Same as Play Again button: reset to opening
    public void OnPlayAgain()
    {
        GameManager.Instance.BackToOpening();

        var audio = FindObjectOfType<GameClearAudio>();
        if (audio != null)
            audio.StopGameClearSounds();
    }

    // Back to main menu button

    public void OnMainMenu()
    {
        SceneManager.LoadScene("Main_Scene");
    }


    // fade in
    IEnumerator FadeInCanvas(CanvasGroup cg, float duration)
    {
        cg.alpha = 0f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(t / duration);
            yield return null;
        }

        cg.alpha = 1f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Y)) // Test key for debug
        {
            TriggerGameClear();
        }
    }
}
