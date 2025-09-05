using System.Collections;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int startingHealth = 3;
    [SerializeField] private GameObject deathVFXPrefab;
    [SerializeField] private float knockBackThrust = 15f;
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
            knockBack.GetKnockedBack(PlayerController.Instance.transform, knockBackThrust);
        }

        if (flash != null)
        {
            StartCoroutine(flash.FlashRoutine());
        }
        StartCoroutine(CheckDetectDeathRoutine());
    }
    private IEnumerator CheckDetectDeathRoutine()
    {
        yield return new WaitForSeconds(flash.GetRestoreMatTime());
        DetectDeath();
    }
    public void DetectDeath()
    {
        if (currentHealth <= 0)
        {
            Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
            GetComponent<PickUpSpawner>().DropItems();
            Destroy(gameObject);
        }
    }
}
