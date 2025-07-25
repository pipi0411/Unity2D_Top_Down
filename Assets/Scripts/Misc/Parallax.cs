using UnityEngine;

public class Parallax : MonoBehaviour
{
    [Header("Parallax Settings")]
    [SerializeField] private Vector2 parallaxOffset = new Vector2(-0.15f, -0.15f); // Hỗ trợ cả X và Y
    [SerializeField] private bool limitToViewport = true; // Giới hạn trong viewport
    
    private Camera cam;
    private Vector2 startPos;
    private Vector2 travel => (Vector2)cam.transform.position - startPos;
    
    // Lưu vị trí ban đầu của background object
    private Vector2 backgroundStartPos;
    
    private void Awake()
    {
        cam = Camera.main;
        if (cam == null)
        {
            cam = FindFirstObjectByType<Camera>();
        }
        backgroundStartPos = transform.position;
    }
    
    private void Start()
    {
        startPos = cam.transform.position;
    }
    
    private void FixedUpdate()
    {
        // Tính toán vị trí mới cho background
        Vector2 newPosition = backgroundStartPos + travel * parallaxOffset;
        
        // Nếu muốn giới hạn trong viewport (tùy chọn)
        if (limitToViewport)
        {
            newPosition = ClampToViewport(newPosition);
        }
        
        transform.position = new Vector3(newPosition.x, newPosition.y, transform.position.z);
    }
    
    private Vector2 ClampToViewport(Vector2 position)
    {
        // Lấy bounds của camera
        float camHeight = cam.orthographicSize * 2;
        float camWidth = camHeight * cam.aspect;
        
        Vector2 camPos = cam.transform.position;
        
        // Clamp đơn giản hơn - giới hạn trong phạm vi camera + buffer
        float bufferX = camWidth * 0.5f;
        float bufferY = camHeight * 0.5f;
        
        float clampedX = Mathf.Clamp(position.x, camPos.x - bufferX, camPos.x + bufferX);
        float clampedY = Mathf.Clamp(position.y, camPos.y - bufferY, camPos.y + bufferY);
        
        return new Vector2(clampedX, clampedY);
    }
}