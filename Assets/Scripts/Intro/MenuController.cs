using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class MenuController : MonoBehaviour
{
    [Header("Scene khi bấm Start")]
    [SerializeField] private string firstSceneName = "IntroScene";

    [Header("Kéo nút Continue vào đây (nếu để trống sẽ tự tìm theo tên 'Continue')")]
    [SerializeField] private GameObject continueButton;

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
            // 🔹 Xóa file save nếu có
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
                Debug.Log($"[MenuController] Deleted old save: {savePath}");
            }

            // 🔹 Reset toàn bộ dữ liệu SceneManagement nếu đang tồn tại
            if (SceneManagement.Instance != null)
            {
                SceneManagement.Instance.ResetClearedScenes();
                SceneManagement.Instance.CurrentSceneName = "";
                Debug.Log("[MenuController] Reset SceneManagement state for new game.");
            }

            // 🔹 Xóa hết dữ liệu trong EconomyManager
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.SetGold(0);
                Debug.Log("[MenuController] Reset EconomyManager gold.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MenuController] Delete or reset failed: {e.Message}");
        }

        // 🔹 Cập nhật lại UI Continue
        RefreshContinueVisibility();

        // 🔹 Load lại scene đầu tiên
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

        Debug.Log("[MenuController] Continue game using SceneManagement.");
        Time.timeScale = 1f;

        if (SceneManagement.Instance == null)
        {
            // 🔹 Nếu chưa có SceneManagement trong MenuScene → tạo tạm
            GameObject sm = new GameObject("SceneManagement");
            sm.AddComponent<SceneManagement>();
        }

        SceneManagement.Instance.LoadGame(null);
    }

    public void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
