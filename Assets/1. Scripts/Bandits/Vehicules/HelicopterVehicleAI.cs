using Unity.Netcode;
using UnityEngine;

public class HelicopterVehicleAI : MonoBehaviour, IVehicule
{
    [Header("Target")]
    public Transform truck;

    [Header("Positioning")]
    public float flightHeight = 20f;
    public float backOffset = 10f;
    public float sideOffset = 0f;

    [Header("Speed Settings")]
    public float maxSpeed = 25f;
    public float acceleration = 8f;

    [Header("Follow Behavior")]
    [Tooltip("Distance de confort autour de la position désirée")]
    public float followRadius = 6f;

    [Tooltip("Temps d’anticipation du mouvement du camion")]
    public float predictionTime = 0.6f;

    [Header("Hover")]
    public float hoverDamping = 3f;

    private Vector3 currentVelocity;
    private Vector3 lastMoveDirection = Vector3.forward;

    public float currentSpeed => currentVelocity.magnitude;

    void Update()
    {
        if (truck == null) return;

        Rigidbody truckRb = truck.GetComponent<Rigidbody>();
        float truckSpeed = truckRb != null ? truckRb.linearVelocity.magnitude : 0f;

        if (truckSpeed < 0.1f && currentVelocity.magnitude < 0.5f)
        {
            HoverStationary();
        }
        else
        {
            MoveTowardsTruck(truckRb);
        }
    }

    void MoveTowardsTruck(Rigidbody truckRb)
    {
        Vector3 truckVel = truckRb != null ? truckRb.linearVelocity : Vector3.zero;

        // 🔮 Anticipation (Pursuit)
        Vector3 predictedTruckPos = truck.position + truckVel * predictionTime;

        // Position désirée relative au camion
        Vector3 desiredPos = predictedTruckPos
                            - truck.forward * backOffset
                            + truck.right * sideOffset
                            + Vector3.up * flightHeight;

        Vector3 toDesired = desiredPos - transform.position;
        float distance = toDesired.magnitude;

        // 🧠 Zone de suivi (évite l’arrivée brutale)
        float speedFactor = Mathf.Clamp01(distance / followRadius);
        Vector3 desiredVelocity = toDesired.normalized * maxSpeed * speedFactor;

        currentVelocity = Vector3.Lerp(currentVelocity, desiredVelocity, Time.deltaTime * acceleration);
        transform.position += currentVelocity * Time.deltaTime;

        HandleRotation(speedFactor);
    }

    void HandleRotation(float speedFactor)
    {
        Vector3 moveDir = currentVelocity;

        if (moveDir.sqrMagnitude > 0.01f)
            lastMoveDirection = moveDir.normalized;

        Vector3 lookTarget = lastMoveDirection;
        lookTarget.y = 0;

        if (lookTarget.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookTarget);

            float pitchAngle = speedFactor * 15f;
            Vector3 euler = targetRotation.eulerAngles;
            euler.x = pitchAngle;

            targetRotation = Quaternion.Euler(euler);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
        }
    }

    void HoverStationary()
    {
        currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, Time.deltaTime * hoverDamping);
        transform.position += currentVelocity * Time.deltaTime;
    }

    #region Death

    public BanditTir tir;

    public void Die()
    {
        GetComponent<Helicopter>().Die();
        tir.enabled = false;
        this.enabled = false;
    }

    #endregion

    // Debug visuel
    void OnDrawGizmosSelected()
    {
        if (truck == null) return;

        Gizmos.color = Color.cyan;

        Vector3 gizmoPos = truck.position
                          - truck.forward * backOffset
                          + truck.right * sideOffset
                          + Vector3.up * flightHeight;

        Gizmos.DrawWireSphere(gizmoPos, 1f);
        Gizmos.DrawLine(transform.position, gizmoPos);
    }
}
