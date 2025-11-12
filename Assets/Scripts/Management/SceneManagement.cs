using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System;
using System.Collections;

public class SceneManagement : Singleton<SceneManagement>
{
    public static event Action<bool> SaveStateChanged;

    private string sceneTransitionName;
    private HashSet<string> clearedScenes = new HashSet<string>();
    public string CurrentSceneName { get; set; }
    public string SceneTransitionName => sceneTransitionName;

    public void SetTransitionName(string sceneTransitionName)
    {
        this.sceneTransitionName = sceneTransitionName;
    }

    // ===================== Scene Clear =====================
    public void MarkSceneCleared(string sceneName)
    {
        if (!clearedScenes.Contains(sceneName))
            clearedScenes.Add(sceneName);

        // ← CHANGED: đảm bảo CurrentSceneName phản ánh scene vừa clear (dùng cho kiểm tra sau này)
        CurrentSceneName = sceneName;
    }

    public bool IsSceneCleared(string sceneName) => clearedScenes.Contains(sceneName);
    public bool IsCurrentSceneCleared() => IsSceneCleared(CurrentSceneName);
    public void ResetClearedScenes() => clearedScenes.Clear();

    // ====================== SAVE / LOAD ======================
    [System.Serializable]
    public class SaveData
    {
        public string sceneName;
        public Vector3 playerPosition;
        public int playerHealth;
        public int gold;
        public string currentWeapon;
        public int currentWaveIndex;
        public List<string> clearedScenes;
    }

    private string savePath => Path.Combine(Application.persistentDataPath, "save.json");

