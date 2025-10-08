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
    public int TotalWaves => waves.Count;
    private bool allWavesCompleted = false;
    public bool AllWavesCompleted => allWavesCompleted;

    void Start()
    {
        if (sceneWaves.Count > 0)
        {
            waves = sceneWaves[currentLevelIndex].waves;
        }
        SpawnLevelWaves();
    }

    private void SpawnLevelWaves()
    {
        // 🔑 Nếu scene đã clear → không spawn lại quái nữa
        if (SceneManagement.Instance.IsCurrentSceneCleared())
        {
            allWavesCompleted = true;
            return;
        }
        else
        {
            allWavesCompleted = false;
        }
        StartCoroutine(SpawnWaveRoutine());
    }

    private IEnumerator SpawnWaveRoutine()
    {
        while (currentWaveIndex < waves.Count)
        {
            Wave wave = waves[currentWaveIndex];
            // Gửi event cho UI
            OnWaveStarted?.Invoke(currentWaveIndex + 1, waves.Count, wave.waveColor, wave.isBossWave);
            yield return new WaitForSeconds(2f); // đợi UI hiện xong

            for (int i = 0; i < wave.enemyCount; i++)
            {
                SpawnEnemy(wave);
                yield return new WaitForSeconds(wave.spawnDelay);
            }

            // Chờ quái chết hết
            while (enemiesAlive > 0)
                yield return null;

            yield return new WaitForSeconds(timeBetweenWaves);
            currentWaveIndex++;
        }

        Debug.Log("✅ All waves completed!");
        allWavesCompleted = true;

        string sceneName = SceneManager.GetActiveScene().name;
        SceneManagement.Instance.MarkSceneCleared(sceneName); // 🔑 đánh dấu scene đã clear
    }

    private void SpawnEnemy(Wave wave)
    {
        if (wave.enemyPrefabs.Count == 0) return;

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

    // ✅ Cho WaveUI dùng khi Player revive
    public Wave GetCurrentWave()
    {
        if (currentWaveIndex < waves.Count)
            return waves[currentWaveIndex];
        return null;
    }

    // Load waves cho scene hiện tại
    public void LoadSceneWave(string sceneName = null)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            sceneName = SceneManagement.Instance.CurrentSceneName;
        }

        int index = sceneWaves.FindIndex(sw => sw.SceneName == sceneName);
        if (index != -1)
        {
            currentLevelIndex = index;
            waves = sceneWaves[currentLevelIndex].waves;
            currentWaveIndex = 0;
            enemiesAlive = 0;
            allWavesCompleted = false;
            SpawnLevelWaves();
        }
        else
        {
            Debug.LogWarning($"[EnemyWaveSpawner] Không tìm thấy waves cho scene {sceneName}!");
        }
    }
}
