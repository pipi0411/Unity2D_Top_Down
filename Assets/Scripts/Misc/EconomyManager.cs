using TMPro;
using UnityEngine;

public class EconomyManager : Singleton<EconomyManager>
{
    /// Tham chiếu tới UI Text hiển thị số vàng.
    private TMP_Text goldText;
    /// Số vàng hiện tại của người chơi.
    private int currentGold = 0;
    /// Tên GameObject chứa UI Text vàng.
    const string COIN_AMOUNT_TEXT = "Gold Amount Text";
    /// Tăng số vàng lên 1 và cập nhật UI hiển thị vàng.
    public void UpdateCurrentGold()
    {
        currentGold += 1;
        if (goldText == null)
        {
            goldText = GameObject.Find(COIN_AMOUNT_TEXT).GetComponent<TMP_Text>();
        }
        goldText.text = currentGold.ToString("D3");
    }   
}
