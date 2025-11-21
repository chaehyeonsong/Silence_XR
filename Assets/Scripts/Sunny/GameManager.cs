using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Opening,
    Playing,
    GameOver,
    Cleared
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // stay across scene loads
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Start wherever you want
        SetState(GameState.Playing);
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case GameState.Opening:
                SceneManager.LoadScene("OpeningScene");
                break;

            case GameState.Playing:
                SceneManager.LoadScene("Level_Background_with_Whatever");
                break;

            case GameState.GameOver:
                SceneManager.LoadScene("Level_Closing");
                break;

            case GameState.Cleared:
                SceneManager.LoadScene("ClearScene");
                break;
        }
    }

    public void TriggerGameOver()  => SetState(GameState.GameOver);
    public void TriggerGameClear() => SetState(GameState.Cleared);
}
