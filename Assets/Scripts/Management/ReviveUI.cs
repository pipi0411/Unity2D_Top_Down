using System;
using UnityEngine;

public class ReviveUI : MonoBehaviour
{
    public static ReviveUI Instance;
    [SerializeField] private GameObject rootPanel;
    private Action _onAccept;
    private Action _onDecline;

    private void Awake()
    {
        Instance = this;
        if (rootPanel != null)
        {
            rootPanel.SetActive(false);
        }
    }
    // Giữ overload cũ nếu nơi khác còn dùng
    public void Show()
    {
        Time.timeScale = 0f;
        if (rootPanel != null)
        {
            rootPanel.SetActive(true);
        }
    }
    // Overload mới: cho phép truyền callback từ PlayerHealth
    public void Show(Action onAccept, Action onDecline)
    {
        _onAccept = onAccept;
        _onDecline = onDecline;
        Show();
    }

    public void Hide()
    {
        Time.timeScale = 1f;
        if (rootPanel != null)
        {
            rootPanel.SetActive(false);
        }
    }
    public void OnYes()
    {
        Hide();
        _onAccept?.Invoke();
    }
    public void OnNo()
    {
        Hide();
        if (_onDecline != null)
            _onDecline.Invoke();
        else
            GameOverUI.Instance?.Show();
    }
}
