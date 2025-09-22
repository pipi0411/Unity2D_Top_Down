using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class AreaExit : MonoBehaviour
{
    [SerializeField] private string sceneToLoad;
    [SerializeField] private string sceneTransitionName;
    [SerializeField] private TMP_Text warningText; // Kéo text từ Canvas vào đây
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float displayTime = 2f;

    private CanvasGroup canvasGroup;

    private float waitToLoadTime = 1f;

    private void Start()
    {
        if (warningText != null)
        {
            canvasGroup = warningText.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = warningText.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            warningText.gameObject.SetActive(true);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.GetComponent<PlayerController>() == null) return;

        EnemyWaveSpawner spawner = FindFirstObjectByType<EnemyWaveSpawner>();
        if (spawner != null && !spawner.AllWavesCompleted)
        {
            ShowWarning("Clear all waves to proceed!");
            return;
        }

        SceneManagement.Instance.SetTransitionName(sceneTransitionName);
        UIFade.Instance.FadeToBlack();
        StartCoroutine(LoadSceneRoutine());
    }

    private IEnumerator LoadSceneRoutine()
    {
        float timer = waitToLoadTime;
        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            yield return null;
        }
        SceneManager.LoadScene(sceneToLoad);
    }

    private void ShowWarning(string message)
    {
        if (warningText == null) return;

        warningText.text = message;
        StopAllCoroutines();
        StartCoroutine(WarningRoutine());
    }

    private IEnumerator WarningRoutine()
    {
        yield return StartCoroutine(FadeCanvasGroup(0f, 1f, fadeDuration));
        yield return new WaitForSeconds(displayTime);
        yield return StartCoroutine(FadeCanvasGroup(1f, 0f, fadeDuration));
    }

    private IEnumerator FadeCanvasGroup(float from, float to, float duration)
    {
        float elapsed = 0f;
        canvasGroup.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
