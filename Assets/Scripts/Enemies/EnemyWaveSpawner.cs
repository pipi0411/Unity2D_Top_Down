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

    void Start()
    {
        StartCoroutine(InitAfterLoad());
    }

    private IEnumerator InitAfterLoad()
    {
        yield return new WaitForSeconds(0.25f);
        string currentScene = SceneManager.GetActiveScene().name;
        SceneManagement.Instance.CurrentSceneName = currentScene;
        LoadSceneWave(currentScene);
    }

    private void SpawnLevelWaves(int startWave = 0)
    {
        if (waves == null || waves.Count == 0)
        {
            Debug.LogWarning("[WaveSpawner] No waves assigned for this scene!");
            return;
        }

        if (SceneManagement.Instance != null && SceneManagement.Instance.IsCurrentSceneCleared())
        {
            allWavesCompleted = true;
            Debug.Log("[WaveSpawner] Scene already cleared → skip spawning.");
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
        Debug.Log("[WaveSpawner] All waves completed!");
        SceneManagement.Instance.MarkSceneCleared(SceneManager.GetActiveScene().name);
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
        StopAllCoroutines();
        enemiesAlive = 0;
        allWavesCompleted = false;
    }
}
