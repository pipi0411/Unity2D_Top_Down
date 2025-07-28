using Unity.Cinemachine;
using UnityEngine;

public class CameraController : Singleton<CameraController>
{
    private CinemachineCamera cinemachineCamera;
    public void SetPlayerCameraFollow()
    {
        cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
        cinemachineCamera.Follow = PlayerController.Instance.transform;
    }
}
