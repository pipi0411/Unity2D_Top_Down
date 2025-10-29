using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Stamina : Singleton<Stamina>
{
    public int CurrentStamina { get; private set; }
    [SerializeField] private Sprite fullStaminaImage, emptyStaminaImage;
    [SerializeField] private int timeBetweenStaminaRefresh = 3;

    private Transform staminaContainer;
    private Coroutine refreshRoutine;

    private int startingStamina = 3;
    private int maxStamina;
    const string STAMINA_CONTAINER_TEXT = "Stamina Container";

    protected override void Awake()
    {
        base.Awake();
        maxStamina = startingStamina;
        CurrentStamina = startingStamina;
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (refreshRoutine != null)
        {
            StopCoroutine(refreshRoutine);
            refreshRoutine = null;
        }
    }

    private void Start()
    {
        TryFindStaminaContainer();
        UpdateStaminaImages(); // safe if không có UI
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        staminaContainer = null;
        TryFindStaminaContainer();
        UpdateStaminaImages();
    }

    private bool TryFindStaminaContainer()
    {
        // Tìm cả khi object đang inactive
        staminaContainer = null;
        foreach (var rt in Resources.FindObjectsOfTypeAll<RectTransform>())
        {
            if (!rt.gameObject.scene.IsValid()) continue; // loại prefab/asset
            if ((rt.hideFlags & HideFlags.HideInHierarchy) != 0) continue;
            if (rt.name == STAMINA_CONTAINER_TEXT)
            {
                staminaContainer = rt.transform;
                break;
            }
        }
        return staminaContainer != null;
    }

    public void UseStamina()
    {
        if (CurrentStamina <= 0) return;
        CurrentStamina--;
        UpdateStaminaImages();

        // Bắt đầu hồi nếu chưa chạy
        if (refreshRoutine == null)
            refreshRoutine = StartCoroutine(RefreshStaminaRoutine());
    }

    public void RefreshStamina()
    {
        if (CurrentStamina < maxStamina)
        {
            CurrentStamina++;
        }
        UpdateStaminaImages();

        // Full thì dừng coroutine
        if (CurrentStamina >= maxStamina && refreshRoutine != null)
        {
            StopCoroutine(refreshRoutine);
            refreshRoutine = null;
        }
    }

    private IEnumerator RefreshStaminaRoutine()
    {
        var wait = new WaitForSeconds(timeBetweenStaminaRefresh);
        while (true)
        {
            yield return wait;

            // Nếu UI bị destroy giữa chừng, tạm dừng update UI
            if (staminaContainer == null || staminaContainer.Equals(null))
            {
                // Thử tìm lại, nếu không có (ví dụ ở MenuScene) thì tiếp tục chờ
                TryFindStaminaContainer();
            }

            RefreshStamina();
        }
    }

    private void UpdateStaminaImages()
    {
        // Nếu UI không tồn tại ở scene này, chỉ cập nhật logic và thoát
        if (staminaContainer == null || staminaContainer.Equals(null))
        {
            // Thử bind lại 1 lần
            if (!TryFindStaminaContainer()) return;
        }

        // An toàn: số child có thể ít hơn maxStamina (tránh out of range)
        int childCount = staminaContainer.childCount;
        for (int i = 0; i < maxStamina && i < childCount; i++)
        {
            var img = staminaContainer.GetChild(i).GetComponent<Image>();
            if (img == null) continue;

            img.sprite = (i <= CurrentStamina - 1) ? fullStaminaImage : emptyStaminaImage;
        }

        // Quản lý coroutine hồi stamina (trong trường hợp bị gọi trực tiếp)
        if (CurrentStamina < maxStamina && refreshRoutine == null)
            refreshRoutine = StartCoroutine(RefreshStaminaRoutine());
        else if (CurrentStamina >= maxStamina && refreshRoutine != null)
        {
            StopCoroutine(refreshRoutine);
            refreshRoutine = null;
        }
    }
}
