using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class WaveUI : MonoBehaviour
{
    [SerializeField] private EnemyWaveSpawner spawner;   // Spawner trong scene
    [SerializeField] private TMP_Text waveText;          // Text để hiển thị wave
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float displayTime = 2f;
    [SerializeField] private float popScale = 1.3f;
    [SerializeField] private float popDuration = 0.3f;

    private CanvasGroup canvasGroup;
    private Vector3 defaultScale;

    private void Awake()
    {
        Debug.Log($"[WaveUI] Awake() in scene {gameObject.scene.name}, object = {gameObject.name}");

        // Auto-assign waveText nếu quên kéo Inspector
        if (waveText == null)
        {
            waveText = GetComponentInChildren<TMP_Text>();
            Debug.LogWarning($"[WaveUI] Auto-assign waveText = {(waveText != null ? waveText.name : "NULL")}");
        }

        if (waveText != null)
        {
            canvasGroup = waveText.gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = waveText.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            defaultScale = waveText.transform.localScale;
        }
    }

    private void Start()
    {
        Debug.Log($"[WaveUI] Start() in scene: {SceneManager.GetActiveScene().name}");
        StartCoroutine(InitDelayed());
    }

    private IEnumerator InitDelayed()
    {
        Debug.Log("[WaveUI] InitDelayed() bắt đầu chờ spawner...");
        yield return new WaitUntil(() => FindFirstObjectByType<EnemyWaveSpawner>() != null);

        if (spawner == null)
        {
            spawner = FindFirstObjectByType<EnemyWaveSpawner>();
            Debug.Log($"[WaveUI] Auto-assign spawner = {(spawner != null ? spawner.name : "NULL")}");
        }

        string sceneName = SceneManagement.Instance.CurrentSceneName;
        bool cleared = SceneManagement.Instance != null && SceneManagement.Instance.IsSceneCleared(sceneName);
        Debug.Log($"[WaveUI] Kiểm tra scene {sceneName}, cleared = {cleared}");

        // Nếu scene đã clear → hiện thông báo thay vì ẩn hoàn toàn
        if (cleared)
        {
            if (waveText != null)
            {
                waveText.gameObject.SetActive(true);
                waveText.text = "✅ All waves cleared!";
                waveText.color = Color.green;
                canvasGroup.alpha = 1f;
            }
            yield break;
        }

        // 🔑 Đăng ký sự kiện ngay tại đây
        if (spawner != null)
        {
            spawner.OnWaveStarted -= ShowWaveText; // tránh đăng ký trùng
            spawner.OnWaveStarted += ShowWaveText;
            Debug.Log("[WaveUI] Đã đăng ký OnWaveStarted event với spawner.");
        }
        else
        {
            Debug.LogError("[WaveUI] InitDelayed() vẫn không tìm thấy spawner!");
        }
        RefreshUI();
    }

    private void OnEnable()
    {
        if (spawner != null)
        {
            spawner.OnWaveStarted += ShowWaveText;
            Debug.Log("[WaveUI] OnEnable: Đã đăng ký OnWaveStarted.");
        }
    }

    private void OnDisable()
    {
        if (spawner != null)
        {
            spawner.OnWaveStarted -= ShowWaveText;
            Debug.Log("[WaveUI] OnDisable: Hủy đăng ký OnWaveStarted.");
        }
    }

    private void ShowWaveText(int currentWave, int totalWaves, Color color, bool isBoss)
    {
        Debug.Log($"[WaveUI] ShowWaveText() → Wave {currentWave}/{totalWaves}, Boss = {isBoss}");

        if (waveText == null) return;

        waveText.text = isBoss
            ? $"⚔️ Boss Wave {currentWave} / {totalWaves}"
            : $"Wave {currentWave} / {totalWaves}";

        waveText.color = color;

        StopAllCoroutines();
        StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        yield return StartCoroutine(PopAndFadeIn());
        yield return new WaitForSeconds(displayTime);
        yield return StartCoroutine(FadeCanvasGroup(1f, 0f, fadeDuration));
    }

    private IEnumerator PopAndFadeIn()
    {
        float elapsed = 0f;
        canvasGroup.alpha = 0f;
        waveText.transform.localScale = defaultScale;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;

            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            float scale = Mathf.Lerp(1f, popScale, t);
            waveText.transform.localScale = defaultScale * scale;

            yield return null;
        }

        waveText.transform.localScale = defaultScale;
        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeCanvasGroup(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    // ✅ Cập nhật UI khi player revive hoặc scene load
    public void RefreshUI()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        bool cleared = SceneManagement.Instance != null && SceneManagement.Instance.IsSceneCleared(sceneName);
        Debug.Log($"[WaveUI] RefreshUI() gọi trong scene {sceneName}, cleared = {cleared}");

        if (cleared)
        {
            if (waveText != null)
            {
                waveText.gameObject.SetActive(true);
                waveText.text = "✅ All waves cleared!";
                waveText.color = Color.green;
                canvasGroup.alpha = 1f;
            }
            return;
        }

        // 🔑 Nếu spawner null → tìm lại
        if (spawner == null)
        {
            spawner = FindFirstObjectByType<EnemyWaveSpawner>();
            Debug.Log($"[WaveUI] RefreshUI() auto-assign lại spawner = {(spawner != null ? spawner.name : "NULL")}");

            if (spawner == null)
            {
                Debug.LogWarning("[WaveUI] RefreshUI() vẫn không tìm thấy spawner!");
                return;
            }
        }

        Wave currentWave = spawner.GetCurrentWave();
        if (currentWave == null)
        {
            Debug.LogWarning("[WaveUI] RefreshUI() gọi nhưng currentWave NULL!");
            return;
        }

        Debug.Log($"[WaveUI] RefreshUI() hiển thị wave {spawner.CurrentWaveIndex + 1}/{spawner.TotalWaves}");
        ShowWaveText(spawner.CurrentWaveIndex + 1,
                     spawner.TotalWaves,
                     currentWave.waveColor,
                     currentWave.isBossWave);
    }
}
