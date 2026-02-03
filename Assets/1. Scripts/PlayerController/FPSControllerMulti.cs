using System;
using Unity.Netcode;
using UnityEngine;

public class FPSControllerMulti : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] Rigidbody rb;
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
    
    float yaw;
    float pitch;
    float horizontalInput;
    private float verticalInput;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        
        GameObject camObj = new GameObject("Camera of " +  gameObject.name);
        Camera cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = 60f;
        cameraTransform = camObj.transform;

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
        
        Vector3 move = (transform.forward * verticalInput + transform.right * horizontalInput).normalized;
        Vector3 velocity = move * moveSpeed;
        
        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;
        
        isGrounded = CheckGround();
    }

    void LateUpdate()
    {
        if (!IsOwner) return;
        
        transform.rotation = Quaternion.Euler(0, yaw, 0);
        cameraTransform.rotation = Quaternion.Euler(pitch, yaw, 0);
        
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, cameraTarget.position, Time.deltaTime * cameraSmoothFollow);
    }
}
