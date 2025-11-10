using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject panelAbout;

    [Header("About")]
    [SerializeField] private TextMeshProUGUI aboutTitle;
    [SerializeField] private TextMeshProUGUI aboutBody;

    [Header("BGM")]
    [SerializeField] private AudioSource bgm;

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "Level_opening"; // 실제 씬 명으로 변경

    private void Awake()
    {
        if (panelAbout != null) panelAbout.SetActive(false);

        if (bgm != null)
        {
            bgm.loop = true;
            if (!bgm.isPlaying) bgm.Play();
        }
    }

    public void OnClickStartGame()
    {
        if (!string.IsNullOrEmpty(gameSceneName))
            SceneManager.LoadScene(gameSceneName);
    }

    public void OnClickOpenAbout()
    {
        if (panelAbout != null) panelAbout.SetActive(true);
    }

    public void OnClickCloseAbout()
    {
        if (panelAbout != null) panelAbout.SetActive(false);
    }

    public void SetAbout(string title, string body)
    {
        if (aboutTitle) aboutTitle.text = title;
        if (aboutBody) aboutBody.text = body;
    }
}
