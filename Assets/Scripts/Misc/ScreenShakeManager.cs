using Unity.Cinemachine;
using UnityEngine;


public class ScreenShakeManager : Singleton<ScreenShakeManager>
{
    private CinemachineImpulseSource source;
    protected override void Awake()
    {
        base.Awake();
        source = GetComponent<CinemachineImpulseSource>();
        if (source == null)
        {
            Debug.LogError("CinemachineImpulseSource component is missing on ScreenShakeManager.");
        }
    }
    public void ShakeScreen()
    {
        source.GenerateImpulse();
    }
}

