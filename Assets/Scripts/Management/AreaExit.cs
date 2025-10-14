using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

#if UNITY_EDITOR
using UnityEditor; // chỉ trong Editor để chọn SceneAsset
#endif

public class AreaExit : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private SceneAsset nextScene;    // Chọn scene trong Editor
#endif
    [SerializeField] private string nextSceneName;    // Dự phòng nếu không dùng SceneAsset
    [SerializeField] private string sceneTransitionName;

    [Header("UI (optional)")]
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float displayTime = 2f;

    [Header("Gating options")]
    [SerializeField] private bool requireWavesCleared = true;   // GIỮ MẶC ĐỊNH = true (hành vi cũ)
    [SerializeField] private bool requireSceneCleared = false;  // Nếu bật: chỉ cho đi khi SceneManagement báo đã clear

    private CanvasGroup canvasGroup;

    private void Start()
    {
        // Auto-assign warningText nếu bạn quên kéo
        if (warningText == null)
        {
            var obj = GameObject.Find("WarningText");
            if (obj != null) warningText = obj.GetComponent<TMP_Text>();
        }

        if (warningText != null)
        {
            canvasGroup = warningText.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = warningText.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            warningText.gameObject.SetActive(true);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Chỉ nhận Player
        if (other.GetComponent<PlayerController>() == null && !other.CompareTag("Player")) return;

        // 1) Khóa theo "đã clear scene chưa" (tuỳ chọn)
        if (requireSceneCleared && SceneManagement.Instance != null && !SceneManagement.Instance.IsCurrentSceneCleared())
        {
            ShowWarning("Defeat the boss to proceed!");
            return;
        }

        // 2) Khóa theo waves (hành vi cũ – chỉ chặn khi bạn để tick)
        if (requireWavesCleared)
        {
            var spawner = FindFirstObjectByType<EnemyWaveSpawner>();
            if (spawner != null && !spawner.AllWavesCompleted)
            {
                ShowWarning("Clear all waves to proceed!");
                return;
            }
        }

        // Lưu TransitionName cho AreaEntrance của scene kế
        SceneManagement.Instance?.SetTransitionName(sceneTransitionName);

        // Fade rồi load
        if (fadeDuration > 0f) UIFade.Instance?.FadeToBlack();
        StartCoroutine(LoadSceneRoutine(fadeDuration > 0f ? fadeDuration : 0f));
    }

    private IEnumerator LoadSceneRoutine(float wait)
    {
        if (wait > 0f) yield return new WaitForSeconds(wait);

        string target = GetNextSceneName();
        if (string.IsNullOrEmpty(target))
        {
            Debug.LogError("[AreaExit] Next scene is not set!");
            yield break;
        }

        SceneManager.LoadScene(target, LoadSceneMode.Single);

        if (SceneManagement.Instance != null)
            SceneManagement.Instance.CurrentSceneName = target;

        // Nếu có hệ thống wave theo scene, gọi an toàn
        var wave = FindFirstObjectByType<EnemyWaveSpawner>();
        if (wave != null && wave == EnemyWaveSpawner.Instance)
            EnemyWaveSpawner.Instance.LoadSceneWave();
    }

    private string GetNextSceneName()
    {
#if UNITY_EDITOR
        if (nextScene != null) return nextScene.name;
#endif
        return nextSceneName;
    }

    // ------ UI Warning ------
    private void ShowWarning(string message)
    {
        if (warningText == null) return;
        warningText.text = message;
        StopAllCoroutines();
        StartCoroutine(WarningRoutine());
    }

    private IEnumerator WarningRoutine()
    {
        if (canvasGroup == null) yield break;

        // fade in
        yield return FadeCanvasGroup(0f, 1f, fadeDuration);

        // giữ chữ
        yield return new WaitForSeconds(displayTime);

        // fade out
        yield return FadeCanvasGroup(1f, 0f, fadeDuration);
    }

    private IEnumerator FadeCanvasGroup(float from, float to, float duration)
    {
        float t = 0f;
        canvasGroup.alpha = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
