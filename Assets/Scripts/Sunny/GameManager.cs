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
    }

    // ---- Public Actions ----
    public void StartGame()        => SetState(GameState.Playing);
    public void TriggerGameOver()  => SetState(GameState.GameOver);
    public void BackToOpening()    => SetState(GameState.Opening);

    // ---- State Machine ----
    void SetState(GameState newState)
    {
        CurrentState = newState;
        Debug.Log("Game State → " + newState);

        switch (newState)
        {
            case GameState.Opening:
                if (openingCanvas) openingCanvas.SetActive(true);
                if (gameOverCtrl)  gameOverCtrl.HideGameOverRig();
                break;

            case GameState.Playing:
                if (openingCanvas) openingCanvas.SetActive(false);
                if (gameOverCtrl)  gameOverCtrl.HideGameOverRig();
                break;

            case GameState.GameOver:
                if (openingCanvas) openingCanvas.SetActive(false);
                if (gameOverCtrl)  gameOverCtrl.TriggerGameOver();
                break;
        }
    }
}
