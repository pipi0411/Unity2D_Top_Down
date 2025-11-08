using System.Collections;
using UnityEngine;
using TMPro;

public class GemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI gemText;

    private IEnumerator Start()
    {
        if (gemText == null)
            gemText = GetComponentInChildren<TextMeshProUGUI>();

        // chờ Frame để đảm bảo GemManager đã Awake()
        yield return null;

        // nếu GemManager chưa tồn tại thì chờ tới khi có
        while (GemManager.Instance == null)
            yield return null;

        GemManager.Instance.OnGemsChanged += Refresh;
        Refresh(GemManager.Instance.CurrentGems);
    }

    private void OnDisable()
    {
        if (GemManager.Instance != null)
            GemManager.Instance.OnGemsChanged -= Refresh;
    }

    private void Refresh(int value)
    {
        if (gemText == null) return;
        gemText.text = value.ToString();
    }
}