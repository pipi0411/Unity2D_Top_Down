using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public bool FacingLeft
    {
        get { return facingLeft; }
    }
    public static PlayerController Instance { get; private set; }
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private TrailRenderer myTrailRenderer;
    [SerializeField] private float dashSpeed = 4f;
    private PlayerControls playerControls;
    private Vector2 movement;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private float startingMoveSpeed;
    private bool facingLeft = false;
    private bool isDashing = false;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        rb = GetComponent<Rigidbody2D>();
        playerControls = new PlayerControls();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    private void Start()
    {
        playerControls.Combat.Dash.performed += _ => Dash();
        startingMoveSpeed = moveSpeed;
    }
    private void OnEnable()
    {
        playerControls.Enable();
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
    private void PlayerInput()
    {
        movement = playerControls.Movement.Move.ReadValue<Vector2>();
        animator.SetFloat("moveX", movement.x);
        animator.SetFloat("moveY", movement.y);
    }
    private void Move()
    {
        rb.MovePosition(rb.position + movement * (moveSpeed * Time.fixedDeltaTime));
    }
    private void AdjustPlayerFacingDirection()
    {
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