    public void SaveGame(PlayerController player)
    {
        if (player == null)
        {
            return;
        }

        try
        {
            var dir = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            int waveIndex = 0;
            var spawner = UnityEngine.Object.FindFirstObjectByType<EnemyWaveSpawner>();
            if (spawner != null)
                waveIndex = spawner.CurrentWaveIndex;

            SaveData data = new SaveData
            {
                sceneName = SceneManager.GetActiveScene().name,
                playerPosition = player.transform.position,
                playerHealth = PlayerHealth.Instance != null ? PlayerHealth.Instance.CurrentHealth : 0,
                gold = EconomyManager.Instance != null ? EconomyManager.Instance.GetGold() : 0,
                currentWeapon = ActiveWeapon.Instance != null ? ActiveWeapon.Instance.CurrentWeaponName : "",
                currentWaveIndex = waveIndex,
                clearedScenes = new List<string>(clearedScenes)
            };

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(savePath, json);
            SaveStateChanged?.Invoke(true);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SceneManagement] Save failed: {e.Message}");
        }
    }

    // ← NEW: lưu tạm data khi chờ load scene
    private SaveData pendingLoadData;

    public void LoadGame(PlayerController player)
    {
        if (!File.Exists(savePath))
        {
            return;
        }

        string json = File.ReadAllText(savePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        clearedScenes = new HashSet<string>(data.clearedScenes ?? new List<string>());

        pendingLoadData = data;

        if (SceneManager.GetActiveScene().name != data.sceneName)
        {
            SceneManager.LoadScene(data.sceneName);
        }

        // dùng named handler để dễ unsubscribe sau này
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // ← NEW: named handler, sẽ unsubscribe chính nó khi được gọi
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        Instance.CurrentSceneName = pendingLoadData.sceneName;
        Instance.StartCoroutine(Instance.RestoreAfterSceneLoad(pendingLoadData));
        pendingLoadData = null;
    }

    private IEnumerator RestoreAfterSceneLoad(SaveData data)
    {
        // chờ một frame để scene init xong
        yield return null;

        // chờ PlayerController xuất hiện (timeout 2s)
        PlayerController player = null;
        float t = 0f;
        while (player == null && t < 2f)
        {
            player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                yield return null;
                t += Time.unscaledDeltaTime;
            }
        }

        if (player == null)
        {
            Debug.LogError("[SceneManagement] Player not found after load!");
            yield break;
        }

        // Reset Rigidbody và velocity để tránh bị “bắn”
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Use linearVelocity (new API) instead of obsolete velocity
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // Luôn dùng vị trí lưu trong save (không cần SpawnPoint)
        player.transform.position = data.playerPosition;

        // Restore HP
        if (PlayerHealth.Instance != null)
            PlayerHealth.Instance.SetHealth(data.playerHealth);

        // --- CHANGED: chờ EconomyManager tồn tại trước khi set gold ---
        float wait = 0f;
        while (EconomyManager.Instance == null && wait < 2f)
        {
            yield return null;
            wait += Time.unscaledDeltaTime;
        }

        if (EconomyManager.Instance != null)
        {
            // thêm 1 frame để UI components có time khởi tạo
            yield return null;
            EconomyManager.Instance.SetGold(data.gold);

            // Thông báo cho listeners (nếu UI bind sau khi scene load) để rebind
            SaveStateChanged?.Invoke(true);;
        }
        else
        {
            Debug.LogWarning("[SceneManagement] EconomyManager not available to restore gold.");
        }

        ActiveWeapon.Instance?.EquipWeaponByName(data.currentWeapon);

        // Restore Wave và UI — chờ spawner/ UI tồn tại, có timeout
        Instance.StartCoroutine(RestoreWaveAndUI(data.sceneName, data.currentWaveIndex));
    }

    private IEnumerator RestoreWaveAndUI(string sceneName, int waveIndex)
    {
        // chờ tới khi có spawner hoặc timeout (2s)
        EnemyWaveSpawner wave = null;
        float t = 0f;
        while (wave == null && t < 2f)
        {
            wave = UnityEngine.Object.FindFirstObjectByType<EnemyWaveSpawner>();
            if (wave == null)
            {
                yield return new WaitForSecondsRealtime(0.1f);
                t += 0.1f;
            }
        }

        string activeScene = SceneManager.GetActiveScene().name;

        if (wave != null)
        {
            if (!clearedScenes.Contains(activeScene))
            {
                wave.ResetSpawnerState();
                wave.LoadSceneWave(activeScene, waveIndex);
            }
            else
            {
                Debug.Log("[SceneManagement] Scene already cleared → skip spawning waves");
            }
        }
        else
        {
            Debug.LogWarning("[SceneManagement] WaveSpawner not found when restoring waves.");
        }

        // chờ và rebind WaveUI
        WaveUI waveUI = null;
        t = 0f;
        while (waveUI == null && t < 2f)
        {
            waveUI = UnityEngine.Object.FindFirstObjectByType<WaveUI>();
            if (waveUI == null)
            {
                yield return new WaitForSecondsRealtime(0.1f);
                t += 0.1f;
            }
        }

        if (waveUI != null)
        {
            waveUI.ForceRebindSpawner();
        }
        else
        {
            Debug.LogWarning("[SceneManagement] WaveUI not found to rebind.");
        }
    }

    public void DeleteSave()
    {
        try
        {
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
                SaveStateChanged?.Invoke(false);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SceneManagement] Delete save failed: {e.Message}");
        }
    }

    public bool HasSave() => File.Exists(savePath);

    // ← NEW: reset toàn bộ trạng thái dùng khi bắt đầu game mới
    public void ResetForNewGame()
    {
        try
        {
            // cancel any pending load handler
            SceneManager.sceneLoaded -= OnSceneLoaded;
            pendingLoadData = null;

            // clear tracked scenes and transition name
            ResetClearedScenes();
            CurrentSceneName = "";
            sceneTransitionName = "";

            // stop coroutines (nếu có)
            try { StopAllCoroutines(); } catch { }

            // delete save file and notify listeners
            DeleteSave();

            // Reset GemManager (gem UI sẽ được cập nhật qua event)
            try { GemManager.Instance?.SetGems(0); } catch { }

            // --- NEW: destroy any persistent Player objects left from previous runs ---
            try
            {
                var players = GameObject.FindGameObjectsWithTag("Player");
                foreach (var p in players)
                {
                    // persistent objects live in scene named "DontDestroyOnLoad"
                    if (p != null && p.scene.name == "DontDestroyOnLoad")
                    {
                        Debug.Log($"[SceneManagement] Destroying persistent Player object: {p.name}");
                        UnityEngine.Object.DestroyImmediate(p);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SceneManagement] failed to cleanup persistent Player: {ex.Message}");
            }

            // notify subscribers explicitly that there's no save
            SaveStateChanged?.Invoke(false);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SceneManagement] ResetForNewGame failed: {e.Message}");
        }
    }
}
