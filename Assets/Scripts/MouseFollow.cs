using UnityEngine;
using UnityEngine.InputSystem; // Thêm dòng này

public class MouseFollow : MonoBehaviour
{
    private void Update()
    {
        FaceMouse();
    }
    private void FaceMouse()
    {
        Vector3 mousePosition = Mouse.current.position.ReadValue(); // Sử dụng Input System mới
        mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);
        Vector2 direction = transform.position - mousePosition;
        transform.right = -direction;
    }
}