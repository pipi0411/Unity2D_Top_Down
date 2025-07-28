using UnityEngine;

public class AreaEntrance : MonoBehaviour
{
    [SerializeField] private string transitionName;
    private void Start()
    {
        if (SceneManagement.Instance != null && transitionName == SceneManagement.Instance.SceneTransitionName)
        {
            PlayerController.Instance.transform.position = this.transform.position;
            SceneManagement.Instance.SetTransitionName("");
            CameraController.Instance.SetPlayerCameraFollow();
        }
    }
}
