using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class MenuController : MonoBehaviour
{
    [Header("Scene khi bấm Start")]
    [SerializeField] private string firstSceneName = "IntroScene";

    [Header("Kéo nút Continue vào đây (nếu để trống sẽ tự tìm theo tên 'Continue')")]
    [SerializeField] private GameObject continueButton;

    // đường dẫn file save giống với SceneManagement
    private string savePath => Path.Combine(Application.persistentDataPath, "save.json");

    private void Start()
    {
        // Tự tìm nút Continue nếu chưa gán
        if (continueButton == null)
        {
            var go = GameObject.Find("Continue");
            if (go != null) continueButton = go;
        }

        RefreshContinueVisibility();
    }

    private void RefreshContinueVisibility()
    {
        if (continueButton != null)
            continueButton.SetActive(File.Exists(savePath));
    }

    // ================== BUTTONS ==================

    public void StartGame()
    {
        try
        {
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
                Debug.Log($"[MenuController] Deleted old save: {savePath}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MenuController] Delete save failed: {e.Message}");
        }

        RefreshContinueVisibility();
        Time.timeScale = 1f;
        SceneManager.LoadScene(firstSceneName);
    }

    public void ContinueGame()
    {
        if (!File.Exists(savePath))
        {
            Debug.LogWarning("[MenuController] No save file to continue.");
            RefreshContinueVisibility();
            return;
        }

        // ✅ Gọi hệ thống LoadGame chính thức để khôi phục mọi dữ liệu
        Debug.Log("[MenuController] Continue game using SceneManagement.");
        Time.timeScale = 1f;

        if (SceneManagement.Instance != null)
        {
            SceneManagement.Instance.LoadGame(null);
        }
        else
        {
            Debug.LogError("[MenuController] SceneManagement instance not found!");
        }
    }

    public void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
