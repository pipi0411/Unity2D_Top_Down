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

    // reference tới coroutine spawn để dừng an toàn (không StopAllCoroutines)
    private Coroutine spawnRoutineCo;
    // cờ để biết có thực sự spawn ít nhất 1 enemy trong level này
    private bool spawnedAnyEnemies = false;

    // tracked enemies (những EnemyHealth đã subscribe) — dùng để unsubscribe khi reset
    private readonly System.Collections.Generic.List<EnemyHealth> trackedEnemies = new System.Collections.Generic.List<EnemyHealth>();

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

        if (SceneManagement.Instance != null)
            SceneManagement.Instance.CurrentSceneName = activeScene;

        if (SceneManagement.Instance != null && SceneManagement.Instance.IsSceneCleared(activeScene))
        {
            allWavesCompleted = true;
            yield break;
        }

        ResetSpawnerState();

        // nếu trong scene đã có enemy (ví dụ boss đặt sẵn), đăng ký để spawner biết
        RegisterExistingEnemies();

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
        spawnedAnyEnemies = false;

        // dừng coroutine trước đó nếu có, rồi start mới
        if (spawnRoutineCo != null) StopCoroutine(spawnRoutineCo);
        spawnRoutineCo = StartCoroutine(SpawnWaveRoutine());
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
                spawnRoutineCo = null;
                yield break;
            }

            Wave wave = waves[currentWaveIndex];
            OnWaveStarted?.Invoke(currentWaveIndex + 1, waves.Count, wave.waveColor, wave.isBossWave);

            yield return new WaitForSeconds(2f);

            for (int i = 0; i < wave.enemyCount; i++)
            {
                // mỗi khi spawn thực sự tăng cờ và instantiate enemy (và EnemyHealth sẽ được subscribe trong SpawnEnemy)
                spawnedAnyEnemies = true;
                SpawnEnemy(wave);
                yield return new WaitForSeconds(wave.spawnDelay);
            }

            while (enemiesAlive > 0)
                yield return null;

            yield return new WaitForSeconds(timeBetweenWaves);
            currentWaveIndex++;
        }

        allWavesCompleted = true;
        spawnRoutineCo = null;
        // CHANGED: mark cleared only when we had waves and actually spawned something
        if (waves != null && waves.Count > 0 && spawnedAnyEnemies)
        {
            string finishedScene = SceneManager.GetActiveScene().name;
            Debug.Log($"[WaveSpawner] All waves completed in scene {finishedScene}. Marking cleared.");
            SceneManagement.Instance?.MarkSceneCleared(finishedScene);
        }
        else
        {
            Debug.LogWarning($"[WaveSpawner] Waves finished but no enemies were spawned (scene={SceneManager.GetActiveScene().name}). Skipping mark-cleared.");
        }
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
            // track để unsubscribe khi reset
            if (!trackedEnemies.Contains(health)) trackedEnemies.Add(health);
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
            SpawnLevelWaves(startWave);
        }
        else
        {
            Debug.LogWarning($"[WaveSpawner] No wave data found for scene: {sceneName}");
        }
    }

    public void ResetSpawnerState()
    {
        // Dừng riêng coroutine spawn (an toàn hơn)
        if (spawnRoutineCo != null) StopCoroutine(spawnRoutineCo);
        spawnRoutineCo = null;

        // Unsubscribe tất cả EnemyHealth đã đăng ký (nếu còn sống)
        for (int i = trackedEnemies.Count - 1; i >= 0; i--)
        {
            var eh = trackedEnemies[i];
            if (eh != null)
                eh.OnEnemyDied -= HandleEnemyDeath;
            trackedEnemies.RemoveAt(i);
        }

        enemiesAlive = 0;
        allWavesCompleted = false;
        waves = null;
        spawnedAnyEnemies = false;
    }

    // đăng ký các EnemyHealth hiện có trong scene (ví dụ boss đặt sẵn)
    private void RegisterExistingEnemies()
    {
        var existing = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (var eh in existing)
        {
            if (eh == null) continue;
            if (!trackedEnemies.Contains(eh))
            {
                trackedEnemies.Add(eh);
                eh.OnEnemyDied += HandleEnemyDeath;
                enemiesAlive++;
                spawnedAnyEnemies = true;
                Debug.Log($"[WaveSpawner] Registered existing enemy: {eh.name}");
            }
        }
    }
}
