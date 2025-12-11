using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject panelTutorial;
    [SerializeField] private Vector3 panelPosition;
    [SerializeField] private List<GameObject> ToggledByHelp;
    [SerializeField] private GameObject panelHelp;

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

    //[Header("Skip Input (XRI)")]
    //[SerializeField] private InputActionReference skipAction;  // XRI Input Actions에서 드래그해서 연결

    // [Header("Scene")]
    // [SerializeField] private string gameSceneName = "Main_Scene"; // 실제 씬 명으로 변경

    [Header("GamePrefab")]
    [SerializeField] private GameObject gamePrefab;

    [Header("GameController")]
    [SerializeField] private GameController gameController;

    [Header("TrimController")]
    [SerializeField] private LineColorManager lineColorManager;
    [SerializeField] private LiquidColorManager liquidColorManager;

    private bool isLiquidSet = false;
    private bool isLineSet = false;
    private bool isTutorialOn = false;

    private GameObject Help;
   



    private void Awake()
    {
        Help = Instantiate(panelTutorial);
        Help.SetActive(false);
        Help.transform.position += panelPosition;
        isLiquidSet = false;
        isLineSet = false;

        // Debug.Log("MainMenuController Awake");

        if (gamePrefab != null)
            gamePrefab.SetActive(false);

        //if (panelAbout != null)
        //    panelAbout.SetActive(false);
        openingCanvas.SetActive(false);

        ShowMainMenuAndPlayBgm();

    }

    private void OnEnable()
    {
        // Debug.Log("MainMenuController OnEnable");
        //if (skipAction != null && skipAction.action != null)
        //{
        //    // Debug.Log("Skip butten is clicked");
        //    skipAction.action.performed += OnSkipPerformed;
        //    skipAction.action.Enable();
        //}
        Help = Instantiate(panelTutorial);
        Help.SetActive(false);
        Help.transform.position += panelPosition;
        lineColorManager.Reset();
        liquidColorManager.Reset();
        isLiquidSet = false;
        isLineSet = false;

        // Debug.Log("MainMenuController Awake");

        if (gamePrefab != null)
            gamePrefab.SetActive(false);

        //if (panelAbout != null)
        //    panelAbout.SetActive(false);
        openingCanvas.SetActive(false);

        ShowMainMenuAndPlayBgm();

    }

    private void OnDisable()
    {
        // Debug.Log("MainMenuController OnDisable");
    }

    //private void OnSkipPerformed(InputAction.CallbackContext ctx)
    //{
    //    SkipOpening();
    //}

    public void SkipOpening()
    {
        Debug.Log("Called SkipOpening");
        if (openingVideo != null && openingVideo.isPlaying)
        {
            openingVideo.Stop();
            Debug.Log("stopped video");
            openingVideo.loopPointReached -= OnOpeningFinished;
        }

        if (openingCanvas != null)
            openingCanvas.SetActive(false);
        Debug.Log("closed opening canvas");

        // ShowMainMenuAndPlayBgm();
        StartGame();
        Debug.Log("called StartGame");
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
        // ShowMainMenuAndPlayBgm();
        StartGame();
        
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

    private void StartGame()
    {
        //if (skipAction != null && skipAction.action != null)
        //{
        //    Debug.Log("Erase skip command");
        //    skipAction.action.performed -= OnSkipPerformed;
        //}
        
        mainMenuRoot.SetActive(false);

        gamePrefab.SetActive(true);
        gameController.GameSetup();

        // ✅ [추가됨] GameManager에게 게임이 시작되었음을 알림 (State를 Playing으로 변경)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame(); 
        }
        else
        {
            Debug.LogWarning("GameManager Instance를 찾을 수 없습니다.");
        }
   
        Destroy(Help);
    }

    public void OnClickStartGame() // This would be called by Play button
    {
        //if (!string.IsNullOrEmpty(gameSceneName))
        //    SceneManager.LoadScene(gameSceneName);

        if (isLineSet && isLiquidSet) // Only start when both difficulties set
        {
            if (openingVideo != null)
            {
                // 오프닝용 캔버스 ON
                if (openingCanvas != null)
                    openingCanvas.SetActive(true);

                // // 메인 메뉴는 숨겨두기
                if (mainMenuRoot != null)
                    mainMenuRoot.SetActive(false);

                // 혹시 BGM이 이미 재생 중이면 끄기
                if (bgm != null && bgm.isPlaying)
                    bgm.Stop();

                // 영상 끝났을 때 콜백 등록 후 재생
                openingVideo.loopPointReached += OnOpeningFinished;
                openingVideo.Play();
            }
            else{
                StartGame();
            }


        }
        else // When difficulty isn't properly decided
        {
            // Currently, do nothing
        }

    }

    public void OnClickSetLineEasy()
    {
        gameController.Linetracer_difficulty = "easy";
        isLineSet = true;
    }

    public void OnClickSetLineNormal()
    {
        gameController.Linetracer_difficulty = "normal";
        isLineSet = true;
    }

    public void OnClickSetLineHard()
    {
        gameController.Linetracer_difficulty = "hard";
        isLineSet = true;
    }

    public void OnClickSetLineInsane()
    {
        gameController.Linetracer_difficulty = "insane";
        isLineSet = true;
    }

    public void OnClickSetLiquidEasy()
    {
        gameController.Liquid_difficulty = "easy";
        isLiquidSet = true;
    }

    public void OnClickSetLiquidNormal()
    {
        gameController.Liquid_difficulty = "normal";
        isLiquidSet = true;
    }

    public void OnClickSetLiquidHard()
    {
        gameController.Liquid_difficulty = "hard";
        isLiquidSet = true;
    }

    public void OnClickSetLiquidInsane()
    {
        gameController.Liquid_difficulty = "insane";
        isLiquidSet = true;
    }


    public void OnClickAbout()
    {
        //if (panelAbout != null)
        //    panelAbout.SetActive(true);

        if (!isTutorialOn)
        {

            foreach (GameObject item in ToggledByHelp)
            {
                item.SetActive(false);
            }

            Help.SetActive(true);
            isTutorialOn = true;

        }
        else if (isTutorialOn)
        {

            foreach (GameObject item in ToggledByHelp)
            {
                item.SetActive(true);
            }

            Help.SetActive(false);
            isTutorialOn = false;

        }

    }

    //public void OnClickCloseAbout()
    //{
    //    if (panelAbout != null)
    //        panelAbout.SetActive(false);
    //}

    public void SetAbout(string title, string body)
    {
        if (aboutTitle) aboutTitle.text = title;
        if (aboutBody) aboutBody.text = body;
    }
}
