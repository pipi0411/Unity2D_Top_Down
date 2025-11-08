using UnityEngine;

public class AttackArea : MonoBehaviour
{
    [SerializeField] private int damage = 10;
    [SerializeField] private float activeTime = 0.15f;
    private Collider2D col;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        if (col == null)
        {
            enabled = false;
            return;
        }
        col.isTrigger = true;
        // ensure there is a kinematic Rigidbody2D on this object or parent for reliable trigger callbacks in build
        if (GetComponent<Rigidbody2D>() == null)
            gameObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        col.enabled = false; // start disabled
    }

    // gọi từ BossController/Animator (Animation Event) khi bắt đầu frame gây sát thương
    public void Activate(float duration = -1f)
    {
        if (duration > 0) activeTime = duration;
        StopAllCoroutines();
        StartCoroutine(ActivateRoutine());
    }

    private System.Collections.IEnumerator ActivateRoutine()
    {
        col.enabled = true;
        yield return new WaitForSeconds(activeTime);
        col.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // tìm component player health (tùy project tên class có thể khác)
        var playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            // pass the attacker transform so PlayerHealth can know the hit source
            playerHealth.TakeDamage(damage, transform);
        }
        else
        {
            // nếu bạn dùng tag -> kiểm tra tag
            if (other.CompareTag("Player"))
            {
                var ph = other.GetComponentInParent<PlayerHealth>();
                if (ph != null) ph.TakeDamage(damage, transform);
            }
        }
    }
}