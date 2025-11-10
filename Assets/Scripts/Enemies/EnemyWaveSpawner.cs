using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnemyWaveSpawner : Singleton<EnemyWaveSpawner>
{
    [System.Serializable]
    private class SceneWave
    {
        // Luôn có 1 field UnityEngine.Object để layout serialized giống nhau giữa Editor và Build
        [SerializeField] private UnityEngine.Object scene;
        [SerializeField] private string sceneName;
        public List<Wave> waves;

        public string SceneName
        {
            get
            {
                // ưu tiên sceneName nếu có, fallback dùng scene.name nếu được gán (editor sẽ gán SceneAsset vào trường này)
                if (!string.IsNullOrEmpty(sceneName)) return sceneName;
                return scene != null ? scene.name : "NULL";
            }
        }

#if UNITY_EDITOR
        // chỉ dùng trong Editor để dễ chọn SceneAsset và đồng bộ lại sceneName
        public SceneAsset SceneAsset => scene as SceneAsset;
        public void SyncSceneNameFromEditor()
        {
            if (scene != null)
            {
                var sa = scene as SceneAsset;
                if (sa != null) sceneName = sa.name;
            }
        }
#endif
    }

    [Header("Per Scene Waves")]
    [SerializeField] private List<SceneWave> sceneWaves;

    [Header("Timing")]
    [SerializeField] private float timeBetweenWaves = 5f;

    [Header("Intro / Boss Clear")]
    [Tooltip("Nếu true sẽ load introSceneName khi boss chết và scene trống")]
    [SerializeField] private bool loadIntroOnBossDeath = false;
    [Tooltip("Tên scene intro (phải có trong Build Settings)")]
    [SerializeField] private string introSceneName = "";
    [Tooltip("Delay trước khi load intro (giây)")]
    [SerializeField] private float introLoadDelay = 0.5f;

    private List<Wave> waves;
    private int currentLevelIndex = 0;
    private int currentWaveIndex = 0;

    // Bộ đếm tham khảo – sẽ được đồng bộ lại bằng recount khi cần
    private int enemiesAlive = 0;

    private bool allWavesCompleted = false;
    private bool spawnedAnyEnemies = false;
    private string currentSceneName;
    private Coroutine spawnRoutineCo;

    // Theo dõi cả enemy thường (EnemyHealth) và Boss (BossHealthManager)
    private readonly List<EnemyHealth> trackedEnemies = new List<EnemyHealth>();
    private readonly List<BossHealthManager> trackedBosses = new List<BossHealthManager>();

    public delegate void WaveEvent(int currentWave, int totalWaves, Color color, bool isBoss);
    public event WaveEvent OnWaveStarted;

    public int CurrentWaveIndex => currentWaveIndex;
    public int TotalWaves => waves != null ? waves.Count : 0;
    public bool AllWavesCompleted => allWavesCompleted;

    // ─────────────────────────────────────────────────────────────────────────────
    private new void Awake()
    {
        base.Awake();

        var all = FindObjectsByType<EnemyWaveSpawner>(FindObjectsSortMode.None);
        if (all.Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        currentSceneName = SceneManager.GetActiveScene().name;
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
        SceneManager.sceneLoaded -= OnSceneLoaded;
        base.OnDestroy();
    }

    private void Start()
    {
        StartCoroutine(InitAfterLoad());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (this == null || !isActiveAndEnabled) return;
        currentSceneName = scene.name;
        StartCoroutine(InitAfterLoad());
    }

    private IEnumerator InitAfterLoad()
    {
        // Cho các hệ thống khác init trước
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

        // Track mọi enemy/boss đang có trong scene (kể cả inactive)
        RegisterExistingEnemies();
        RegisterExistingBosses();

        LoadSceneWave(activeScene);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Public API

    public void LoadSceneWave(string sceneName = null, int startWave = 0)
    {
        if (string.IsNullOrEmpty(sceneName))
            sceneName = SceneManager.GetActiveScene().name;

        currentSceneName = sceneName;

        int index = sceneWaves.FindIndex(sw => sw.SceneName == sceneName);
        if (index == -1)
        {
            Debug.LogWarning($"[WaveSpawner] No wave data found for scene: {sceneName}");
            return;
        }

        currentLevelIndex = index;
        waves = sceneWaves[currentLevelIndex].waves;

        enemiesAlive = CountAliveTracked();  // đồng bộ số sống hiện tại
        allWavesCompleted = false;

        SpawnLevelWaves(startWave);
    }

    public void ResetSpawnerState()
    {
        if (spawnRoutineCo != null) StopCoroutine(spawnRoutineCo);
        spawnRoutineCo = null;

        // Unsubscribe & clear lists
        for (int i = trackedEnemies.Count - 1; i >= 0; i--)
        {
            var eh = trackedEnemies[i];
            if (eh != null) eh.OnEnemyDied -= HandleEnemyDeath;
            trackedEnemies.RemoveAt(i);
        }
        for (int i = trackedBosses.Count - 1; i >= 0; i--)
        {
            var bh = trackedBosses[i];
            if (bh != null) bh.OnBossDied -= HandleBossDeath;
            trackedBosses.RemoveAt(i);
        }

        enemiesAlive = 0;
        allWavesCompleted = false;
        waves = null;
        spawnedAnyEnemies = false;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Spawning loop

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

        if (spawnRoutineCo != null) StopCoroutine(spawnRoutineCo);
        spawnRoutineCo = StartCoroutine(SpawnWaveRoutine());
    }

    private IEnumerator SpawnWaveRoutine()
    {
        while (currentWaveIndex < waves.Count)
        {
            // Nếu scene đổi trong lúc spawn → dừng
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
                spawnedAnyEnemies = true;
                SpawnEnemy(wave);
                yield return new WaitForSeconds(wave.spawnDelay);
            }

            // ── CHỜ DỌN XONG WAVE: chống false-zero khi revive/inactive ──
            while (true)
            {
                enemiesAlive = Mathf.Max(enemiesAlive, 0);

                if (enemiesAlive > 0)
                {
                    yield return null;
                    continue;
                }

                // Bộ đếm về 0 → recount thực tế (Enemy + Boss; include inactive)
                int aliveNow = CountAliveTracked();
                if (aliveNow > 0)
                {
                    enemiesAlive = aliveNow; // đồng bộ lại
                    yield return null;
                    continue;
                }

                // Thật sự hết
                break;
            }

            yield return new WaitForSeconds(timeBetweenWaves);
            currentWaveIndex++;
        }

        allWavesCompleted = true;
        spawnRoutineCo = null;

        // ── FINAL GUARD: re-register + grace 0.5s + recount lần cuối ──
        RegisterExistingEnemies();
        RegisterExistingBosses();
        int finalAlive = CountAliveTracked();
        if (finalAlive > 0)
        {
            enemiesAlive = finalAlive;
            allWavesCompleted = false;
            spawnRoutineCo = StartCoroutine(SpawnWaveRoutine());
            yield break;
        }

        yield return new WaitForSeconds(0.5f);
        RegisterExistingEnemies();
        RegisterExistingBosses();
        finalAlive = CountAliveTracked();
        if (finalAlive > 0)
        {
            enemiesAlive = finalAlive;
            allWavesCompleted = false;
            spawnRoutineCo = StartCoroutine(SpawnWaveRoutine());
            yield break;
        }

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
            // Track & subscribe (không cộng đếm nếu đã có trong tracked)
            if (!trackedEnemies.Contains(health))
            {
                trackedEnemies.Add(health);
                health.OnEnemyDied += HandleEnemyDeath;
            }

            enemiesAlive++;            // enemy mới spawn → chắc chắn đang sống
            spawnedAnyEnemies = true;
        }
        else if (enemy.TryGetComponent<BossHealthManager>(out var boss))
        {
            if (!trackedBosses.Contains(boss))
            {
                trackedBosses.Add(boss);
                boss.OnBossDied += HandleBossDeath;
            }

            enemiesAlive++;            // boss mới spawn (nếu có kiểu này)
            spawnedAnyEnemies = true;
        }
    }

    private void HandleEnemyDeath(GameObject enemyGO)
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
    }

    private void HandleBossDeath(BossHealthManager bh)
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        // khởi kiểm tra xem sau khi boss chết scene có thực sự trống → nếu trống và cấu hình thì load intro
        StartCoroutine(CheckSceneClearAfterBossDeath());
    }

    private IEnumerator CheckSceneClearAfterBossDeath()
    {
        // nhỏ delay để cho Die() xử lý/hủy object xong
        yield return new WaitForSeconds(0.5f);

        // re-register + recount (giống final guard)
        RegisterExistingEnemies();
        RegisterExistingBosses();
        int finalAlive = CountAliveTracked();
        if (finalAlive > 0)
        {
            enemiesAlive = finalAlive;
            yield break;
        }

        yield return new WaitForSeconds(0.5f);

        RegisterExistingEnemies();
        RegisterExistingBosses();
        finalAlive = CountAliveTracked();
        if (finalAlive > 0)
        {
            enemiesAlive = finalAlive;
            yield break;
        }

        // nếu không còn thực thể, đánh dấu cleared và load intro nếu được bật
        if (waves != null && waves.Count > 0 && spawnedAnyEnemies)
        {
            string finishedScene = SceneManager.GetActiveScene().name;
            Debug.Log($"[WaveSpawner] All waves completed after boss death in scene {finishedScene}. Marking cleared.");
            SceneManagement.Instance?.MarkSceneCleared(finishedScene);

            if (loadIntroOnBossDeath && !string.IsNullOrEmpty(introSceneName))
            {
                Debug.Log($"[WaveSpawner] Loading intro scene '{introSceneName}' after boss death.");
                SceneManagement.Instance?.SetTransitionName(introSceneName);
                yield return new WaitForSeconds(introLoadDelay);
                SceneManager.LoadScene(introSceneName);
            }
        }
    }

    private Vector3 GetSpawnPosition()
    {
        Vector2 offset = Random.insideUnitCircle * 2f;
        return transform.position + new Vector3(offset.x, offset.y, 0f);
    }

    public Wave GetCurrentWave()
    {
        if (waves != null && currentWaveIndex < waves.Count)
            return waves[currentWaveIndex];
        return null;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Tracking & recount

    /// Lấy mọi EnemyHealth đang có trong scene (KỂ CẢ INACTIVE) để theo dõi
    private void RegisterExistingEnemies()
    {
        var scene = SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            var arr = root.GetComponentsInChildren<EnemyHealth>(true); // include inactive
            foreach (var eh in arr)
            {
                if (eh == null) continue;
                if (!trackedEnemies.Contains(eh))
                {
                    trackedEnemies.Add(eh);
                    eh.OnEnemyDied += HandleEnemyDeath;
                }
            }
        }
    }

    /// Lấy mọi BossHealthManager trong scene (KỂ CẢ INACTIVE) để theo dõi
    private void RegisterExistingBosses()
    {
        var scene = SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            var arr = root.GetComponentsInChildren<BossHealthManager>(true); // include inactive
            foreach (var bh in arr)
            {
                if (bh == null) continue;
                if (!trackedBosses.Contains(bh))
                {
                    trackedBosses.Add(bh);
                    bh.OnBossDied += HandleBossDeath;
                }
            }
        }
    }

    /// Đếm số thực thể còn sống (Enemy + Boss). 
    /// EnemyHealth: component còn tồn tại → coi là còn sống (vì chết sẽ bắn event &/hoặc bị Destroy).
    /// Boss: dựa vào BossHealthManager.IsDead.
    private int CountAliveTracked()
    {
        int alive = 0;

        // Enemy thường
        for (int i = trackedEnemies.Count - 1; i >= 0; i--)
        {
            var eh = trackedEnemies[i];
            if (eh == null)
            {
                trackedEnemies.RemoveAt(i);
                continue;
            }
            alive++; // nếu còn component → chưa bị tiêu diệt hoàn toàn
        }

        // Boss
        for (int i = trackedBosses.Count - 1; i >= 0; i--)
        {
            var bh = trackedBosses[i];
            if (bh == null)
            {
                trackedBosses.RemoveAt(i);
                continue;
            }
            if (!bh.IsDead) alive++;
        }

        return alive;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (sceneWaves == null) return;
        foreach (var sw in sceneWaves) sw?.SyncSceneNameFromEditor();
        EditorUtility.SetDirty(this);
    }
#endif
}
