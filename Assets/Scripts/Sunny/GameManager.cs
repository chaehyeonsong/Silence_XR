using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Opening,
        Playing,
        GameOver,
        GameClear
    }

    public GameState CurrentState { get; private set; }

    [Header("UI / Rigs")]
    public GameObject openingCanvas;          // Opening UI Canvas
    public GameOverController gameOverCtrl;   // Game over arms + UI controller
    public GameObject gameClearRig;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        SetState(GameState.Opening);

        if (suin_FlagHub.instance != null)
        {
            suin_FlagHub.instance.OnPlayerKillFlag += OnPlayerKillFlagReceived;
            Debug.Log("[GameManager] FlagHub 이벤트 연결됨");
        }
        else
        {
            Debug.LogWarning("[GameManager] suin_FlagHub 인스턴스를 찾을 수 없습니다.");
        }
    }

    void OnDestroy()
    {
        if (suin_FlagHub.instance != null)
        {
            suin_FlagHub.instance.OnPlayerKillFlag -= OnPlayerKillFlagReceived;
        }
    }

    private void OnPlayerKillFlagReceived()
    {
        if (CurrentState == GameState.Playing)
        {
            Debug.Log("💀 [GameManager] Kill Flag 수신 → SetState(GameOver) 호출");
            SetState(GameState.GameOver);
        }
    }

    public void StartGame()        => SetState(GameState.Playing);
    public void TriggerGameOver()  => SetState(GameState.GameOver);
    public void BackToOpening()    => SetState(GameState.Opening);
    public void TriggerGameClear() => SetState(GameState.GameClear);

    // ---- State Machine ----
    void SetState(GameState newState)
    {
        CurrentState = newState;
        Debug.Log("Game State → " + newState);

        // ✅ [추가] 게임 오버 혹은 게임 클리어 시 몬스터 싹 지우기 로직
        if (newState == GameState.GameOver || newState == GameState.GameClear)
        {
            // 씬에 있는 Spawner를 찾아서 청소 명령 내림
            Spawner spawner = FindObjectOfType<Spawner>();
            if (spawner != null)
            {
                spawner.ClearAllMonsters();
            }
        }

        switch (newState)
        {
            case GameState.Opening:
                if (openingCanvas) openingCanvas.SetActive(true);
                if (gameClearRig) gameClearRig.SetActive(false);
                if (gameOverCtrl) gameOverCtrl.HideGameOverRig();
                break;

            case GameState.Playing:
                if (openingCanvas) openingCanvas.SetActive(false);
                if (gameClearRig) gameClearRig.SetActive(false);
                if (gameOverCtrl)  gameOverCtrl.HideGameOverRig();
                break;

            case GameState.GameOver:
                if (openingCanvas) openingCanvas.SetActive(false);
                if (gameClearRig) gameClearRig.SetActive(false);

                // GameOver 상태 진입 시 컨트롤러 작동
                //if (gameOverCtrl)  gameOverCtrl.TriggerGameOver();
                break;

            case GameState.GameClear:
                if (openingCanvas) openingCanvas.SetActive(false);
                if (gameOverCtrl) gameOverCtrl.HideGameOverRig();

                if (gameClearRig) gameClearRig.SetActive(true);
                break;


        }
    }
}