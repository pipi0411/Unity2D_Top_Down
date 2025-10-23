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
            // Nếu có SceneManagement, dùng API của nó để reset triệt để
            if (SceneManagement.Instance != null)
            {
                SceneManagement.Instance.ResetForNewGame();

                // Hủy luôn object singleton nếu nó là DontDestroyOnLoad để tránh giữ state cũ
                if (SceneManagement.Instance.gameObject != null)
                {
                    Destroy(SceneManagement.Instance.gameObject);
                    Debug.Log("[MenuController] Destroyed SceneManagement gameObject to ensure fresh state.");
                }
            }
            else
            {
                // Nếu không có SceneManagement thì xóa file save trực tiếp
                if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                    Debug.Log($"[MenuController] Deleted old save: {savePath}");
                }
            }

            // 🔹 Xóa hết dữ liệu trong EconomyManager
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.SetGold(0);
                Debug.Log("[MenuController] Reset EconomyManager gold.");
            }

            // (tùy ý) reset các manager khác nếu có
            if (PlayerHealth.Instance != null)
            {
                // Try to find a max-health value via common property/field names; fall back to a safe default.
                var ph = PlayerHealth.Instance;
                int maxHealth = -1;
                var type = ph.GetType();
                var prop = type.GetProperty("MaxHealth") ?? type.GetProperty("maxHealth") ?? type.GetProperty("MaxHP") ?? type.GetProperty("maxHP") ?? type.GetProperty("StartingHealth") ?? type.GetProperty("startingHealth");
                if (prop != null)
                {
                    var val = prop.GetValue(ph);
                    if (val is int) maxHealth = (int)val;
                    else if (val is float) maxHealth = Mathf.RoundToInt((float)val);
                }
                else
                {
                    var field = type.GetField("MaxHealth") ?? type.GetField("maxHealth") ?? type.GetField("MaxHP") ?? type.GetField("maxHP") ?? type.GetField("StartingHealth") ?? type.GetField("startingHealth");
                    if (field != null)
                    {
                        var val = field.GetValue(ph);
                        if (val is int) maxHealth = (int)val;
                        else if (val is float) maxHealth = Mathf.RoundToInt((float)val);
                    }
                }

                if (maxHealth <= 0)
                {
                    // Fallback default if no max value found; adjust as needed for your game.
                    maxHealth = 100;
                }

                ph.SetHealth(maxHealth);
            }
            ActiveWeapon.Instance?.EquipWeaponByName("");
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
