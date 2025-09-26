using Unity.Cinemachine;
using UnityEngine;

public class ScreenShakeManager : Singleton<ScreenShakeManager>
{
    private CinemachineImpulseSource source;

    protected override void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Xóa instance mới nếu đã có instance cũ
            return;
        }
        base.Awake();
        source = GetComponent<CinemachineImpulseSource>();
        if (source == null)
        {
            Debug.LogError("CinemachineImpulseSource component is missing on ScreenShakeManager.");
        }
        // DontDestroyOnLoad(gameObject);
    }

    public void ShakeScreen()
    {
        if (source != null)
        {
            source.GenerateImpulse();
        }
        else
        {
            Debug.LogWarning("ScreenShakeManager: CinemachineImpulseSource is missing, cannot shake screen.");
        }
    }
}