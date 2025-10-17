using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : Singleton<PlayerController>
{
    public bool FacingLeft => facingLeft;

    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private TrailRenderer myTrailRenderer;
    [SerializeField] private float dashSpeed = 4f;
    [SerializeField] private Transform weaponCollider;

    private PlayerControls playerControls;
    private Vector2 movement;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private KnockBack knockBack;
    private float startingMoveSpeed;
    private bool facingLeft = false;
    private bool isDashing = false;
    private bool isMoving = false;

    protected override void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        playerControls = new PlayerControls();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        knockBack = GetComponent<KnockBack>();
    }

    private void OnEnable() => playerControls?.Enable();
    private void OnDisable() => playerControls?.Disable();

    private void Start()
    {
        playerControls.Combat.Dash.performed += _ => Dash();
        startingMoveSpeed = moveSpeed;
        ActiveInventory.Instance.EquipStartingWeapon();
    }

    private void Update()
    {
        PlayerInput();
        HandleMovementSound();
    }

    private void FixedUpdate()
    {
        Move();
        AdjustPlayerFacingDirection();
    }

    public Transform GetWeaponCollider() => weaponCollider;

    private void PlayerInput()
    {
        movement = playerControls.Movement.Move.ReadValue<Vector2>();
        animator.SetFloat("moveX", movement.x);
        animator.SetFloat("moveY", movement.y);
    }

    private void Move()
    {
        if (knockBack.GettingKnockedBack || PlayerHealth.Instance.isDead) return;
        rb.MovePosition(rb.position + movement * (moveSpeed * Time.fixedDeltaTime));
    }

    private void HandleMovementSound()
    {
        if (PlayerHealth.Instance.isDead)
        {
            AudioManager.Instance?.StopPlayerRun();
            return;
        }

        if (movement.magnitude > 0.1f)
        {
            if (!isMoving)
            {
                isMoving = true;
                AudioManager.Instance?.PlayPlayerRun();
            }
        }
        else if (isMoving)
        {
            isMoving = false;
            AudioManager.Instance?.StopPlayerRun();
        }
    }

    public void PlayDeathSound()
    {
        AudioManager.Instance?.PlayPlayerDeath();
    }

    private void AdjustPlayerFacingDirection()
    {
        if (Mouse.current == null) return;
        Vector3 mousePos = Mouse.current.position.ReadValue();
        Vector3 playerScreenPos = Camera.main.WorldToScreenPoint(transform.position);
        spriteRenderer.flipX = mousePos.x < playerScreenPos.x;
        facingLeft = spriteRenderer.flipX;
    }

    private void Dash()
    {
        if (!isDashing && Stamina.Instance.CurrentStamina > 0)
        {
            Stamina.Instance.UseStamina();

            AudioManager.Instance?.PlayPlayerDash();
            isDashing = true;
            moveSpeed *= dashSpeed;
            myTrailRenderer.emitting = true;
            StartCoroutine(EndDashRoutine());
        }
    }

    private IEnumerator EndDashRoutine()
    {
        float dashTime = 0.2f;
        float dashCD = 0.25f;
        yield return new WaitForSeconds(dashTime);
        moveSpeed = startingMoveSpeed;
        myTrailRenderer.emitting = false;
        yield return new WaitForSeconds(dashCD);
        isDashing = false;
    }
}
