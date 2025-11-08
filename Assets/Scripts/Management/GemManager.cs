using System;
using UnityEngine;

public class GemManager : MonoBehaviour
{
    public static GemManager Instance { get; private set; }

    private int currentGems = 0;
    public event Action<int> OnGemsChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public int CurrentGems => currentGems;

    public void AddGems(int amount)
    {
        if (amount <= 0) return;
        currentGems += amount;
        OnGemsChanged?.Invoke(currentGems);
    }

    public GameObject SpawnGemPrefab(GameObject gemPrefab, Vector3 worldPos, int amount = 1)
    {
        if (gemPrefab == null) return null;
        var go = Instantiate(gemPrefab, worldPos, Quaternion.identity);
        var gp = go.GetComponent<GemPickup>();
        if (gp != null) gp.SetAmount(amount);
        return go;
    }
}