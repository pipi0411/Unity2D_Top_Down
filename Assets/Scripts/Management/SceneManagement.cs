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
            Debug.LogWarning("[SceneManagement] Player not found – cannot save.");
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

            Debug.Log($"[SceneManagement] Game Saved ({data.sceneName}) | wave {data.currentWaveIndex}");
            SaveStateChanged?.Invoke(true);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SceneManagement] Save failed: {e.Message}");
        }
    }

    public void LoadGame(PlayerController player)
    {
        if (!File.Exists(savePath))
        {
            Debug.LogWarning("[SceneManagement] No save file found!");
            return;
        }

        string json = File.ReadAllText(savePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        clearedScenes = new HashSet<string>(data.clearedScenes ?? new List<string>());
        Debug.Log($"[SceneManagement] Loading game: {data.sceneName}");

        if (SceneManager.GetActiveScene().name != data.sceneName)
        {
            SceneManager.LoadScene(data.sceneName);
        }

        SceneManager.sceneLoaded += (scene, mode) =>
        {
            Instance.CurrentSceneName = data.sceneName;
            Instance.StartCoroutine(Instance.RestoreAfterSceneLoad(data));
        };
    }

    public IEnumerator RestoreAfterSceneLoad(SaveData data)
    {
        yield return new WaitForSecondsRealtime(0.2f);

        PlayerController newPlayer = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (newPlayer != null)
        {
            newPlayer.transform.position = data.playerPosition;
            if (PlayerHealth.Instance != null)
                PlayerHealth.Instance.SetHealth(data.playerHealth);
        }

        EconomyManager.Instance?.SetGold(data.gold);
        ActiveWeapon.Instance?.EquipWeaponByName(data.currentWeapon);

        Instance.StartCoroutine(RestoreWaveAndUI(data.sceneName, data.currentWaveIndex));
    }

    private IEnumerator RestoreWaveAndUI(string sceneName, int waveIndex)
    {
        yield return new WaitForSecondsRealtime(0.3f);

        var wave = UnityEngine.Object.FindFirstObjectByType<EnemyWaveSpawner>();
        if (wave != null)
        {
            if (!clearedScenes.Contains(sceneName))
            {
                wave.ResetSpawnerState();
                wave.LoadSceneWave(sceneName, waveIndex);
                Debug.Log($"[SceneManagement] ✅ WaveSpawner restored at wave {waveIndex + 1}");
            }
            else
            {
                Debug.Log("[SceneManagement] Scene already cleared → skip spawning waves");
            }
        }

        var waveUI = UnityEngine.Object.FindFirstObjectByType<WaveUI>();
        if (waveUI != null)
        {
            waveUI.ForceRebindSpawner();
            Debug.Log("[SceneManagement] ✅ WaveUI rebound to spawner after continue.");
        }
    }

    public void DeleteSave()
    {
        try
        {
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
                Debug.Log("[SceneManagement] Save file deleted.");
                SaveStateChanged?.Invoke(false);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SceneManagement] Delete save failed: {e.Message}");
        }
    }

    public bool HasSave() => File.Exists(savePath);
}
