using UnityEngine;
using System.Collections.Generic;

/// Hitbox của boss: collider luôn bật (IsTrigger = true), KHÔNG toggle enable ở runtime.
/// Khi Activate(): đặt cờ "isHot", quét overlap hiện tại để ăn đòn ngay nếu đang chồng collider.
/// Trong lúc "isHot": cả Enter/Stay đều có thể gây 1 hit duy nhất cho mỗi nạn nhân / lần kích hoạt.
[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class AttackArea : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 10;

    [Header("Timing")]
    [Tooltip("Thời gian hitbox hoạt động nếu không truyền duration vào Activate().")]
    [SerializeField] private float defaultActiveTime = 0.15f;

    [Header("Filtering")]
    [Tooltip("Chỉ gây sát thương cho các layer này (ví dụ: Player, PlayerHurtBox).")]
    [SerializeField] private LayerMask damageLayers = ~0; // mặc định: tất cả

    private Collider2D col;
    private bool isHot = false;
    private float hotUntil = -1f;

    // chống trừ máu lặp trong cùng một lần kích hoạt
    private readonly HashSet<int> hitVictims = new HashSet<int>();

    // cho OverlapCollider
    private readonly List<Collider2D> overlapResults = new List<Collider2D>(8);
    private ContactFilter2D filter;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        if (!col)
        {
            Debug.LogError("[AttackArea] Missing Collider2D, disabling.");
            enabled = false;
            return;
        }

        col.isTrigger = true; // luôn là trigger

        // đảm bảo có Rigidbody2D kinematic để trigger ổn định ở build
        var rb = GetComponent<Rigidbody2D>();
        if (!rb)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        else
        {
            // nếu đang để Dynamic thì vẫn hoạt động, nhưng Kinematic là đủ & rẻ hơn
            if (rb.bodyType != RigidbodyType2D.Kinematic)
                rb.bodyType = RigidbodyType2D.Kinematic;
        }
        rb.simulated = true;

        // thiết lập bộ lọc overlap (chỉ quét các layer mục tiêu nếu có)
        filter = new ContactFilter2D
        {
            useTriggers = true,
            useLayerMask = true,
            layerMask = damageLayers
        };

        // KHÔNG đụng tới col.enabled ở runtime để tránh crash build
        // col.enabled = true;
    }

    /// <summary>Bật "sát thương" trong 'duration' giây (nếu < 0 dùng defaultActiveTime).</summary>
    public void Activate(float duration = -1f)
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy) return;

        isHot = true;
        hotUntil = Time.time + ((duration > 0f) ? duration : defaultActiveTime);
        hitVictims.Clear();

        // Nếu bật hitbox khi đã đứng chồng collider với Player → quét và gây đòn ngay.
        overlapResults.Clear();
        col.Overlap(filter, overlapResults);
        for (int i = 0; i < overlapResults.Count; i++)
        {
            TryDamage(overlapResults[i]);
        }
    }

    /// <summary>Tắt "sát thương" ngay lập tức.</summary>
    public void Deactivate()
    {
        isHot = false;
        hotUntil = -1f;
        hitVictims.Clear();
    }

    private void Update()
    {
        if (isHot && hotUntil > 0f && Time.time >= hotUntil)
        {
            isHot = false;
            hotUntil = -1f;
            hitVictims.Clear();
        }
    }

    private void OnDisable()
    {
        // reset khi object bị disable (đổi scene/unload…)
        isHot = false;
        hotUntil = -1f;
        hitVictims.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isHot) return;
        if (!IsInDamageLayer(other.gameObject.layer)) return;
        TryDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isHot) return;
        if (!IsInDamageLayer(other.gameObject.layer)) return;
        TryDamage(other);
    }

    private bool IsInDamageLayer(int layer)
    {
        return (damageLayers.value & (1 << layer)) != 0;
    }

    /// <summary>Gây sát thương nếu collider thuộc về Player (ở bất kỳ cấp nào) và chưa bị đánh trong lần kích hoạt này.</summary>
    private void TryDamage(Collider2D other)
    {
        // Tìm PlayerHealth ở collider hiện tại hoặc parent của nó
        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph == null) ph = other.GetComponentInParent<PlayerHealth>();
        if (ph == null) return;

        int id = ph.GetInstanceID();
        if (hitVictims.Contains(id)) return; // đã ăn dame trong lần kích hoạt hiện tại

        ph.TakeDamage(damage, transform);
        hitVictims.Add(id);
        // Debug.Log($"[AttackArea] Hit {ph.name} for {damage}");
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // đồng bộ filter khi đổi LayerMask trên inspector
        filter.useTriggers = true;
        filter.useLayerMask = true;
        filter.layerMask = damageLayers;
    }
#endif
}
