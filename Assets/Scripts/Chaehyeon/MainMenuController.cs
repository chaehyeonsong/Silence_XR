using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject panelAbout;

    [Header("Main Menu Root")]
    [SerializeField] private GameObject mainMenuRoot;   // 시작 버튼 / 설명 버튼이 들어있는 루트 패널(또는 Canvas)

    [Header("About")]
    [SerializeField] private TextMeshProUGUI aboutTitle;
    [SerializeField] private TextMeshProUGUI aboutBody;

    [Header("BGM")]
    [SerializeField] private AudioSource bgm;

    [Header("Opening Video")]
    [SerializeField] private VideoPlayer openingVideo;  // 오프닝 영상 VideoPlayer
    [SerializeField] private GameObject openingCanvas;  // 오프닝 영상이 올라간 Canvas (검은 배경 + RawImage)

    [Header("Skip Input (XRI)")]
    [SerializeField] private InputActionReference skipAction;  // XRI Input Actions에서 드래그해서 연결

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "Level_opening2"; // 실제 씬 명으로 변경

    [Header("GameController")]
    [SerializeField] private GameObject GameController;

    [Header("GamePrefab")]
    [SerializeField] private GameObject gamePrefab;

    private GameController gameController;

    private void Awake()
    {
        // Debug.Log("MainMenuController Awake");

        if (panelAbout != null)
            panelAbout.SetActive(false);

        // 오프닝 영상이 있으면 : 영상 먼저 재생, 메인 메뉴/ BGM은 나중에
        if (openingVideo != null)
        {
            // 오프닝용 캔버스 ON
            if (openingCanvas != null)
                openingCanvas.SetActive(true);

            // // 메인 메뉴는 숨겨두기
            // if (mainMenuRoot != null)
            //     mainMenuRoot.SetActive(false);

            // 혹시 BGM이 이미 재생 중이면 끄기
            if (bgm != null && bgm.isPlaying)
                bgm.Stop();

            // 영상 끝났을 때 콜백 등록 후 재생
            openingVideo.loopPointReached += OnOpeningFinished;
            openingVideo.Play();
        }
        else
        {
            // 오프닝 영상이 없으면 바로 메인 메뉴 + BGM
            ShowMainMenuAndPlayBgm();
        }
    }

    private void OnEnable()
    {
        // Debug.Log("MainMenuController OnEnable");
        if (skipAction != null && skipAction.action != null)
        {
            // Debug.Log("Skip butten is clicked");
            skipAction.action.performed += OnSkipPerformed;
            skipAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        // Debug.Log("MainMenuController OnDisable");
        if (skipAction != null && skipAction.action != null)
        {
            skipAction.action.performed -= OnSkipPerformed;
            skipAction.action.Disable();
        }
    }

    private void OnSkipPerformed(InputAction.CallbackContext ctx)
    {
        SkipOpening();
    }

    public void SkipOpening()
    {
        if (openingVideo != null && openingVideo.isPlaying)
        {
            openingVideo.Stop();
            openingVideo.loopPointReached -= OnOpeningFinished;
        }

        if (openingCanvas != null)
            openingCanvas.SetActive(false);

        ShowMainMenuAndPlayBgm();
    }

    private void OnDestroy()
    {
        if (openingVideo != null)
            openingVideo.loopPointReached -= OnOpeningFinished;
    }

    private void OnOpeningFinished(VideoPlayer vp)
    {
        // 오프닝 캔버스 끄기
        if (openingCanvas != null)
            openingCanvas.SetActive(false);

        // 메인 메뉴 띄우고 BGM 재생
        ShowMainMenuAndPlayBgm();
    }

    private void ShowMainMenuAndPlayBgm()
    {
        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(true);

        if (bgm != null)
        {
            bgm.loop = true;
            if (!bgm.isPlaying)
                bgm.Play();
        }
    }

    public void OnClickStartEasyGame()
    {
        //if (!string.IsNullOrEmpty(gameSceneName))
        //    SceneManager.LoadScene(gameSceneName);

        gamePrefab.SetActive(true);
        gameController = GameController.GetComponent<GameController>();
        gameController.Linetracer_difficulty = "easy";
        gameController.Liquid_difficulty = "easy";
        gameController.GameSetup();

        transform.Find("Displayed").gameObject.SetActive(false);

    }

    public void OnClickStartNormalGame()
    {
        //if (!string.IsNullOrEmpty(gameSceneName))
        //    SceneManager.LoadScene(gameSceneName);

        gamePrefab.SetActive(true);
        gameController = GameController.GetComponent<GameController>();
        gameController.Linetracer_difficulty = "normal";
        gameController.Liquid_difficulty = "normal";
        gameController.GameSetup();

        transform.Find("Displayed").gameObject.SetActive(false);
    }

    public void OnClickStartHardGame()
    {
        //if (!string.IsNullOrEmpty(gameSceneName))
        //    SceneManager.LoadScene(gameSceneName);

        gamePrefab.SetActive(true);
        gameController = GameController.GetComponent<GameController>();
        gameController.Linetracer_difficulty = "hard";
        gameController.Liquid_difficulty = "hard";
        gameController.GameSetup();

        transform.Find("Displayed").gameObject.SetActive(false);
    }

    public void OnClickStartInsaneGame()
    {
        //if (!string.IsNullOrEmpty(gameSceneName))
        //    SceneManager.LoadScene(gameSceneName);

        gamePrefab.SetActive(true);
        gameController = GameController.GetComponent<GameController>();
        gameController.Linetracer_difficulty = "insane";
        gameController.Liquid_difficulty = "insane";
        gameController.GameSetup();

        transform.Find("Displayed").gameObject.SetActive(false);
    }

    public void OnClickOpenAbout()
    {
        if (panelAbout != null)
            panelAbout.SetActive(true);
    }

    public void OnClickCloseAbout()
    {
        if (panelAbout != null)
            panelAbout.SetActive(false);
    }

    public void SetAbout(string title, string body)
    {
        if (aboutTitle) aboutTitle.text = title;
        if (aboutBody) aboutBody.text = body;
    }
}
