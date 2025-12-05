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

        // [LINK] 1. FlagHub의 PlayerKill 이벤트 구독 (연결)
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
        // [LINK] 2. 오브젝트가 파괴될 때 구독 해제 (중요: 메모리 누수 방지)
        if (suin_FlagHub.instance != null)
        {
            suin_FlagHub.instance.OnPlayerKillFlag -= OnPlayerKillFlagReceived;
        }
    }

    // [LINK] 3. 킬 플래그가 들어왔을 때 실행되는 함수
    private void OnPlayerKillFlagReceived()
    {
        // 게임 플레이 중에만 죽음 처리
        if (CurrentState == GameState.Playing)
        {
            Debug.Log("💀 [GameManager] Kill Flag 수신 → SetState(GameOver) 호출");
            
            // 직접 컨트롤러를 부르지 않고 State Machine을 통해 전환
            SetState(GameState.GameOver);
        }
    }

    // ---- Public Actions ----
    public void StartGame()        => SetState(GameState.Playing);
    public void TriggerGameOver()  => SetState(GameState.GameOver);
    public void BackToOpening()    => SetState(GameState.Opening);
    public void TriggerGameClear() => SetState(GameState.GameClear);

    // ---- State Machine ----
    void SetState(GameState newState)
    {
        CurrentState = newState;
        Debug.Log("Game State → " + newState);

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
                if (gameOverCtrl)  gameOverCtrl.TriggerGameOver();
                break;

            case GameState.GameClear:
                if (openingCanvas) openingCanvas.SetActive(false);
                if (gameOverCtrl) gameOverCtrl.HideGameOverRig();

                if (gameClearRig) gameClearRig.SetActive(true);
                break;


        }
    }
}