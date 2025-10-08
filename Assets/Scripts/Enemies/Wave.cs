using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "New Wave", menuName = "Wave")]
public class Wave : ScriptableObject
{
    public string waveName;
    public List<GameObject> enemyPrefabs;
    public int enemyCount = 5;
    public float spawnDelay = 1f;

    [Header("UI Settings")]
    public Color waveColor = Color.white;
    public bool isBossWave = false;
}