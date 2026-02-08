using System.Collections;
using UnityEngine;

public class TrailMaker : MonoBehaviour
{
    [SerializeField] private TrailRenderer _prefab;
    [SerializeField] private float _duration = 0.5f;
    [SerializeField] private AnimationCurve _speedCurve = AnimationCurve.Linear(0, 0, 1, 1);

    public void Make(Vector3 start, Vector3 end, Color color) => Make(_prefab, start, end, _duration, color);

    public void Make(TrailRenderer prefab, Vector3 start, Vector3 end, float duration, Color color)
    {
        if (prefab == null) return;

        TrailRenderer trail = Instantiate(prefab, start, Quaternion.identity);
        trail.startColor = color;
        trail.endColor = color;
        StartCoroutine(MoveTrail(trail, start, end, duration));
    }

    private IEnumerator MoveTrail(TrailRenderer trail, Vector3 start, Vector3 end, float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float factor = Mathf.Clamp01(t / duration);

            float curveFactor = _speedCurve.Evaluate(factor);

            trail.transform.position = Vector3.Lerp(start, end, curveFactor);

            yield return null;
        }

        trail.transform.position = end;

        yield return new WaitForSeconds(trail.time);

        Dispose(trail.gameObject);
    }

    private void Dispose(GameObject obj)
    {
        Destroy(obj);
    }
}