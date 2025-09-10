using System.Collections;
using UnityEngine;

public class Pickup : MonoBehaviour
{
    private enum PickupType
    {
        /// Vật phẩm là đồng vàng.
        GoldCoin,
        // Vật phẩm là quả cầu stamina (năng lượng).
        StaminaGlobe,
        // Vật phẩm là quả cầu máu (hồi phục sức khỏe).
        HealthGlobe
    }
    [SerializeField] private PickupType pickUpType;
    [SerializeField] private float pickUpDistance = 5f;
    [SerializeField] private float accelartionRate = 0.2f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private AnimationCurve animCurve;
    [SerializeField] private float heightY = 1.5f;
    [SerializeField] private float popDuration = 1f;
    private Vector3 moveDir;
    private Rigidbody2D rb;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    private void Start()
    {
        StartCoroutine(AnimCurveSpawnRoutine());
    }
    /// Hàm Update được gọi mỗi khung hình (frame) để cập nhật trạng thái của đối tượng.
    /// Hàm này kiểm tra khoảng cách giữa đối tượng hiện tại và người chơi, sau đó điều chỉnh hướng di chuyển và tốc độ của đối tượng.
    private void Update()
    {
        /// Lấy vị trí của người chơi từ đối tượng PlayerController (singleton instance).
        Vector3 playerPos = PlayerController.Instance.transform.position;

        /// Kiểm tra khoảng cách giữa đối tượng hiện tại và người chơi.
        /// Nếu khoảng cách nhỏ hơn <c>pickUpDistance</c>, đối tượng sẽ di chuyển về phía người chơi.
        if (Vector3.Distance(transform.position, playerPos) < pickUpDistance)
        {
            /// Tính toán hướng di chuyển bằng cách lấy vector từ vị trí đối tượng đến vị trí người chơi,
            /// sau đó chuẩn hóa (normalized) để có độ dài bằng 1.
            moveDir = (playerPos - transform.position).normalized;

            /// Tăng tốc độ di chuyển của đối tượng bằng cách cộng thêm giá trị <c>accelartionRate</c>.
            moveSpeed += accelartionRate;
        }
        else
        {
            /// Nếu khoảng cách lớn hơn hoặc bằng <c>pickUpDistance</c>,
            /// đặt hướng di chuyển về vector không (Vector3.zero) và tốc độ về 0,
            /// khiến đối tượng dừng lại.
            moveDir = Vector3.zero;
            moveSpeed = 0;
        }
    }
    private void FixedUpdate()
    {
        rb.linearVelocity = moveDir * moveSpeed * Time.fixedDeltaTime;
    }
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.gameObject.GetComponent<PlayerController>())
        {
            DetectPickupType();
            Destroy(gameObject);
        }
    }
    /// Coroutine điều khiển hiệu ứng xuất hiện (spawn) của đối tượng với chuyển động theo đường cong.
    /// Đối tượng sẽ di chuyển từ vị trí bắt đầu đến một vị trí ngẫu nhiên gần đó, đồng thời tạo hiệu ứng "nhảy lên" theo một đường cong được định nghĩa bởi AnimationCurve.
    /// <returns>Trả về một IEnumerator để sử dụng trong cơ chế coroutine của Unity.</returns>
    private IEnumerator AnimCurveSpawnRoutine()
    {
        /// Lưu vị trí ban đầu của đối tượng (vị trí hiện tại của transform).
        Vector2 startPoint = transform.position;

        /// Tạo một giá trị X ngẫu nhiên trong khoảng từ -2 đến 2 đơn vị so với vị trí hiện tại.
        float randomX = transform.position.x + Random.Range(-2f, 2f);

        /// Tạo một giá trị Y ngẫu nhiên trong khoảng từ -1 đến 1 đơn vị so với vị trí hiện tại.
        float randomY = transform.position.y + Random.Range(-1f, 1f);

        /// Tạo điểm kết thúc cho chuyển động của đối tượng dựa trên các giá trị X, Y ngẫu nhiên.
        Vector2 endPoint = new Vector2(randomX, randomY);

        /// Biến đếm thời gian đã trôi qua kể từ khi bắt đầu coroutine.
        float timePassed = 0f;

        /// Lặp cho đến khi thời gian đã trôi qua vượt quá thời gian hiệu ứng (popDuration).
        while (timePassed < popDuration)
        {
            /// Cộng dồn thời gian đã trôi qua mỗi frame, dựa trên Time.deltaTime.
            timePassed += Time.deltaTime;

            /// Tính tỷ lệ tuyến tính (linearT) của thời gian đã trôi qua so với thời gian tổng (popDuration).
            /// Giá trị này nằm trong khoảng [0, 1].
            float linearT = timePassed / popDuration;

            /// Đánh giá giá trị của AnimationCurve tại thời điểm linearT để xác định độ cao của chuyển động.
            /// AnimationCurve định nghĩa hình dạng của hiệu ứng "nhảy lên".
            float heightT = animCurve.Evaluate(linearT);

            /// Nội suy tuyến tính (Lerp) để tính độ cao thực tế của đối tượng, từ 0 đến heightY,
            /// dựa trên giá trị heightT từ AnimationCurve.
            float height = Mathf.Lerp(0f, heightY, heightT);

            /// Nội suy tuyến tính vị trí của đối tượng từ startPoint đến endPoint dựa trên linearT,
            /// đồng thời cộng thêm độ cao (height) trên trục Y để tạo hiệu ứng nhảy lên.
            transform.position = Vector2.Lerp(startPoint, endPoint, linearT) + new Vector2(0f, height);

            /// Tạm dừng coroutine đến frame tiếp theo, đảm bảo chuyển động mượt mà.
            yield return null;
        }
    }
    /// Hàm kiểm tra và xử lý hành vi dựa trên loại vật phẩm được nhặt.
    /// Gọi các hành động tương ứng với từng loại vật phẩm (PickupType).
    private void DetectPickupType()
    {
        /// Sử dụng cấu trúc switch để kiểm tra giá trị của biến pickUpType
        /// và thực hiện hành động tương ứng với từng loại vật phẩm.
        switch (pickUpType)
        {
            /// Trường hợp vật phẩm là GoldCoin: In ra thông báo nhặt được đồng vàng.
            case PickupType.GoldCoin:
                EconomyManager.Instance.UpdateCurrentGold();
                break;
            /// Trường hợp vật phẩm là StaminaGlobe: In ra thông báo nhặt được quả cầu stamina.
            case PickupType.StaminaGlobe:
                Stamina.Instance.RefreshStamina();
                break;
            /// Trường hợp vật phẩm là HealthGlobe: Gọi hàm HealPlayer từ PlayerHealth
            /// để hồi máu cho người chơi và in ra thông báo nhặt được quả cầu máu.
            case PickupType.HealthGlobe:
                PlayerHealth.Instance.HealPlayer();
                break;
            default:
                break;
        }
    }
}
