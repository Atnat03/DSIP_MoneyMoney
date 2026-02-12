using Unity.Netcode;
using UnityEngine;

public class BulletVisual : MonoBehaviour
{
    private Vector3 target;
    private float speed;
    private string targetTag;

    public void Init(Vector3 targetPoint, float moveSpeed, string hitTag)
    {
        target = targetPoint;
        speed = moveSpeed;
        targetTag = hitTag;
        transform.LookAt(target);
        Destroy(gameObject, 3f);
    }

    private void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            if (targetTag == "Player")
            {
                SFX_Manager.instance.PlaySFX(6 + Random.Range(0, 2));
            }
            else if (targetTag == "TruckPart")
            {
                SFX_Manager.instance.PlaySFX(8,.3f);
            }
            
            Destroy(gameObject);
        }
    }
}