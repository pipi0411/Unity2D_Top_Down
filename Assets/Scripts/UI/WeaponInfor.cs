using UnityEngine;

[CreateAssetMenu(menuName = "Weapon Information")]
public class WeaponInfor : ScriptableObject
{
    [Header("Weapon Basic Info")]
    public string weaponName;         
    public GameObject weaponPrefab;
    public float weaponCooldown;
    public int weaponDamage;
    public float weaponRange;

    [Header("Audio Settings")]
    public string attackSoundName;    
}
