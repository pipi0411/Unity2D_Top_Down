using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[System.Serializable]
public class StoryPage
{
    public Sprite image;
    [TextArea(3, 10)]
    public string text;
}

public class IntroduceScene : MonoBehaviour
{
    public Image storyImage;
    public Text storyText;
    public StoryPage[] pages;
    private int currentPage = 0;

    void Start()
    {
        ShowPage();
    }

    void Update()
    {
        // Khi người chơi click chuột trái → chuyển trang
        if (Input.GetMouseButtonDown(0))
        {
            NextPage();
        }
    }

    void ShowPage()
    {
        if (currentPage < pages.Length)
        {
            storyImage.sprite = pages[currentPage].image;
            storyText.text = pages[currentPage].text;
        }
        else
        {
            // Khi hết trang → chuyển sang MenuScene
            SceneManager.LoadScene("MenuScene");
        }
    }

    void NextPage()
    {
        currentPage++;
        ShowPage();
    }
}