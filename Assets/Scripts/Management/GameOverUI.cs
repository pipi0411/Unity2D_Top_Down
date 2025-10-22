using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance;
    [SerializeField] private GameObject rootPanel;

    private void Awake()
    {
        Instance = this;
        if (rootPanel != null)
            rootPanel.SetActive(false);
    }

    public void Show()
    {
        Time.timeScale = 0f;
        if (rootPanel != null)
            rootPanel.SetActive(true);
    }

    public void Hide()
    {
        Time.timeScale = 1f;
        if (rootPanel != null)
            rootPanel.SetActive(false);
    }

    public void Retry()
    {
        Hide();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ExitToMenu()
    {
        Hide();
        SceneManager.LoadScene("MenuScene");
    }
}
