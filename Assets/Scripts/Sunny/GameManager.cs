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
    public RaySwitch LeftController;
    public RaySwitch RightController;

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

        // 1. 상태 변경에 따른 몬스터 정리 (Game Over / Clear 시)
        if (newState == GameState.GameOver || newState == GameState.GameClear)
        {
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

                // Enables ray interactor during gameplay
                if (LeftController && RightController)
                {
                    LeftController.RayOn();
                    RightController.RayOn();
                    LeftController.isGamePlaying = false;
                    RightController.isGamePlaying = false;
                }
                break;

            case GameState.Playing:
                if (openingCanvas) openingCanvas.SetActive(false);
                if (gameClearRig) gameClearRig.SetActive(false);
                if (gameOverCtrl) gameOverCtrl.HideGameOverRig();
                
                // Disables ray interactor during gameplay
                if (LeftController && RightController)
                {
                    LeftController.RayOff();
                    RightController.RayOff();
                    LeftController.isGamePlaying = true;
                    RightController.isGamePlaying = true;
                }

                // 🔥 [핵심 수정] 게임 시작(Playing) 시 Spawner를 찾아서 "리셋" 시킵니다.
                // 이걸 해줘야 변수와 코루틴이 초기화되어 몬스터가 다시 나옵니다.
                Spawner spawner = FindObjectOfType<Spawner>();
                if (spawner != null)
                {
                    spawner.ResetSpawner(); 
                }
                else
                {
                    Debug.LogWarning("[GameManager] Spawner를 찾을 수 없습니다!");
                }
                break;

            case GameState.GameOver:
                if (openingCanvas) openingCanvas.SetActive(false);
                if (gameClearRig) gameClearRig.SetActive(false);

                if (gameOverCtrl) gameOverCtrl.TriggerGameOver();

                // Enables ray interactor during gameplay
                if (LeftController && RightController)
                {
                    LeftController.RayOn();
                    RightController.RayOn();
                    LeftController.isGamePlaying = false;
                    RightController.isGamePlaying = false;
                }
                break;

            case GameState.GameClear:
                if (openingCanvas) openingCanvas.SetActive(false);
                if (gameOverCtrl) gameOverCtrl.HideGameOverRig();

                if (gameClearRig) gameClearRig.SetActive(true);

                // Enables ray interactor during gameplay
                if (LeftController && RightController)
                {
                    LeftController.RayOn();
                    RightController.RayOn();
                    LeftController.isGamePlaying = false;
                    RightController.isGamePlaying = false;
                }
                break;
        }
    }
}