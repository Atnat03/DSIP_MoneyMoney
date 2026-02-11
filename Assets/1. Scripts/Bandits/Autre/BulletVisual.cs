using Unity.Netcode;
using UnityEngine;

public class BulletVisual : MonoBehaviour
{
    private Vector3 target;
    private float speed;

    public void Init(Vector3 targetPoint, float moveSpeed)
    {
        target = targetPoint;
        speed = moveSpeed;
        transform.LookAt(target);
        Destroy(gameObject, 3f);
    }

    private void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            Destroy(gameObject);
        }
    }
}