using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class StoryController : MonoBehaviour
{
    [System.Serializable]
    public class StoryPage
    {
        public Sprite image;
        [TextArea(3, 5)] public string text;
    }

    public Image storyImage;
    public TextMeshProUGUI storyText;
    public StoryPage[] pages;
    public float fadeDuration = 0.5f;

    private int currentPage = 0;
    private CanvasGroup imageGroup;
    private CanvasGroup textGroup;
    private bool isFading = false; // chặn spam click trong lúc fade

    void Start()
    {
        imageGroup = storyImage.GetComponent<CanvasGroup>();
        if (imageGroup == null) imageGroup = storyImage.gameObject.AddComponent<CanvasGroup>();

        textGroup = storyText.GetComponent<CanvasGroup>();
        if (textGroup == null) textGroup = storyText.gameObject.AddComponent<CanvasGroup>();

        ShowPage(0, instant: true);
    }

    void Update()
    {
        // Nhấn chuột trái hoặc phím Space / Enter để qua trang
        if (!isFading && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
        {
            NextPage();
        }
    }

    public void NextPage()
    {
        if (currentPage < pages.Length - 1)
        {
            currentPage++;
            StopAllCoroutines();
            StartCoroutine(FadeToPage(currentPage));
        }
        else
        {
            Debug.Log("Hết truyện!");
            // Ví dụ load sang scene khác:
            // UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
        }
    }

    IEnumerator FadeToPage(int index)
    {
        isFading = true;
        yield return StartCoroutine(Fade(1, 0)); // Fade out

        storyImage.sprite = pages[index].image;
        storyText.text = pages[index].text;

        yield return StartCoroutine(Fade(0, 1)); // Fade in
        isFading = false;
    }

    IEnumerator Fade(float from, float to)
    {
        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / fadeDuration);
            imageGroup.alpha = a;
            textGroup.alpha = a;
            yield return null;
        }
    }

    void ShowPage(int index, bool instant = false)
    {
        storyImage.sprite = pages[index].image;
        storyText.text = pages[index].text;
        imageGroup.alpha = textGroup.alpha = instant ? 1 : 0;
    }
}