using UnityEngine;

public class BanditTir : MonoBehaviour
{
    #region Inspector

    [Header("References")]
    [SerializeField] private Transform turretGun;     // X rotation
    [SerializeField] private Transform firePoint;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private float gunElevationSpeed = 8f;

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 60f;
    [SerializeField] private LayerMask targetLayer;   // Layer "Partie"

    [Header("Shooting")]
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private GameObject bulletVisualPrefab;
    [SerializeField] private float bulletVisualSpeed = 120f;

    public float damage;

    #endregion

    private float nextFireTime;

    private void Update()
    {
        if (Time.time < nextFireTime) return;

        Transform target = FindClosestTarget();
        if (target == null) return;

        AimAt(target);

        Shoot(target);

        nextFireTime = Time.time + fireRate;
    }

    #region Targeting

    private Transform FindClosestTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, targetLayer);

        Transform closest = null;
        float minDist = Mathf.Infinity;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("TruckPart"))
            {
                if (!hit.GetComponent<TruckPart>().isBroke.Value)
                {
                    float dist = (hit.transform.position - transform.position).sqrMagnitude;
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closest = hit.transform;
                    }
                }
                
            }
        }
           
        return closest;
    }

    #endregion

    #region Aiming

    private void AimAt(Transform target)
    {
        Vector3 dir = target.position - firePoint.position;

        // Rotation Y (base)
        Vector3 flatDir = new Vector3(dir.x, 0f, dir.z);
        Quaternion baseRot = Quaternion.LookRotation(flatDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, baseRot, rotationSpeed * Time.deltaTime);

        // Rotation X (gun)
        Quaternion gunRot = Quaternion.LookRotation(dir);
        turretGun.rotation = Quaternion.Slerp(turretGun.rotation, gunRot, gunElevationSpeed * Time.deltaTime);
    }

    #endregion

    #region Shooting

    private void Shoot(Transform target)
    {
        Vector3 origin = firePoint.position;
        Vector3 dir = (target.position - origin).normalized;

        RaycastHit hit;
        Vector3 hitPoint;
        if (Physics.Raycast(origin, dir, out hit, detectionRadius))
        {
            hitPoint = hit.point;
            target.GetComponent<TruckPart>().TakeDamage(damage);
            // Plus tard : dégâts
            // var health = hit.collider.GetComponent<Health>();
            // if (health) health.TakeDamage(10);
        }
        else
        {
            hitPoint = origin + dir * detectionRadius;
        }

        SpawnVisualBullet(origin, hitPoint);
    }

    private void SpawnVisualBullet(Vector3 start, Vector3 end)
    {
        GameObject bullet = Instantiate(bulletVisualPrefab, start, Quaternion.identity);
        bullet.GetComponent<BulletVisual>().Init(end, bulletVisualSpeed);
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
