using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int startingHealth = 3;

    private int currentHealth;
    private KnockBack knockBack;
    private void Start()
    {
        currentHealth = startingHealth;
        knockBack = GetComponent<KnockBack>();
    }
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        
        // Kiểm tra null trước khi sử dụng
        if (knockBack != null && PlayerController.Instance != null)
        {
            knockBack.GetKnockedBack(PlayerController.Instance.transform, 15f);
        }
        
        DetectDeath();
    }
    private void DetectDeath()
    {
        if (currentHealth <= 0)
        {

            Destroy(gameObject);
        }
    }
}
