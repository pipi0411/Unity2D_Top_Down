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
        // Auto-assign waveText nếu quên kéo Inspector
        if (waveText == null)
        {
            waveText = GetComponentInChildren<TMP_Text>();
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
        StartCoroutine(InitDelayed());
    }

    private IEnumerator InitDelayed()
    {
        yield return new WaitUntil(() => FindFirstObjectByType<EnemyWaveSpawner>() != null);

        if (spawner == null)
        {
            spawner = FindFirstObjectByType<EnemyWaveSpawner>();
        }

        string sceneName = SceneManagement.Instance.CurrentSceneName;
        bool cleared = SceneManagement.Instance != null && SceneManagement.Instance.IsSceneCleared(sceneName);

        // Nếu scene đã clear → hiện thông báo thay vì ẩn hoàn toàn
        if (cleared)
        {
            if (waveText != null)
            {
                waveText.gameObject.SetActive(true);
                waveText.text = "All waves cleared!";
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
        }
        RefreshUI();
    }

    private void OnEnable()
    {
        if (spawner != null)
        {
            spawner.OnWaveStarted += ShowWaveText;
        }
    }

    private void OnDisable()
    {
        if (spawner != null)
        {
            spawner.OnWaveStarted -= ShowWaveText;
        }
    }

    private void ShowWaveText(int currentWave, int totalWaves, Color color, bool isBoss)
    {

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

            if (spawner == null)
            {
                return;
            }
        }

        Wave currentWave = spawner.GetCurrentWave();
        if (currentWave == null)
        {
            return;
        }
        ShowWaveText(spawner.CurrentWaveIndex + 1,
                     spawner.TotalWaves,
                     currentWave.waveColor,
                     currentWave.isBossWave);
    }
}
