using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance;

    [SerializeField] private GameObject rootPanel;

    private bool _isFinding;

    private void Awake()
    {
        // Đảm bảo singleton + tồn tại xuyên scene
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (rootPanel != null)
        {
            rootPanel.SetActive(false);
            SetupCanvasGroup(rootPanel, false);
        }
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Khi vào scene mới, đảm bảo UI ẩn
        Time.timeScale = 1f;
        if (rootPanel != null)
        {
            rootPanel.SetActive(false);
            SetupCanvasGroup(rootPanel, false);
        }
        StartCoroutine(FindUIDelayed());
    }

    private IEnumerator FindUIDelayed()
    {
        yield return new WaitForSecondsRealtime(0.2f);
        FindRootPanel();
    }

    private void FindRootPanel()
    {
        if (_isFinding) return;
        _isFinding = true;

        GameObject found = null;
        foreach (var obj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (obj.name == "Canvas_GameOver")
            {
                found = obj;
                break;
            }
        }

        if (found == null)
        {
            Debug.LogWarning("[GameOverUI] Canvas_GameOver not found in this scene!");
            _isFinding = false;
            return;
        }

        rootPanel = found;
        rootPanel.SetActive(false);
        SetupCanvasGroup(rootPanel, false);
        AssignButtonEvents();

        _isFinding = false;
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
        if (rootPanel == null) return;

        foreach (var btn in rootPanel.GetComponentsInChildren<Button>(true))
        {
            // Tránh add trùng khi đổi scene
            btn.onClick.RemoveAllListeners();

            if (btn.name.Contains("Exit", System.StringComparison.OrdinalIgnoreCase) ||
                btn.name.Contains("Menu", System.StringComparison.OrdinalIgnoreCase))
            {
                btn.onClick.AddListener(ExitToMenu);
            }
        }
    }

    public void Show()
    {
        if (rootPanel == null) FindRootPanel();
        if (rootPanel == null) return;

        Time.timeScale = 0f;
        rootPanel.SetActive(true);
        SetupCanvasGroup(rootPanel, true);
    }

    public void Hide()
    {
        if (rootPanel == null) return;

        Time.timeScale = 1f;
        rootPanel.SetActive(false);
        SetupCanvasGroup(rootPanel, false);
    }

    public void ExitToMenu()
    {
        Hide();
        SceneManager.LoadScene("MenuScene");
    }
}
