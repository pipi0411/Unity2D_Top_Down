// path: Assets/Scripts/Intro/StoryController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Trình chiếu Story có fade in/out, auto-advance theo thời gian.
/// KHÔNG còn Skip/overlay đen.
/// </summary>
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
    [Tooltip("Thời gian fade in/out cho mỗi lần chuyển trang.")]
    public float fadeDuration = 0.5f;

    [Header("Auto Play")]
    [Tooltip("Tự động chuyển trang sau Page Hold Seconds.")]
    public bool autoPlay = true;

    [Tooltip("Thời gian giữ mỗi trang trước khi chuyển trang (không tính thời gian fade).")]
    public float pageHoldSeconds = 2.0f;

    [Tooltip("Chờ thêm ở TRANG CUỐI trước khi tự sang scene kế.")]
    public float lastPageExtraWaitSeconds = 2.0f;

    [Tooltip("Dùng unscaled time để không bị ảnh hưởng bởi Time.timeScale.")]
    public bool useUnscaledTime = true;

    [Header("Next Scene")]
    [Tooltip("Tự động load scene tiếp theo khi hết trang.")]
    public bool autoLoadNextScene = true;

    [Tooltip("Tên scene sẽ load sau khi hết trang.")]
    public string nextSceneName = "Scene1";

    [Header("Input Lock")]
    [Tooltip("Khóa input trong lúc fade để tránh double trigger.")]
    public bool lockDuringFade = true;

    [Tooltip("Debounce input tối thiểu giữa 2 lần bấm.")]
    public float minInputGapSeconds = 0.15f;

    [Tooltip("UI/Image trong suốt full-screen để chặn raycast khi bị khóa (tùy chọn).")]
    public Image inputBlocker;

    private int currentPage = 0;
    private CanvasGroup imageGroup;
    private CanvasGroup textGroup;
    private CanvasGroup blockerGroup;

    private bool isFading = false;

    private double nextPageAt = double.PositiveInfinity;
    private Coroutine autoLoopCo;

    // Input lock
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

        if (inputBlocker != null)
        {
            blockerGroup = inputBlocker.GetComponent<CanvasGroup>();
            if (blockerGroup == null) blockerGroup = inputBlocker.gameObject.AddComponent<CanvasGroup>();
            blockerGroup.alpha = 0f;
            blockerGroup.interactable = false;
            blockerGroup.blocksRaycasts = false; // không chặn khi idle
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
            TryLoadNextScene();
        }
    }

    private void ManualNextPage()
    {
        if (currentPage >= pages.Length - 1)
        {
            TryLoadNextScene();
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
            if (!isFading)
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
                        TryLoadNextScene();
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
        if (lockDuringFade) LockInput(0); // vì: chặn input trong lúc fade

        yield return Fade(1f, 0f);

        storyImage.sprite = pages[index].image;
        storyText.text = pages[index].text;
        currentPage = index;

        yield return Fade(0f, 1f);

        isFading = false;
        UnlockInput();
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

    private void TryLoadNextScene()
    {
        if (!autoLoadNextScene || string.IsNullOrEmpty(nextSceneName)) return;
        SceneManager.LoadScene(nextSceneName);
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

    private double Now()
    {
#if UNITY_2022_2_OR_NEWER
        return useUnscaledTime ? (double)Time.unscaledTimeAsDouble : (double)Time.timeAsDouble;
#else
        return useUnscaledTime ? (double)Time.unscaledTime : (double)Time.time;
#endif
    }

    private float Delta() => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    // ===== Input Lock =====
    private bool IsInputLocked()
    {
        if (isFading && lockDuringFade) return true;
        if (Now() < lockedUntil) return true;
        return false;
    }

    private void LockInput(float seconds)
    {
        lockedUntil = Mathf.Max((float)lockedUntil, (float)(Now() + Mathf.Max(0f, seconds)));
        if (inputBlocker != null)
        {
            inputBlocker.raycastTarget = true;
            if (blockerGroup != null) blockerGroup.blocksRaycasts = true;
        }
    }

    private void UnlockInput()
    {
        lockedUntil = 0.0f;
        if (inputBlocker != null)
        {
            inputBlocker.raycastTarget = false;
            if (blockerGroup != null) blockerGroup.blocksRaycasts = false;
        }
    }
}
