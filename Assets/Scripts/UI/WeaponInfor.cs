using UnityEngine;

[CreateAssetMenu(menuName = "Weapon Information")]
public class WeaponInfor : ScriptableObject
{
    public GameObject weaponPrefab;
    public float weaponCooldown;
    public int weaponDamage;
    public float weaponRange;
}
