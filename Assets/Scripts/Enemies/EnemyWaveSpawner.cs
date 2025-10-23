using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor;

public class EnemyWaveSpawner : Singleton<EnemyWaveSpawner>
{
    [System.Serializable]
    class SceneWave
    {
        public SceneAsset scene;
        public List<Wave> waves;
        public string SceneName => scene != null ? scene.name : "NULL";
    }

    [SerializeField] private List<SceneWave> sceneWaves;
    private List<Wave> waves;
    [SerializeField] private float timeBetweenWaves = 5f;

    private int currentLevelIndex = 0;
    private int currentWaveIndex = 0;
    private int enemiesAlive = 0;

    public delegate void WaveEvent(int currentWave, int totalWaves, Color color, bool isBoss);
    public event WaveEvent OnWaveStarted;

    public int CurrentWaveIndex => currentWaveIndex;
    public int TotalWaves => waves != null ? waves.Count : 0;
    private bool allWavesCompleted = false;
    public bool AllWavesCompleted => allWavesCompleted;

    private string currentSceneName;

    private new void Awake()
    {
        base.Awake();

        var existing = FindObjectsByType<EnemyWaveSpawner>(FindObjectsSortMode.None);
        if (existing.Length > 1)
        {
            Debug.Log("[WaveSpawner] Destroying duplicate instance from previous scene");
            Destroy(gameObject); // ← changed from DestroyImmediate to safe Destroy
            return;
        }

        currentSceneName = SceneManager.GetActiveScene().name;
        // NOTE: don't subscribe here — do it in OnEnable()
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private new void OnDestroy()
    {
        // extra safety
        SceneManager.sceneLoaded -= OnSceneLoaded;
        base.OnDestroy();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Guard: nếu object đã bị destroy hoặc không active thì không làm gì
        if (this == null || !isActiveAndEnabled) return;

        currentSceneName = scene.name;
        StartCoroutine(InitAfterLoad());
    }

    private void Start()
    {
        StartCoroutine(InitAfterLoad());
    }

    private IEnumerator InitAfterLoad()
    {
        yield return new WaitForSeconds(0.3f);

        string activeScene = SceneManager.GetActiveScene().name;
        currentSceneName = activeScene;
        Debug.Log($"[WaveSpawner] InitAfterLoad → Scene = {activeScene}");

        if (SceneManagement.Instance != null)
            SceneManagement.Instance.CurrentSceneName = activeScene;

        if (SceneManagement.Instance != null && SceneManagement.Instance.IsSceneCleared(activeScene))
        {
            allWavesCompleted = true;
            Debug.Log("[WaveSpawner] Scene already cleared → skip spawning.");
            yield break;
        }

        ResetSpawnerState();

        LoadSceneWave(activeScene);
    }

    private void SpawnLevelWaves(int startWave = 0)
    {
        if (waves == null || waves.Count == 0)
        {
            Debug.LogWarning($"[WaveSpawner] No waves assigned for scene: {currentSceneName}");
            return;
        }

        allWavesCompleted = false;
        currentWaveIndex = startWave;

        StartCoroutine(SpawnWaveRoutine());
    }

    private IEnumerator SpawnWaveRoutine()
    {
        while (currentWaveIndex < waves.Count)
        {
            // Nếu scene đổi trong lúc đang spawn → dừng lại
            string activeScene = SceneManager.GetActiveScene().name;
            if (activeScene != currentSceneName)
            {
                Debug.Log($"[WaveSpawner] Scene changed ({currentSceneName} → {activeScene}), stop spawning.");
                yield break;
            }

            Wave wave = waves[currentWaveIndex];
            OnWaveStarted?.Invoke(currentWaveIndex + 1, waves.Count, wave.waveColor, wave.isBossWave);

            yield return new WaitForSeconds(2f);

            for (int i = 0; i < wave.enemyCount; i++)
            {
                SpawnEnemy(wave);
                yield return new WaitForSeconds(wave.spawnDelay);
            }

            while (enemiesAlive > 0)
                yield return null;

            yield return new WaitForSeconds(timeBetweenWaves);
            currentWaveIndex++;
        }

        allWavesCompleted = true;
        // CHANGED: mark cleared using actual active scene name to avoid marking wrong scene
        string finishedScene = SceneManager.GetActiveScene().name;
        Debug.Log($"[WaveSpawner] All waves completed in {finishedScene}");
        SceneManagement.Instance?.MarkSceneCleared(finishedScene);
    }

    private void SpawnEnemy(Wave wave)
    {
        if (wave.enemyPrefabs == null || wave.enemyPrefabs.Count == 0) return;

        GameObject prefab = wave.enemyPrefabs[Random.Range(0, wave.enemyPrefabs.Count)];
        GameObject enemy = Instantiate(prefab, GetSpawnPosition(), Quaternion.identity);

        if (enemy.TryGetComponent<EnemyHealth>(out var health))
        {
            enemiesAlive++;
            health.OnEnemyDied += HandleEnemyDeath;
        }
    }

    private void HandleEnemyDeath(GameObject enemy)
    {
        enemiesAlive--;
    }

    private Vector3 GetSpawnPosition()
    {
        Vector2 offset = Random.insideUnitCircle * 2f;
        return transform.position + new Vector3(offset.x, offset.y, 0);
    }

    public Wave GetCurrentWave()
    {
        if (waves != null && currentWaveIndex < waves.Count)
            return waves[currentWaveIndex];
        return null;
    }

    public void LoadSceneWave(string sceneName = null, int startWave = 0)
    {
        if (string.IsNullOrEmpty(sceneName))
            sceneName = SceneManager.GetActiveScene().name;

        // CHANGED: giữ currentSceneName đồng bộ khi gọi explicit load
        currentSceneName = sceneName;

        int index = sceneWaves.FindIndex(sw => sw.SceneName == sceneName);
        if (index != -1)
        {
            currentLevelIndex = index;
            waves = sceneWaves[currentLevelIndex].waves;
            enemiesAlive = 0;
            allWavesCompleted = false;

            Debug.Log($"[WaveSpawner] Loaded wave data for scene {sceneName}");
            SpawnLevelWaves(startWave);
        }
        else
        {
            Debug.LogWarning($"[WaveSpawner] No wave data found for scene: {sceneName}");
        }
    }

    public void ResetSpawnerState()
    {
        StopAllCoroutines();
        enemiesAlive = 0;
        allWavesCompleted = false;
        waves = null;
    }
}
