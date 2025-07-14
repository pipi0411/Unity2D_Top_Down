using UnityEngine;
using UnityEngine.PlayerLoop;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int startingHealth = 3;
    [SerializeField] private GameObject deathVFXPrefab;
    private int currentHealth;
    private KnockBack knockBack;
    private Flash flash;
    private void Awake()
    {
        knockBack = GetComponent<KnockBack>();
        flash = GetComponent<Flash>();
    }
    private void Start()
    {
        currentHealth = startingHealth;
    }
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        
        // Kiểm tra null trước khi sử dụng
        if (knockBack != null && PlayerController.Instance != null)
        {
            knockBack.GetKnockedBack(PlayerController.Instance.transform, 15f);
        }
        
        if (flash != null)
        {
            StartCoroutine(flash.FlashRoutine());
        }
    }
    public void DetectDeath()
    {
        if (currentHealth <= 0)
        {
            Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
