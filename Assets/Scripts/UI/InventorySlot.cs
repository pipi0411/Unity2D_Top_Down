using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IPointerClickHandler
{
    [Header("Weapon Data")]
    [SerializeField] private WeaponInfor weaponInfo;

    public WeaponInfor GetWeaponInfor() => weaponInfo;
    public void SetWeaponInfor(WeaponInfor newWeaponInfo) => weaponInfo = newWeaponInfo;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (weaponInfo == null)
        {
            Debug.LogWarning("[InventorySlot] Slot này chưa có WeaponInfor!");
            return;
        }

        if (weaponInfo.weaponPrefab == null)
        {
            Debug.LogWarning($"[InventorySlot] Weapon prefab của {weaponInfo.weaponName} chưa được gán!");
            return;
        }

        if (ActiveWeapon.Instance.CurrentActiveWeapon != null)
            Destroy(ActiveWeapon.Instance.CurrentActiveWeapon.gameObject);

        GameObject weaponObj = Instantiate(weaponInfo.weaponPrefab);
        IWeapon newWeapon = weaponObj.GetComponent<IWeapon>();

        if (newWeapon == null)
        {
            Debug.LogError($"[InventorySlot] Prefab {weaponInfo.weaponPrefab.name} không có script IWeapon!");
            Destroy(weaponObj);
            return;
        }

        ActiveWeapon.Instance.NewWeapon(newWeapon as MonoBehaviour);
    }
}
