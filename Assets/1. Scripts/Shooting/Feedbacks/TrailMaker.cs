using UnityEngine;

public class TrailMaker : MonoBehaviour
{
    [SerializeField] private LineRenderer _prefab;
    [SerializeField] private float _duration = 0.5f;

    public void Make(Vector3 start, Vector3 end) => Make(_prefab, start, end, _duration);
    public void Make(LineRenderer prefab, Vector3 start, Vector3 end, float duration)
    {
        if (prefab != null)
        {
            LineRenderer trail = GameObject.Instantiate(prefab);
            trail.transform.position = Vector3.zero;
            Vector3[] positions = new Vector3[2];
            positions[0] = start;
            positions[1] = end;
            trail.SetPositions(positions);
            this.WaitAndDo(duration, () => Dispose(trail.gameObject));
        }
    }

    private void Dispose(GameObject obj)
    {
        Destroy(obj);
    }
}