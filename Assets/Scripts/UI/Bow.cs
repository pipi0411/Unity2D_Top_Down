using UnityEngine;
using System.Collections;

public class Bow : MonoBehaviour, IWeapon
{
    public void Attack()
    {
        Debug.Log("Bow attack executed.");
        ActiveWeapon.Instance.ToggleIsAttacking(false);
    }
}