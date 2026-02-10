using Unity.Netcode;
using UnityEngine;

public class HelicopterVehicleAI : MonoBehaviour, IVehicule
{
    [Header("Target")]
    public Transform truck;
    
    [Header("Positioning")]
    [Tooltip("Hauteur fixe de l'hélico au-dessus du camion")]
    public float flightHeight = 20f;
    [Tooltip("Décalage vers l'arrière par rapport au camion")]
    public float backOffset = 10f;
    [Tooltip("Décalage latéral du camion")]
    public float sideOffset = 0f; // pour placer hélico légèrement sur le côté si nécessaire
    [Tooltip("Distance à laquelle l'hélico considère qu'il est arrivé")]
    public float arrivalThreshold = 1f;

    [Header("Speed Settings")]
    public float maxSpeed = 25f;
    public float acceleration = 8f;
    public float deceleration = 10f;

    [Header("Stop Behavior")]
    public float hoverDamping = 3f; // pour vol stationnaire doux

    private Vector3 currentVelocity;
    private bool isStopping = false;

    private Vector3 lastMoveDirection = Vector3.forward; // direction persistante

    public float currentSpeed => currentVelocity.magnitude;

    void Update()
    {
        if (truck == null) return;

        Vector3 targetPos = truck.position
                            - truck.forward * backOffset
                            + truck.right * sideOffset
                            + Vector3.up * flightHeight;

        Vector3 toTarget = targetPos - transform.position;
        Rigidbody truckRb = truck.GetComponent<Rigidbody>();
        float truckSpeed = truckRb != null ? truckRb.linearVelocity.magnitude : 0f;

        // Décider si on doit hover ou voler
        if (toTarget.magnitude <= arrivalThreshold && truckSpeed < 0.1f)
        {
            if (!isStopping) StopVehicle();
        }
        else
        {
            if (isStopping) isStopping = false; // reprendre le vol si le camion bouge
            MoveTowardsTarget(targetPos);
        }

        if (isStopping)
        {
            HoverStationary();
        }
    }

    void MoveTowardsTarget(Vector3 targetPos)
    {
        // Interpolation pour mouvement fluide
        Vector3 toTarget = targetPos - transform.position;
        Vector3 desiredVelocity = toTarget.normalized * maxSpeed;
        currentVelocity = Vector3.Lerp(currentVelocity, desiredVelocity, Time.deltaTime * acceleration);

        transform.position += currentVelocity * Time.deltaTime;

        // Calcul de direction pour rotation
        Vector3 moveDir = currentVelocity;
        if (moveDir.sqrMagnitude > 0.01f)
        {
            lastMoveDirection = moveDir.normalized; // mémoriser la dernière direction significative
        }

        // Si on bouge peu, on regarde directement le camion
        Vector3 lookTarget = (toTarget.sqrMagnitude > 0.1f) ? lastMoveDirection : (truck.position - transform.position).normalized;

        // Garder hélico horizontalement
        lookTarget.y = 0;

        if (lookTarget.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookTarget);

            // Inclinaison avant proportionnelle à la vitesse
            float speedFactor = Mathf.Clamp(currentVelocity.magnitude / maxSpeed, 0f, 1f);
            float pitchAngle = speedFactor * 15f; // positif = penche vers l'avant
            Vector3 euler = targetRotation.eulerAngles;
            euler.x = pitchAngle;
            targetRotation = Quaternion.Euler(euler);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
        }
    }

    #region Hover / Stop

    public void StopVehicle()
    {
        isStopping = true;
        currentVelocity = Vector3.zero;
    }

    void HoverStationary()
    {
        // Maintenir la position actuelle avec interpolation douce
        transform.position = Vector3.Lerp(transform.position, transform.position, Time.deltaTime * hoverDamping);
    }

    #endregion

    public BanditTir tir;

    public void Die()
    {
        GetComponent<Helicopter>().Die();
        tir.enabled = false;
        this.enabled = false;
    }

    // Debug visuel
    void OnDrawGizmosSelected()
    {
        if (truck == null) return;

        Vector3 targetPos = truck.position
                            - truck.forward * backOffset
                            + truck.right * sideOffset
                            + Vector3.up * flightHeight;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(targetPos, 1f);
        Gizmos.DrawLine(transform.position, targetPos);
    }
}
