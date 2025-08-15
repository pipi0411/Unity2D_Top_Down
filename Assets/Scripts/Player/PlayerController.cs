using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerController : Singleton<PlayerController>
{
    public bool FacingLeft
    {
        get { return facingLeft; }
    }
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
    protected override void Awake()
    {
        // Kiểm tra duplicate trước
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        base.Awake();

        // Khởi tạo components
        rb = GetComponent<Rigidbody2D>();
        playerControls = new PlayerControls();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        knockBack = GetComponent<KnockBack>();
    }
    private void OnEnable()
    {
       if (playerControls != null)
       {
        playerControls.Enable();
       }
    }
    private void OnDisable()
    {
        // Null check
        if (playerControls != null)
        {
            playerControls.Disable();
        }
    }
    
    protected override void OnDestroy()
    {
        if (playerControls != null)
        {
            try
            {
                playerControls.Combat.Dash.performed -= _ => Dash();
                playerControls.Dispose();
            }
            catch (System.Exception) { }
        }
        base.OnDestroy();
    }
    private void Start()
    {
        if (playerControls != null)
        {   
            playerControls.Enable();
            playerControls.Combat.Dash.performed += _ => Dash();
        }
        startingMoveSpeed = moveSpeed;
    }
    private void Update()
    {
        PlayerInput();
    }
    private void FixedUpdate()
    {
        Move();
        AdjustPlayerFacingDirection();
    }

    public Transform GetWeaponCollider()
    {
        return weaponCollider;
    }
    private void PlayerInput()
    {
        movement = playerControls.Movement.Move.ReadValue<Vector2>();
        animator.SetFloat("moveX", movement.x);
        animator.SetFloat("moveY", movement.y);
    }
    private void Move()
    {
        if (knockBack.GettingKnockedBack) { return; }
        rb.MovePosition(rb.position + movement * (moveSpeed * Time.fixedDeltaTime));
    }
    private void AdjustPlayerFacingDirection()
    {
        // ✅ Kiểm tra null safety
        if (Mouse.current == null) return;
        
        Vector3 mousePos = Mouse.current.position.ReadValue();
        Vector3 playerScreenPos = Camera.main.WorldToScreenPoint(transform.position);
        
        if (mousePos.x < playerScreenPos.x)
        {
            spriteRenderer.flipX = true;
            facingLeft = true;
        }
        else
        {
            spriteRenderer.flipX = false;
            facingLeft = false;
        }
    }
    private void Dash()
    {
        if (!isDashing)
        {
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
