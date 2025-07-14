using System.Collections;
using UnityEngine;

public class Flash : MonoBehaviour
{
    [SerializeField] private Material whileFlashMat;
    [SerializeField] private float restoreDefaultMatTime = 0.2f;
    private Material defaultMat;
    private SpriteRenderer spriteRenderer;
    private EnemyHealth enemyHealth;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyHealth = GetComponent<EnemyHealth>();
        defaultMat = spriteRenderer.material;
    }
    public IEnumerator FlashRoutine()
    {
        spriteRenderer.material = whileFlashMat;
        yield return new WaitForSeconds(restoreDefaultMatTime);
        spriteRenderer.material = defaultMat;
        enemyHealth.DetectDeath();
    }
}
