using UnityEngine;

public class ActiveInventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    [SerializeField] private int maxSlots = 5;
    
    private int activeSlotIndexNum = 0;
    private PlayerControls playerControls;
    
    // Cache references để tránh GetChild() calls
    private Transform[] inventorySlots;
    private GameObject[] highlightObjects;
    
    private void Awake()
    {
        playerControls = new PlayerControls();
        CacheInventorySlots();
    }
    
    private void Start()
    {
        // Subscribe event
        playerControls.Inventory.Keyboard.performed += OnInventoryKeyPressed;
        
        // Initialize first slot
        SetActiveSlot(0);
    }
    
    private void OnEnable()
    {
        playerControls?.Enable();
    }
    
    private void OnDisable()
    {
        playerControls?.Disable();
    }
    
    // Cleanup events để tránh memory leak
    private void OnDestroy()
    {
        if (playerControls != null)
        {
            playerControls.Inventory.Keyboard.performed -= OnInventoryKeyPressed;
            playerControls.Dispose();
        }
    }
    
    // Cache references một lần duy nhất
    private void CacheInventorySlots()
    {
        int slotCount = transform.childCount;
        inventorySlots = new Transform[slotCount];
        highlightObjects = new GameObject[slotCount];
        
        for (int i = 0; i < slotCount; i++)
        {
            inventorySlots[i] = transform.GetChild(i);
            
            // Safe GetChild với error handling
            if (inventorySlots[i].childCount > 0)
            {
                highlightObjects[i] = inventorySlots[i].GetChild(0).gameObject;
            }
        }
    }
    
    // Separate event handler
    private void OnInventoryKeyPressed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        int numValue = (int)ctx.ReadValue<float>();
        SetActiveSlot(numValue - 1);
    }
    
    // Optimized slot switching
    private void SetActiveSlot(int newIndex)
    {
        // Bounds checking
        if (newIndex < 0 || newIndex >= inventorySlots.Length)
        {
            Debug.LogWarning($"Invalid slot index: {newIndex}");
            return;
        }
        
        // Avoid unnecessary work
        if (newIndex == activeSlotIndexNum) return;
        
        // Deactivate current slot
        if (activeSlotIndexNum >= 0 && activeSlotIndexNum < highlightObjects.Length)
        {
            highlightObjects[activeSlotIndexNum]?.SetActive(false);
        }
        
        // Activate new slot
        activeSlotIndexNum = newIndex;
        if (highlightObjects[activeSlotIndexNum] != null)
        {
            highlightObjects[activeSlotIndexNum].SetActive(true);
        }
        
        // Optional: Event for other systems
        OnSlotChanged?.Invoke(activeSlotIndexNum);
    }
    
    // Public API
    public System.Action<int> OnSlotChanged;
    
    public int GetActiveSlotIndex() => activeSlotIndexNum;
    
    public void SetActiveSlotProgrammatically(int index)
    {
        SetActiveSlot(index);
    }
    
    // Validation method
    private void OnValidate()
    {
        if (maxSlots <= 0)
        {
            maxSlots = 1;
        }
    }
}