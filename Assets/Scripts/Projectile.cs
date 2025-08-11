using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 22f;
    [SerializeField] private GameObject particleOnHitPrefabVFX;
    private WeaponInfor weaponInfo;
    private Vector3 startPosition;
    
    private void Start()
    {
        startPosition = transform.position;
    }
    
    private void Update()
    {
        MoveProjectile();
        DetectFireDistance();
    }
    
    public void UpdateWeaponInfo(WeaponInfor weaponInfor)
    {
        weaponInfo = weaponInfor;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Bỏ qua các trigger collider
        if (other.isTrigger) return;
        
        // Kiểm tra xem có phải enemy không
        EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            // Gây damage cho enemy

            Instantiate(particleOnHitPrefabVFX, transform.position, Quaternion.identity);
            Destroy(gameObject);
            return;
        }
        
        // Kiểm tra xem có phải vật thể không thể phá hủy không
        Indestructible indestructible = other.GetComponent<Indestructible>();
        if (indestructible != null)
        {
            // Chỉ tạo effect và destroy projectile, không gây damage
            Instantiate(particleOnHitPrefabVFX, transform.position, Quaternion.identity);
            Destroy(gameObject);
            return;
        }
        
        // Kiểm tra các vật thể khác có thể va chạm (như tường, obstacle)
        // Nếu không phải trigger và không phải enemy thì cũng destroy projectile
        Instantiate(particleOnHitPrefabVFX, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
    
    private void DetectFireDistance()
    {
        if (weaponInfo != null && Vector3.Distance(startPosition, transform.position) > weaponInfo.weaponRange)
        {
            Destroy(gameObject);
        }
    }

    private void MoveProjectile()
    {
        transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
    }
}