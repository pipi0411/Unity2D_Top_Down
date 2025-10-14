using UnityEngine;

public class AreaEntrance : MonoBehaviour
{
    [SerializeField] private string transitionName;

    private void Start()
    {
        var sm = SceneManagement.Instance;
        if (sm != null && transitionName == sm.SceneTransitionName)
        {
            // Tìm player an toàn
            var player = PlayerController.Instance != null
                ? PlayerController.Instance.gameObject
                : GameObject.FindGameObjectWithTag("Player");

            if (player != null)
                player.transform.position = transform.position;

            // Xóa transition để không bị “nhớ” cho lần sau
            sm.SetTransitionName(string.Empty);

            // Camera follow + fade
            if (CameraController.Instance != null)
                CameraController.Instance.SetPlayerCameraFollow();

            UIFade.Instance?.FadeToClear();
        }
    }
}
