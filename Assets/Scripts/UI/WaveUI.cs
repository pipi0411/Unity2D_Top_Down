// Scripts/WaveUI.cs
using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class WaveUI : MonoBehaviour
{
    [SerializeField] private EnemyWaveSpawner spawner;
    [SerializeField] private TMP_Text waveText; 
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float displayTime = 2f;
    [SerializeField] private float popScale = 1.3f;
    [SerializeField] private float popDuration = 0.3f;

    private CanvasGroup canvasGroup;
    private Vector3 defaultScale;

    private void Awake()
    {
        canvasGroup = waveText.gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = waveText.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        defaultScale = waveText.transform.localScale;
    }
    private void Start()
    {
        Debug.Log($"[WaveUI] scene: {SceneManager.GetActiveScene().name}, " +
              $"waveText = {(waveText == null ? "NULL" : waveText.name)}");
        // 🔑 Nếu scene đã clear => ẩn UI luôn
        string sceneName = SceneManager.GetActiveScene().name;
        if (SceneManagement.Instance.IsSceneCleared(sceneName))
        {
            waveText.gameObject.SetActive(false);
            return;
        }
        RefreshUI(); // Thêm dòng này để khi scene load lại sẽ hiện wave hiện tại
    }

    private void OnEnable()
    {
        if (spawner != null)
            spawner.OnWaveStarted += ShowWaveText;
    }

    private void OnDisable()
    {
        if (spawner != null)
            spawner.OnWaveStarted -= ShowWaveText;
    }

    private void ShowWaveText(int currentWave, int totalWaves, Color color, bool isBoss)
    {
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

    // ✅ Gọi khi Player hồi sinh để update wave hiện tại
    public void RefreshUI()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (SceneManagement.Instance.IsSceneCleared(sceneName))
        {
            waveText.gameObject.SetActive(false);
            return;
        }
        if (spawner == null) return;

        Wave currentWave = spawner.GetCurrentWave();
        if (currentWave == null) return;

        ShowWaveText(spawner.CurrentWaveIndex + 1,
                     spawner.TotalWaves,
                     currentWave.waveColor,
                     currentWave.isBossWave);
    }
}
