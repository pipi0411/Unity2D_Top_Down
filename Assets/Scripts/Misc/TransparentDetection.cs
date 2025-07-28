using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TransparentDetection : MonoBehaviour
{
    [Range(0, 1)]
    [SerializeField] private float transparencyAmount = 0.8f;
    [SerializeField] private float fadeTime = 0.4f;
    private SpriteRenderer spriteRenderer;
    private Tilemap tilemap;
    
    // ✅ Track active coroutines để có thể stop chúng
    private Coroutine fadeCoroutine;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        tilemap = GetComponent<Tilemap>();
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.GetComponent<PlayerController>())
        {
            // ✅ Kiểm tra GameObject còn active không
            if (!gameObject.activeInHierarchy) return;
            
            // ✅ Stop coroutine cũ nếu đang chạy
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            
            if (spriteRenderer)
            {
                fadeCoroutine = StartCoroutine(FadeRoutine(spriteRenderer, fadeTime, spriteRenderer.color.a, transparencyAmount));
            }
            else if (tilemap)
            {
                fadeCoroutine = StartCoroutine(FadeRoutine(tilemap, fadeTime, tilemap.color.a, transparencyAmount));
            }
        }
    }
    
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.GetComponent<PlayerController>())
        {
            // ✅ Kiểm tra GameObject còn active không
            if (!gameObject.activeInHierarchy) return;
            
            // ✅ Stop coroutine cũ nếu đang chạy
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            
            if (spriteRenderer)
            {
                fadeCoroutine = StartCoroutine(FadeRoutine(spriteRenderer, fadeTime, spriteRenderer.color.a, 1f));
            }
            else if (tilemap)
            {
                fadeCoroutine = StartCoroutine(FadeRoutine(tilemap, fadeTime, tilemap.color.a, 1f));
            }
        }
    }
    
    // ✅ Cleanup khi GameObject bị disable
    private void OnDisable()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        
        // ✅ Reset về trạng thái ban đầu
        ResetTransparency();
    }
    
    private void ResetTransparency()
    {
        if (spriteRenderer)
        {
            Color color = spriteRenderer.color;
            spriteRenderer.color = new Color(color.r, color.g, color.b, 1f);
        }
        else if (tilemap)
        {
            Color color = tilemap.color;
            tilemap.color = new Color(color.r, color.g, color.b, 1f);
        }
    }
    
    private IEnumerator FadeRoutine(SpriteRenderer spriteRenderer, float fadeTime, float startValue, float targetTransparency)
    {
        float elapsedTime = 0;

        while (elapsedTime < fadeTime)
        {
            // ✅ Kiểm tra GameObject vẫn còn active trong lúc fade
            if (!gameObject.activeInHierarchy) yield break;
            
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startValue, targetTransparency, elapsedTime / fadeTime);
            spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, newAlpha);
            yield return null;
        }
        
        // ✅ Đảm bảo alpha chính xác ở cuối
        spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, targetTransparency);
        fadeCoroutine = null;
    }
    
    private IEnumerator FadeRoutine(Tilemap tilemap, float fadeTime, float startValue, float targetTransparency)
    {
        float elapsedTime = 0;
        
        while (elapsedTime < fadeTime)
        {
            // ✅ Kiểm tra GameObject vẫn còn active trong lúc fade
            if (!gameObject.activeInHierarchy) yield break;
            
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startValue, targetTransparency, elapsedTime / fadeTime);
            tilemap.color = new Color(tilemap.color.r, tilemap.color.g, tilemap.color.b, newAlpha);
            yield return null;
        }
        
        // ✅ Đảm bảo alpha chính xác ở cuối
        tilemap.color = new Color(tilemap.color.r, tilemap.color.g, tilemap.color.b, targetTransparency);
        fadeCoroutine = null;
    }
}