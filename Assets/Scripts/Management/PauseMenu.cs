using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused = false;
    [SerializeField] private GameObject pauseMenuUI;
    private bool isFinding = false;

    private void Awake() => DontDestroyOnLoad(gameObject);

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;
        GameIsPaused = false;
        StartCoroutine(FindPauseUIDelayed());
    }

    private IEnumerator FindPauseUIDelayed()
    {
        yield return new WaitForSecondsRealtime(0.2f);
        FindPauseUI();
    }

    private void FindPauseUI()
    {
        if (isFinding) return;
        isFinding = true;

        foreach (GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (obj.name == "UIPause")
            {
                pauseMenuUI = obj;
                break;
            }
        }

        if (pauseMenuUI == null)
        {
            Debug.LogWarning("[PauseMenu] UIPause not found in this scene!");
            isFinding = false;
            return;
        }

        pauseMenuUI.SetActive(false);
        SetupCanvasGroup(pauseMenuUI, true);
        AssignButtonEvents();
        isFinding = false;
    }

    private void SetupCanvasGroup(GameObject obj, bool active)
    {
        var cg = obj.GetComponent<CanvasGroup>() ?? obj.AddComponent<CanvasGroup>();
        cg.alpha = active ? 1f : 0f;
        cg.interactable = active;
        cg.blocksRaycasts = active;
    }

    private void AssignButtonEvents()
    {
        if (pauseMenuUI == null) return;
        foreach (var btn in pauseMenuUI.GetComponentsInChildren<Button>(true))
        {
            if (btn.name.Contains("Resume")) btn.onClick.AddListener(Resume);
            else if (btn.name.Contains("Save")) btn.onClick.AddListener(SaveGame);
            else if (btn.name.Contains("Exit")) btn.onClick.AddListener(ExitToMenu);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pauseMenuUI == null) FindPauseUI();
            if (pauseMenuUI == null) return;
            if (GameIsPaused) Resume(); else Pause();
        }
    }

    public void Resume()
    {
        if (pauseMenuUI == null) return;
        pauseMenuUI.SetActive(false);
        SetupCanvasGroup(pauseMenuUI, false);
        Time.timeScale = 1f;
        GameIsPaused = false;
    }

    private void Pause()
    {
        if (pauseMenuUI == null) FindPauseUI();
        if (pauseMenuUI == null) return;
        pauseMenuUI.SetActive(true);
        SetupCanvasGroup(pauseMenuUI, true);
        Time.timeScale = 0f;
        GameIsPaused = true;
    }

    public void SaveGame()
    {
        var player = FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            Debug.LogWarning("[PauseMenu] No player found to save!");
            return;
        }

        // 🔹 Dù file có bị xóa, SceneManagement.SaveGame sẽ tự tạo lại file
        SceneManagement.Instance?.SaveGame(player);
        Debug.Log("[PauseMenu] Game saved!");
    }

    public void ExitToMenu()
    {
        Time.timeScale = 1f;
        GameIsPaused = false;
        SceneManager.LoadScene("MenuScene");
    }
}
