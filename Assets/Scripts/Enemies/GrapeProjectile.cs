using System.Collections;
using UnityEngine;

public class GrapeProjectile : MonoBehaviour
{
    [SerializeField] private float duration = 1f;
    [SerializeField] private AnimationCurve animCurve;
    [SerializeField] private float heightY = 3f;
    [SerializeField] private GameObject grapeProjectileShadow;
    [SerializeField] private GameObject splatterPrefab;

    private void Start()
    {
        GameObject grapeShadow = Instantiate(grapeProjectileShadow, transform.position + new Vector3(0, -0.3f, 0), Quaternion.identity);

        Vector3 playerPos = PlayerController.Instance.transform.position;
        Vector3 grapeShadowStartPos = grapeShadow.transform.position;

        StartCoroutine(ProjectileCurveRoutine(transform.position, playerPos));
        StartCoroutine(MoveGrapeShadowRoutine(grapeShadow, grapeShadowStartPos, playerPos));
    }
    private IEnumerator ProjectileCurveRoutine(Vector3 startPosition, Vector3 endPosition)
    {
        float timeElapsed = 0f;
        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            float linearT = timeElapsed / duration;
            float heightT = animCurve.Evaluate(linearT);
            float height = Mathf.Lerp(0f, heightY, heightT);
            // Lấy vị trí người chơi mỗi frame
            Vector3 currentPlayerPos = PlayerController.Instance.transform.position;
            transform.position = Vector2.Lerp(startPosition, endPosition, linearT) + new Vector2(0f, height);
            yield return null;
        }
        Instantiate(splatterPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
    private IEnumerator MoveGrapeShadowRoutine(GameObject grapeShadow, Vector3 startPosition, Vector3 endPosition)
    {
        float timeElapsed = 0f;
        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            float linearT = timeElapsed / duration;
            grapeShadow.transform.position = Vector2.Lerp(startPosition, endPosition, linearT);
            yield return null;
        }
        Destroy(grapeShadow);
    }
}
