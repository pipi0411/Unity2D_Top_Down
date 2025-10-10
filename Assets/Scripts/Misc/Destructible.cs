using UnityEngine;

public class Destructible : MonoBehaviour
{
    [SerializeField] private GameObject destroyVFX;
    [SerializeField] private EnemyData enemyData;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.GetComponent<DamageSource>() || collision.gameObject.GetComponent<Projectile>())
        {
            GetComponent<PickUpSpawner>()?.DropItems(enemyData);
            Instantiate(destroyVFX, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
