using System.Collections;
using UnityEngine;

public class GemPickup : MonoBehaviour
{
    [SerializeField] private int gemAmount = 1;
    [SerializeField] private float pickUpDistance = 5f;
    [SerializeField] private float accelerationRate = 0.2f;
    [SerializeField] private float maxMoveSpeed = 6f;
    [SerializeField] private float moveSpeed = 0f;
    [SerializeField] private AnimationCurve animCurve;
    [SerializeField] private float heightY = 1.5f;
    [SerializeField] private float popDuration = 1f;
    [SerializeField] private AudioClip pickupSFX;

    private Vector3 moveDir = Vector3.zero;
    private Rigidbody2D rb;
    private Transform playerTransform;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;
    }

    private void Start()
    {
        StartCoroutine(AnimCurveSpawnRoutine());
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
            else return;
        }

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        if (distance < pickUpDistance)
        {
            moveDir = (playerTransform.position - transform.position).normalized;
            moveSpeed = Mathf.Min(moveSpeed + accelerationRate, maxMoveSpeed);
        }
        else
        {
            moveDir = Vector3.zero;
            moveSpeed = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (rb != null)
            rb.linearVelocity = moveDir * moveSpeed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Phát âm thanh nhặt item qua AudioManager (nếu có)
        AudioManager.Instance?.PlayItemPickup();

        // Thêm gem vào GemManager
        GemManager.Instance?.AddGems(gemAmount);

        Destroy(gameObject);
    }

    // Thêm public setter để GemManager có thể gán số lượng khi spawn từ boss
    public void SetAmount(int amount)
    {
        gemAmount = Mathf.Max(0, amount);
    }

    private IEnumerator AnimCurveSpawnRoutine()
    {
        Vector2 startPoint = transform.position;
        float randomX = transform.position.x + Random.Range(-2f, 2f);
        float randomY = transform.position.y + Random.Range(-1f, 1f);
        Vector2 endPoint = new Vector2(randomX, randomY);

        float timePassed = 0f;
        while (timePassed < popDuration)
        {
            timePassed += Time.deltaTime;
            float linearT = Mathf.Clamp01(timePassed / popDuration);
            float heightT = animCurve != null ? animCurve.Evaluate(linearT) : linearT;
            float height = Mathf.Lerp(0f, heightY, heightT);
            transform.position = Vector2.Lerp(startPoint, endPoint, linearT) + new Vector2(0f, height);
            yield return null;
        }
    }
}