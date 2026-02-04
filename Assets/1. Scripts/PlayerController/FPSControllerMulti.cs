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
    [SerializeField] private CapsuleCollider capsule;
    
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
    
    [Header("Truck Physics")]
    [SerializeField] float truckDamping = 5f;
    
    float yaw;
    float pitch;
    float horizontalInput;
    private float verticalInput;
    
    public bool isInTruck = false;
    private Transform truckParent;

    [Header("Truck Interaction")]
    [SerializeField] float interactDistance = 5f;
    [SerializeField] LayerMask truckLayer;

    [SerializeField] float truckFollowStrength = 0.5f;
    
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            transform.GetChild(1).GetComponent<MeshRenderer>().material.color = Random.ColorHSV();
            return;
        }
        
        GameObject camObj = new GameObject("Camera of " +  gameObject.name);
        Camera cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = 60f;
        cameraTransform = camObj.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        yaw = transform.eulerAngles.y;
        pitch = cameraTransform.localEulerAngles.x;
    }
    
    private bool CheckGround()
    {
        return Physics.Raycast(groundedPoint.position, Vector3.down, distanceToGround, groundLayer);
    }

    private Vector3 lastTruckPosition;

    void Update()
    {
        if (!IsOwner) return;

        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensibility;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensibility;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, verticalLimit.x, verticalLimit.y);

        if (!isInTruck && Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        Vector3 move = Vector3.zero;

        if (isInTruck && truckRb != null)
        {
            Vector3 localMove = new Vector3(horizontalInput, 0, verticalInput).normalized;
            move = transform.TransformDirection(localMove);
        }
        else
        {
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0;
            Vector3 camRight = cameraTransform.right;
            camRight.y = 0;
            move = (camForward * verticalInput + camRight * horizontalInput).normalized;
        }

        if (isInTruck && truckRb != null)
        {
            Vector3 truckDelta = truckRb.position - lastTruckPosition;
            lastTruckPosition = truckRb.position;

            transform.position += move * moveSpeed * Time.fixedDeltaTime + truckDelta;

            transform.position += Vector3.down * 9.81f * Time.fixedDeltaTime;

            ApplyTruckBounds();
        }
        else
        {
            Vector3 velocity = move * moveSpeed;
            velocity.y = rb.linearVelocity.y;
            rb.linearVelocity = velocity;

            isGrounded = CheckGround();
        }
    }


    private void ApplyTruckBounds()
    {
        if (truckRb == null) return;
        if (capsule == null) return;

        float halfHeight = capsule.height * 0.5f;
        float radius = capsule.radius;

        Vector3 localPosition = truckRb.transform.InverseTransformPoint(transform.position);

        Vector3 min = TruckController.instance.boundsCenter - TruckController.instance.boundsSize * 0.5f;
        Vector3 max = TruckController.instance.boundsCenter + TruckController.instance.boundsSize * 0.5f;

        Vector3 pushForce = Vector3.zero;
        bool isOutOfBounds = false;

        if (localPosition.x - radius < min.x)
        {
            localPosition.x = min.x + radius;
            pushForce.x = TruckController.instance.boundsPushForce;
            isOutOfBounds = true;
        }
        else if (localPosition.x + radius > max.x)
        {
            localPosition.x = max.x - radius;
            pushForce.x = -TruckController.instance.boundsPushForce;
            isOutOfBounds = true;
        }

        if (localPosition.y - halfHeight < min.y)
        {
            localPosition.y = min.y + halfHeight;
            pushForce.y = TruckController.instance.boundsPushForce;
            isOutOfBounds = true;
        }
        else if (localPosition.y + halfHeight > max.y)
        {
            localPosition.y = max.y - halfHeight;
            pushForce.y = -TruckController.instance.boundsPushForce;
            isOutOfBounds = true;
        }

        if (localPosition.z - radius < min.z)
        {
            localPosition.z = min.z + radius;
            pushForce.z = TruckController.instance.boundsPushForce;
            isOutOfBounds = true;
        }
        else if (localPosition.z + radius > max.z)
        {
            localPosition.z = max.z - radius;
            pushForce.z = -TruckController.instance.boundsPushForce;
            isOutOfBounds = true;
        }

        if (isOutOfBounds)
        {
            Vector3 correctedWorldPosition = truckRb.transform.TransformPoint(localPosition);
            transform.position = correctedWorldPosition;

            Vector3 worldPushForce = truckRb.transform.TransformDirection(pushForce);
            rb.AddForce(worldPushForce, ForceMode.Acceleration);

            Vector3 localVelocity = truckRb.transform.InverseTransformDirection(rb.linearVelocity);

            if (Mathf.Abs(pushForce.x) > 0) localVelocity.x = 0;
            if (Mathf.Abs(pushForce.y) > 0) localVelocity.y = 0;
            if (Mathf.Abs(pushForce.z) > 0) localVelocity.z = 0;

            rb.linearVelocity = truckRb.transform.TransformDirection(localVelocity);
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
        print("GetInTruck - Requesting server to set parent");
        
        SetParentServerRpc(true);
        
        isInTruck = true;
        rb.useGravity = false;
        rb.linearDamping = 0f;
        rb.isKinematic = true;
        
        truckRb = TruckController.instance.GetComponent<Rigidbody>();
        
        NetworkTransform netTransform = GetComponent<NetworkTransform>();
        if (netTransform != null)
        {
            netTransform.InLocalSpace = true;
        }
    }

    public void GetOutTruck()
    {
        print("GetOutTruck - Requesting server to clear parent");
        
        SetParentServerRpc(false);
        
        isInTruck = false;
        truckRb = null;
        truckParent = null;
        rb.isKinematic = false;
        
        rb.useGravity = true;
        rb.linearDamping = 0f;
        
        NetworkTransform netTransform = GetComponent<NetworkTransform>();
        if (netTransform != null)
        {
            netTransform.InLocalSpace = false;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetParentServerRpc(bool setParent)
    {
        if (setParent)
        {
            Transform truckTransform = TruckController.instance.transform;
            truckParent = truckTransform.Find("PlayerParent");
            if (truckParent == null)
            {
                truckParent = truckTransform;
            }
        
            NetworkObject netObj = GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.TrySetParent(truckParent);
            }
        }
        else
        {
            NetworkObject netObj = GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.TrySetParent((Transform)null);
            }
        }
    }

}