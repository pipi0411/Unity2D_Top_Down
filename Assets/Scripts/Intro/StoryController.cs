// path: Assets/Scripts/Intro/StoryController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class StoryController : MonoBehaviour
{
    [System.Serializable]
    public class StoryPage
    {
        public Sprite image;
        [TextArea(3, 5)] public string text;
    }

    [Header("UI")]
    public Image storyImage;
    public TextMeshProUGUI storyText;

    [Header("Pages")]
    public StoryPage[] pages;

    [Header("Transition")]
    public float fadeDuration = 0.5f;

    [Header("Auto Play")]
    public bool autoPlay = true;
    public float pageHoldSeconds = 2.0f;
    [Tooltip("Thêm thời gian ở trang cuối trước khi tự qua scene.")]
    public float lastPageExtraWaitSeconds = 2.0f;
    public bool useUnscaledTime = true;

    [Header("Next Scene")]
    public bool autoLoadNextScene = true;
    public string nextSceneName = "Scene1";

    [Header("Skip Fade To Black")]
    [Tooltip("Image đen full-screen (trên cùng Canvas).")]
    public Image fadeOverlay;
    public float skipFadeDuration = 0.5f;

    [Header("Input Lock")]
    public bool lockDuringFade = true;
    public float minInputGapSeconds = 0.15f;
    [Tooltip("Image trong suốt full-screen để chặn input khi khóa (tuỳ chọn).")]
    public Image inputBlocker;

    private int currentPage = 0;
    private CanvasGroup imageGroup;
    private CanvasGroup textGroup;
    private CanvasGroup overlayGroup;   // CanvasGroup của fadeOverlay
    private CanvasGroup blockerGroup;   // CanvasGroup của inputBlocker (nếu có)

    private bool isFading = false;
    private bool isSkipping = false;

    private double nextPageAt = double.PositiveInfinity;
    private Coroutine autoLoopCo;

    private double lockedUntil = 0.0;

    void Start()
    {
        if (pages == null || pages.Length == 0)
        {
            Debug.LogWarning("[StoryController] No pages configured.");
            enabled = false;
            return;
        }

        imageGroup = storyImage != null ? storyImage.GetComponent<CanvasGroup>() : null;
        if (storyImage != null && imageGroup == null) imageGroup = storyImage.gameObject.AddComponent<CanvasGroup>();

        textGroup = storyText != null ? storyText.GetComponent<CanvasGroup>() : null;
        if (storyText != null && textGroup == null) textGroup = storyText.gameObject.AddComponent<CanvasGroup>();

        // --- Fade overlay: đảm bảo KHÔNG chặn raycast khi idle ---
        if (fadeOverlay != null)
        {
            overlayGroup = fadeOverlay.GetComponent<CanvasGroup>();
            if (overlayGroup == null) overlayGroup = fadeOverlay.gameObject.AddComponent<CanvasGroup>();
            overlayGroup.alpha = 0f;
            overlayGroup.interactable = false;
            overlayGroup.blocksRaycasts = false; // quan trọng: không chặn nút Skip
            fadeOverlay.raycastTarget = false;
        }

        // --- Input blocker: mặc định không chặn ---
        if (inputBlocker != null)
        {
            blockerGroup = inputBlocker.GetComponent<CanvasGroup>();
            if (blockerGroup == null) blockerGroup = inputBlocker.gameObject.AddComponent<CanvasGroup>();
            blockerGroup.alpha = 0f;
            blockerGroup.interactable = false;
            blockerGroup.blocksRaycasts = false; // quan trọng
            inputBlocker.raycastTarget = false;
        }

        ShowPage(0, instant: true);

        if (autoPlay)
        {
            ScheduleHoldForCurrentPage();
            autoLoopCo = StartCoroutine(AutoAdvanceLoop());
        }
    }

    void Update()
    {
        if (!IsInputLocked())
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                ManualNextPage();
                LockInput(minInputGapSeconds);
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                NextPage();
                LockInput(minInputGapSeconds);
            }
        }
    }

    // ===== Skip một chạm =====
    public void OnClickSkipIntro()
    {
        if (IsInputLocked()) return;
        StartCoroutine(SkipWithFadeToBlack());
    }

    [ContextMenu("NextPage")]
    public void NextPage()
    {
        if (IsInputLocked()) return;

        if (currentPage < pages.Length - 1)
        {
            StopAllCoroutines();
            StartCoroutine(FadeToPage(currentPage + 1, onDone: () =>
            {
                if (autoPlay)
                {
                    ScheduleHoldForCurrentPage();
                    RestartAutoLoop();
                }
                LockInput(minInputGapSeconds);
            }));
        }
        else
        {
            LoadNextSceneImmediate();
        }
    }

    private void ManualNextPage()
    {
        if (currentPage >= pages.Length - 1)
        {
            LoadNextSceneImmediate();
            return;
        }

        StopAllCoroutines();
        StartCoroutine(FadeToPage(currentPage + 1, onDone: () =>
        {
            if (autoPlay)
            {
                ScheduleHoldForCurrentPage();
                RestartAutoLoop();
            }
            LockInput(minInputGapSeconds);
        }));
    }

    private IEnumerator AutoAdvanceLoop()
    {
        while (autoPlay)
        {
            if (!isFading && !isSkipping)
            {
                if (currentPage < pages.Length - 1)
                {
                    if (Now() >= nextPageAt)
                    {
                        yield return FadeToPage(currentPage + 1);
                        ScheduleHoldForCurrentPage();
                        continue;
                    }
                }
                else
                {
                    if (autoLoadNextScene && Now() >= nextPageAt)
                    {
                        LoadNextSceneImmediate();
                        yield break;
                    }
                }
            }
            yield return null;
        }
    }

    private IEnumerator FadeToPage(int index, System.Action onDone = null)
    {
        isFading = true;
        if (lockDuringFade) LockInput(0); // chặn input trong lúc fade

        yield return Fade(1f, 0f);

        storyImage.sprite = pages[index].image;
        storyText.text = pages[index].text;
        currentPage = index;

        yield return Fade(0f, 1f);

        isFading = false;
        if (!isSkipping) UnlockInput();
        onDone?.Invoke();
    }

    private IEnumerator Fade(float from, float to)
    {
        if (imageGroup == null || textGroup == null || fadeDuration <= 0f)
        {
            if (imageGroup != null) imageGroup.alpha = to;
            if (textGroup != null) textGroup.alpha = to;
            yield break;
        }

        float t = 0f;
        SetAlpha(from);

        while (t < fadeDuration)
        {
            t += Delta();
            float a = Mathf.Lerp(from, to, Mathf.Clamp01(t / fadeDuration));
            SetAlpha(a);
            yield return null;
        }

        SetAlpha(to);
    }

    private void SetAlpha(float a)
    {
        if (imageGroup != null) imageGroup.alpha = a;
        if (textGroup != null) textGroup.alpha = a;
    }

    private void ShowPage(int index, bool instant = false)
    {
        currentPage = index;
        storyImage.sprite = pages[index].image;
        storyText.text = pages[index].text;
        SetAlpha(instant ? 1f : 0f);
    }

    // ===== Skip: Fade-to-black một chạm =====
    private IEnumerator SkipWithFadeToBlack()
    {
        isSkipping = true;
        StopAllCoroutines();
        LockInput(0); // đang chuyển scene

        if (overlayGroup == null)
        {
            LoadNextSceneImmediate();
            yield break;
        }

        // Quan trọng: bật chặn raycast qua overlay
        overlayGroup.blocksRaycasts = true;
        fadeOverlay.raycastTarget = true;

        float t = 0f;
        overlayGroup.alpha = 0f;

        while (t < skipFadeDuration)
        {
            t += Delta();
            overlayGroup.alpha = Mathf.Clamp01(t / Mathf.Max(0.0001f, skipFadeDuration));
            yield return null;
        }

        overlayGroup.alpha = 1f;
        LoadNextSceneImmediate();
        // Không unlock: scene mới sẽ reset
    }

    // ===== Helpers thời gian / lịch =====
    private void ScheduleHoldForCurrentPage()
    {
        float hold = pageHoldSeconds + (IsLastPage() ? lastPageExtraWaitSeconds : 0f);
        nextPageAt = Now() + hold;
    }

    private void RestartAutoLoop()
    {
        if (autoLoopCo != null) StopCoroutine(autoLoopCo);
        autoLoopCo = StartCoroutine(AutoAdvanceLoop());
    }

    private bool IsLastPage() => currentPage == pages.Length - 1;

    private void LoadNextSceneImmediate()
    {
        if (!autoLoadNextScene || string.IsNullOrEmpty(nextSceneName)) return;
        SceneManager.LoadScene(nextSceneName);
    }

    private double Now()
    {
#if UNITY_2022_2_OR_NEWER
        return useUnscaledTime ? (double)Time.unscaledTimeAsDouble : (double)Time.timeAsDouble;
#else
        return useUnscaledTime ? (double)Time.unscaledTime : (double)Time.time;
#endif
    }

    private float Delta() => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    // ===== Input Lock Core =====
    private bool IsInputLocked()
    {
        if (isFading && lockDuringFade) return true;
        if (isSkipping) return true;
        if (Now() < lockedUntil) return true;
        return false;
    }

    private void LockInput(float seconds)
    {
        lockedUntil = Mathf.Max((float)lockedUntil, (float)(Now() + Mathf.Max(0f, seconds)));

        if (inputBlocker != null)
        {
            inputBlocker.raycastTarget = true;
            if (blockerGroup != null) blockerGroup.blocksRaycasts = true; // quan trọng
        }
    }

    private void UnlockInput()
    {
        lockedUntil = 0.0;

        if (inputBlocker != null)
        {
            inputBlocker.raycastTarget = false;
            if (blockerGroup != null) blockerGroup.blocksRaycasts = false;
        }

        if (fadeOverlay != null)
        {
            fadeOverlay.raycastTarget = false;
            if (overlayGroup != null) overlayGroup.blocksRaycasts = false; // quan trọng
        }
    }
}
