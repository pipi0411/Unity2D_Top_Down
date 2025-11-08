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
    private bool isStartingNewGame = false;

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

    private void OnDestroy()
    {
        // cleanup subscription nếu object menu bị hủy
        SceneManager.sceneLoaded -= OnSceneLoaded;
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
            // delete save file immediately so start is always "clean"
            if (File.Exists(savePath))
                File.Delete(savePath);

            // mark flag so OnSceneLoaded knows to initialize for a fresh run
            isStartingNewGame = true;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            // Try to reset SceneManagement if present (do not destroy its GameObject here)
            if (SceneManagement.Instance != null)
            {
                SceneManagement.Instance.ResetForNewGame();
                // don't destroy the manager object immediately — it may be needed across scenes
            }

            // Reset economy / other managers that have Instance singletons
            if (EconomyManager.Instance != null)
                EconomyManager.Instance.SetGold(0);

            // Reset ActiveWeapon to default (may be null)
            ActiveWeapon.Instance?.EquipWeaponByName("");

            // Ensure time scale is normal and load the first scene
            Time.timeScale = 1f;
            SceneManager.LoadScene(firstSceneName, LoadSceneMode.Single);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MenuController] StartGame failed: {e.Message}");
            // try best-effort to still load scene
            Time.timeScale = 1f;
            SceneManager.LoadScene(firstSceneName, LoadSceneMode.Single);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!isStartingNewGame) return;
        isStartingNewGame = false;
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // WaitOneFrame pattern could be used if some objects initialize in Awake/A Start later.
        // Here we attempt to find the player and reset health/give default equip.
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            var ph = playerObj.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                // Try to set to a sensible starting max or a property on the PlayerHealth itself
                int maxHealth = 100;
                var type = ph.GetType();
                var prop = type.GetProperty("MaxHealth") ?? type.GetProperty("maxHealth");
                if (prop != null)
                {
                    var val = prop.GetValue(ph);
                    if (val is int) maxHealth = (int)val;
                    else if (val is float) maxHealth = Mathf.RoundToInt((float)val);
                }
                // If PlayerHealth exposes SetHealth method
                var setMethod = type.GetMethod("SetHealth");
                if (setMethod != null)
                    setMethod.Invoke(ph, new object[] { maxHealth });
                else
                    // fallback to common SetHealth signature on instance
                    ph.SetHealth(maxHealth);
            }
        }
        else
        {
            Debug.LogWarning("[MenuController] Player not found on scene load. Ensure the Player prefab with tag 'Player' exists in the first scene.");
        }

        // Ensure UI Continue is hidden after starting fresh
        RefreshContinueVisibility();
    }

    public void ContinueGame()
    {
        if (!File.Exists(savePath))
        {
            RefreshContinueVisibility();
            return;
        }

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
