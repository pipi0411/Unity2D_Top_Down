// Scripts/Flash.cs
using System.Collections;
using UnityEngine;

public class Flash : MonoBehaviour
{
    [SerializeField] private EnemyData enemyData;

    private Material defaultMat;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        defaultMat = spriteRenderer.material;
    }

    public IEnumerator FlashRoutine()
    {
        if (enemyData.flashMaterial != null)
        {
            spriteRenderer.material = enemyData.flashMaterial;
            yield return new WaitForSeconds(enemyData.flashRestoreTime);
            spriteRenderer.material = defaultMat;
        }
    }
}
