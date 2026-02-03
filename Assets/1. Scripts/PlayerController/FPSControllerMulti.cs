using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using Random = UnityEngine.Random;

public class FPSControllerMulti : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] Rigidbody rb;
    private Rigidbody truckRb;
    [SerializeField] Transform cameraTarget;
    Transform cameraTransform;
    
    [Header("Move Settings")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField, Range(0.1f, 10)] float mouseSensibility;
    [SerializeField] Vector2 verticalLimit = new Vector2(-80, 80);
    [SerializeField] float cameraSmoothFollow = 15f;
    
    [Header("Jump Settings")]
    [SerializeField] private Transform groundedPoint;
    [SerializeField] private float distanceToGround;
    [SerializeField] private float jumpForce = 10;
    [SerializeField] bool isGrounded = false;
    [SerializeField] LayerMask groundLayer;
    
    [Header("Truck Bounds (Local Space)")]
    [SerializeField] Vector3 truckBoundsMin = new Vector3(-2f, 0f, -3f);
    [SerializeField] Vector3 truckBoundsMax = new Vector3(2f, 3f, 3f);
    
    float yaw;
    float pitch;
    float horizontalInput;
    private float verticalInput;
    
    public bool isInTruck = false;
    private Vector3 lastTruckPosition;
    private Quaternion lastTruckRotation;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            transform.GetChild(1).GetComponent<MeshRenderer>().material.color = Random.ColorHSV();
            return;
        }
        
        NetworkTransform netTransform = GetComponent<NetworkTransform>();
        if (netTransform != null)
        {
            netTransform.InLocalSpace = false; // TOUJOURS en world space
        }
        
        GameObject camObj = new GameObject("Camera of " +  gameObject.name);
        Camera cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = 60f;
        cameraTransform = camObj.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        rb.mass = 70f; 
        rb.linearDamping = 0f; 
        rb.angularDamping = 0.05f;
    
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // IMPORTANT
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    
        rb.sleepThreshold = 0f;
        rb.maxDepenetrationVelocity = 2f; // Augmenté pour mieux gérer les collisions
        
        // Configure le CapsuleCollider
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            capsule.radius = 0.3f;
            capsule.height = 1.8f;
            capsule.center = new Vector3(0, 0.9f, 0);
        }

        yaw = transform.eulerAngles.y;
        pitch = cameraTransform.localEulerAngles.x;
    }

    private void Update()
    {
        if (!IsOwner) return;
        
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensibility;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensibility;
        
        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, verticalLimit.x, verticalLimit.y);

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Debug.Log("Jump");
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private bool CheckGround()
    {
        return Physics.Raycast(groundedPoint.position, Vector3.down, distanceToGround, groundLayer);
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;
    
        if (truckRb != null && isInTruck)
        {
            // Calcule le mouvement du camion depuis la dernière frame
            Vector3 truckDeltaPosition = truckRb.position - lastTruckPosition;
            Quaternion truckDeltaRotation = truckRb.rotation * Quaternion.Inverse(lastTruckRotation);
            
            // Applique le mouvement du camion au joueur
            // Cela fait bouger le joueur avec le camion tout en gardant la physique active
            rb.MovePosition(rb.position + truckDeltaPosition);
            
            // Applique la rotation du camion autour de son centre
            Vector3 offset = rb.position - truckRb.position;
            offset = truckDeltaRotation * offset;
            rb.MovePosition(truckRb.position + offset);
            
            // Mouvement relatif du joueur DANS le camion
            Vector3 move = (transform.forward * verticalInput + transform.right * horizontalInput).normalized;
            Vector3 targetVelocity = move * moveSpeed;
            
            // Convertit la vélocité cible en espace local du camion
            Vector3 localTargetVel = TruckController.instance.transform.InverseTransformDirection(targetVelocity);
            targetVelocity = TruckController.instance.transform.TransformDirection(localTargetVel);
            
            Vector3 currentVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            Vector3 velocityDiff = targetVelocity - currentVelocity;
            
            rb.AddForce(velocityDiff * rb.mass * 15f, ForceMode.Force);
            
            // Applique les contraintes de position (bounds)
            ClampPositionInTruck();
            
            // Sauvegarde la position/rotation du camion pour la prochaine frame
            lastTruckPosition = truckRb.position;
            lastTruckRotation = truckRb.rotation;
        }
        else
        {
            // HORS DU CAMION : Contrôle direct
            Vector3 move = (transform.forward * verticalInput + transform.right * horizontalInput).normalized;
            Vector3 velocity = move * moveSpeed;
            velocity.y = rb.linearVelocity.y;
            rb.linearVelocity = velocity;
        }
    
        isGrounded = CheckGround();
    }

    void ClampPositionInTruck()
    {
        if (TruckController.instance == null) return;
        
        // Convertit la position du joueur en espace local du camion
        Vector3 localPos = TruckController.instance.transform.InverseTransformPoint(rb.position);
        
        // Clamp dans les bounds
        Vector3 clampedLocalPos = new Vector3(
            Mathf.Clamp(localPos.x, truckBoundsMin.x, truckBoundsMax.x),
            Mathf.Clamp(localPos.y, truckBoundsMin.y, truckBoundsMax.y),
            Mathf.Clamp(localPos.z, truckBoundsMin.z, truckBoundsMax.z)
        );
        
        // Si la position a été clampée, repositionne le joueur
        if (Vector3.Distance(localPos, clampedLocalPos) > 0.01f)
        {
            Vector3 clampedWorldPos = TruckController.instance.transform.TransformPoint(clampedLocalPos);
            rb.position = clampedWorldPos;
            
            // Réduit la vélocité dans la direction du mur
            Vector3 normalizedDiff = (localPos - clampedLocalPos).normalized;
            Vector3 worldNormal = TruckController.instance.transform.TransformDirection(normalizedDiff);
            
            float velocityInNormalDirection = Vector3.Dot(rb.linearVelocity, worldNormal);
            if (velocityInNormalDirection > 0)
            {
                rb.linearVelocity -= worldNormal * velocityInNormalDirection * 0.8f;
            }
        }
    }

    void LateUpdate()
    {
        if (!IsOwner) return;
        
        transform.rotation = Quaternion.Euler(0, yaw, 0);
        cameraTransform.rotation = Quaternion.Euler(pitch, yaw, 0);
        
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, cameraTarget.position, Time.deltaTime * cameraSmoothFollow);
    }

    public void GetInTruck()
    {
        isInTruck = true;
        truckRb = TruckController.instance.GetComponent<Rigidbody>();
        
        if (truckRb != null)
        {
            lastTruckPosition = truckRb.position;
            lastTruckRotation = truckRb.rotation;
        }
    }

    public void GetOutTruck()
    {
        truckRb = null;
        isInTruck = false;
    }
}