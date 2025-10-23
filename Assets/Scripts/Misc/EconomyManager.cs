using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement; // ← NEW

public class EconomyManager : Singleton<EconomyManager>
{
    private TMP_Text goldText;
    private int currentGold = 0;
    private const string COIN_AMOUNT_TEXT = "Gold Amount Text";

    private void OnEnable()
    {
        // Khi save state thay đổi (xóa/ghi), cập nhật UI
        SceneManagement.SaveStateChanged += OnSaveStateChanged;
        SceneManager.sceneLoaded += OnSceneLoaded; // ← NEW
    }

    private void OnDisable()
    {
        SceneManagement.SaveStateChanged -= OnSaveStateChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded; // ← NEW
    }

    private void Start()
    {
        // Tự tìm Text vàng khi vào scene
        RebindGoldTextIfNeeded();
        UpdateGoldUI();
    }

    private void OnSaveStateChanged(bool hasSave)
    {
        // Khi save bị xóa/ghi, cố gắng rebind và cập nhật UI
        RebindGoldTextIfNeeded();
        UpdateGoldUI();
    }

    // cố gắng tìm lại goldText bằng nhiều heuristic nếu cần
    private void RebindGoldTextIfNeeded()
    {
        if (goldText != null) return;

        // 1) tìm theo tên cố định
        var go = GameObject.Find(COIN_AMOUNT_TEXT);
        if (go != null)
        {
            goldText = go.GetComponent<TMP_Text>();
            if (goldText != null) return;
        }

        // 2) fallback: tìm tất cả TMP_Text trong scene, chọn object có tên chứa "gold" hoặc text mặc định giống "000"
        // include inactive objects to catch UI instantiated but disabled at start
        var texts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var t in texts)
        {
            if (t.gameObject.name.ToLower().Contains("gold") || t.gameObject.name.ToLower().Contains("coin"))
            {
                goldText = t;
                return;
            }
        }

        foreach (var t in texts)
        {
            // nếu text đang là số (dạng 000) hoặc rỗng thì coi là candidate
            if (string.IsNullOrEmpty(t.text) || System.Text.RegularExpressions.Regex.IsMatch(t.text, @"^\d+$"))
            {
                goldText = t;
                return;
            }
        }
    }

    // đảm bảo rebind lại khi scene load xong (đúng thời điểm UI đã khởi tạo)
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // start coroutine để rebind sau một frame (UI prefabs/awake có thời gian)
        StartCoroutine(DelayedRebindAndRefresh());
    }

    private System.Collections.IEnumerator DelayedRebindAndRefresh()
    {
        yield return null; // một frame
        RebindGoldTextIfNeeded();
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
        RebindGoldTextIfNeeded();

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
    public bool SendGold(int amount)
    {
        if (currentGold >= amount)
        {
            currentGold -= amount;
            UpdateGoldUI();
            return true;
        }
        return false;
    }

    // Giữ lại hàm cũ (nếu chỗ nào vẫn gọi)
    public void UpdateCurrentGold()
    {
        AddGold(1);
    }
}
