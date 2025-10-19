using TMPro;
using UnityEngine;

public class EconomyManager : Singleton<EconomyManager>
{
    private TMP_Text goldText;
    private int currentGold = 0;
    private const string COIN_AMOUNT_TEXT = "Gold Amount Text";

    private void Start()
    {
        // Tự tìm Text vàng khi vào scene
        if (goldText == null)
        {
            var obj = GameObject.Find(COIN_AMOUNT_TEXT);
            if (obj != null)
                goldText = obj.GetComponent<TMP_Text>();
        }

        UpdateGoldUI();
    }

    // 🔹 Cộng vàng
    public void AddGold(int amount)
    {
        currentGold += Mathf.Max(0, amount);
        UpdateGoldUI();
    }

    // 🔹 Trừ vàng
    public void SpendGold(int amount)
    {
        currentGold = Mathf.Max(0, currentGold - amount);
        UpdateGoldUI();
    }

    // 🔹 Cập nhật UI
    private void UpdateGoldUI()
    {
        if (goldText == null)
        {
            var obj = GameObject.Find(COIN_AMOUNT_TEXT);
            if (obj != null)
                goldText = obj.GetComponent<TMP_Text>();
        }

        if (goldText != null)
            goldText.text = currentGold.ToString("D3");
    }

    // 🔹 Getter/Setter cho Save/Load
    public int GetGold() => currentGold;
    public void SetGold(int value)
    {
        currentGold = Mathf.Max(0, value);
        UpdateGoldUI();
    }

    // Giữ lại hàm cũ (nếu chỗ nào vẫn gọi)
    public void UpdateCurrentGold()
    {
        AddGold(1);
    }
}
