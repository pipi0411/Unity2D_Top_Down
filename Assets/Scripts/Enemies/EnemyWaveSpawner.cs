using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[System.Serializable]
public class Wave
{
    public string waveName;
    public List<GameObject> enemyPrefabs;
    public int enemyCount = 5;
    public float spawnDelay = 1f;

    [Header("UI Settings")]
    public Color waveColor = Color.white;
    public bool isBossWave = false;
}

public class EnemyWaveSpawner : MonoBehaviour
{
    [SerializeField] private List<Wave> waves;
    [SerializeField] private float timeBetweenWaves = 5f;

    private int currentWaveIndex = 0;
    private int enemiesAlive = 0;

    public delegate void WaveEvent(int currentWave, int totalWaves, Color color, bool isBoss);
    public event WaveEvent OnWaveStarted;

    public int CurrentWaveIndex => currentWaveIndex;
    public int TotalWaves => waves.Count;
    private bool allWavesCompleted = false;
    public bool AllWavesCompleted => allWavesCompleted;

    private void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        // 🔑 Nếu scene đã clear → không spawn lại quái nữa
        if (SceneManagement.Instance.IsSceneCleared(sceneName))
        {
            Debug.Log($"✅ Scene {sceneName} đã clear → không spawn quái nữa");
            allWavesCompleted = true;
            return;
        }

        Debug.Log($"[Spawner] Scene {sceneName} chưa clear → bắt đầu spawn waves");
        StartCoroutine(SpawnWaveRoutine());
    }

    private IEnumerator SpawnWaveRoutine()
    {
        while (currentWaveIndex < waves.Count)
        {
            Wave wave = waves[currentWaveIndex];
            Debug.Log($"[Spawner] Bắt đầu wave {currentWaveIndex + 1}, enemyCount = {wave.enemyCount}, isBoss = {wave.isBossWave}");
            // Gửi event cho UI
            OnWaveStarted?.Invoke(currentWaveIndex + 1, waves.Count, wave.waveColor, wave.isBossWave);

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

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health != null)
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
}
