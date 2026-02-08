using UnityEngine;

public class BanditTir : MonoBehaviour
{
    #region Inspector

    [Header("References")]
    [SerializeField] private Transform turretGun;
    [SerializeField] private Transform firePoint;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private float gunElevationSpeed = 8f;

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 60f;
    [SerializeField] private LayerMask truckPartLayer;
    [SerializeField] private LayerMask playerLayer;

    [Header("Raycast Masks (VERY IMPORTANT)")]
    [SerializeField] private LayerMask visibilityMask; // murs + player
    [SerializeField] private LayerMask shootMask;      // murs + player + truckpart

    [Header("Shooting")]
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private GameObject bulletVisualPrefab;
    [SerializeField] private float bulletVisualSpeed = 120f;

    public float damage;
    public bool isRight;

    #endregion

    private float nextFireTime;
    public BanditVehicleAI banditAI;

    private void Start()
    {
        if (banditAI.position == BanditVehicleAI.RelativePosition.Right)
            isRight = true;
    }

    private void Update()
    {
        if (Time.time < nextFireTime) return;

        Transform target = ChooseTarget();
        if (target == null) return;

        AimAt(target);
        Shoot(target);

        nextFireTime = Time.time + fireRate;
    }

    #region Target Selection

    private Transform ChooseTarget()
    {
        bool canShootPlayer =
            (isRight && TruckLife.instance.canShootPlayerRight) ||
            (!isRight && TruckLife.instance.canShootPlayerLeft);
        print(canShootPlayer + " ouaiiiii");

        if (canShootPlayer)
        {
            Transform visiblePlayer = FindClosestVisiblePlayer();
            if (visiblePlayer != null)
            {
                Debug.Log("visible player ?");
                return visiblePlayer;
            }
               
        }

        return FindClosestTruckPart();
    }

    private Transform FindClosestVisiblePlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, playerLayer);

        Transform closest = null;
        float minDist = Mathf.Infinity;

        foreach (var hit in hits)
        {
            Debug.Log("jai trouvé qqn ?");
            HealthComponent health = hit.GetComponent<HealthComponent>();
            if (health.Invulnerable) continue;

            Debug.Log("test vision");
            if (!HasLineOfSight(hit.transform)) continue;

            float dist = (hit.transform.position - transform.position).sqrMagnitude;
            if (dist < minDist)
            {
                minDist = dist;
                closest = hit.transform;
            }
        }
        Debug.Log("closest j'ai trouvé");
        return closest;
    }

    private Transform FindClosestTruckPart()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, truckPartLayer);

        Transform closest = null;
        float minDist = Mathf.Infinity;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("TruckPart"))
            {
                TruckPart part = hit.GetComponent<TruckPart>();
                if (part.isBroke.Value) continue;

                float dist = (hit.transform.position - transform.position).sqrMagnitude;
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = hit.transform;
                }
            }
        }

        return closest;
    }

    private bool HasLineOfSight(Transform target)
    {
        Vector3 origin = firePoint.position;
        Vector3 dir = (target.position - origin).normalized;

        RaycastHit hit;
        if (Physics.Raycast(origin, dir, out hit, detectionRadius, visibilityMask, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("visible");
            return hit.transform == target;
        }

        Debug.Log("obstrué " + hit.collider.name);
        return false;
    }

    #endregion

    #region Aiming

    private void AimAt(Transform target)
    {
        Vector3 dir = target.position - firePoint.position;

        Vector3 flatDir = new Vector3(dir.x, 0f, dir.z);
        Quaternion baseRot = Quaternion.LookRotation(flatDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, baseRot, rotationSpeed * Time.deltaTime);

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
        Vector3 hitPoint = origin + dir * detectionRadius;

        if (Physics.Raycast(origin, dir, out hit, detectionRadius, shootMask, QueryTriggerInteraction.Ignore))
        {
            hitPoint = hit.point;

            if (hit.collider.CompareTag("Player"))
            {
                hit.collider.GetComponent<HealthComponent>().TryTakeDamage(damage);
            }
            else if (hit.collider.CompareTag("TruckPart"))
            {
                hit.collider.GetComponent<TruckPart>().TakeDamage(damage);
            }
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
