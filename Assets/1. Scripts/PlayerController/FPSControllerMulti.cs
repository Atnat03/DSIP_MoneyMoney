using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using Random = UnityEngine.Random;

public class FPSControllerMulti : NetworkBehaviour
{
    [Header("References")]
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
    [SerializeField, Range(0, 1f)] float truckFollowStrength = 0.5f;
    
    private TruckInteraction nearbyTruck;
    public bool isDriver = false;
    private Vector3 driverLocalPosition;
    
    [Header("UI")]
    public GameObject ui;

    public GameObject textGoInCamion;
    
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            transform.GetChild(1).GetComponent<MeshRenderer>().material.color = Random.ColorHSV();
            ui.SetActive(false);
            return;
        }
        
        GameObject camObj = new GameObject("Camera of " +  gameObject.name);
        Camera cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = 60f;
        cameraTransform = camObj.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        nearbyTruck = TruckController.instance.GetComponent<TruckInteraction>();

        yaw = transform.eulerAngles.y;
        pitch = cameraTransform.localEulerAngles.x;
    }

    private Vector3 lastTruckPosition;

    public CharacterController controller;
    public bool canEnterInTruck = false;

    void Update()
    {
        if (!IsOwner) return;
        
        textGoInCamion.SetActive(canEnterInTruck);
        
        if (Input.GetKeyDown(KeyCode.E) && (canEnterInTruck ||isInTruck))
        {
            if (!isInTruck)
            {
                canEnterInTruck = false;
                TruckController.instance.GetComponent<TruckInteraction>().TryEnterTruck(this);
            }
            else if (isInTruck)
            {
                print("TryExitTruck");
                nearbyTruck.TryExitTruck(this);
            }
        }

        if (isDriver && isInTruck)
        {
            if (TruckController.instance != null)
            {
                transform.position = TruckController.instance.driverPos.position;
            }
            
            HandleCameraInput();
            return;
        }

        if (!isInTruck || !isDriver)
        {
            HandleMovement();
        }
    }

    void HandleCameraInput()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensibility;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensibility;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, verticalLimit.x, verticalLimit.y);
    }

    void HandleMovement()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensibility;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensibility;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, verticalLimit.x, verticalLimit.y);

        if (Input.GetKeyDown(KeyCode.Space) && controller.isGrounded)
        {
            controller.Move(Vector3.up * jumpForce * Time.deltaTime);
        }

        Vector3 move = Vector3.zero;
        Vector3 localMove = new Vector3(horizontalInput, 0, verticalInput).normalized;
        move = transform.TransformDirection(localMove) * moveSpeed * Time.deltaTime;

        if (isInTruck && truckRb != null)
        {
            Vector3 truckDelta = truckRb.position - lastTruckPosition;
            lastTruckPosition = truckRb.position;
            move += truckDelta * truckFollowStrength;
        }

        move -= Vector3.up * 9.81f * Time.deltaTime;
        controller.Move(move);
    }

    void LateUpdate()
    {
        if (!IsOwner) return;
        
        transform.rotation = Quaternion.Euler(0, yaw, 0);
        cameraTransform.rotation = Quaternion.Euler(pitch, yaw, 0);
        
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, cameraTarget.position, Time.deltaTime * cameraSmoothFollow);
    }

    public void EnterTruck(bool asDriver, Vector3 spawnPosition)
    {
        print($"EnterTruck - asDriver: {asDriver}");
        
        isInTruck = true;
        isDriver = asDriver;
        
        SetParentServerRpc(true);
        
        transform.localPosition = spawnPosition;

        if (isDriver)
        {
            driverLocalPosition = TruckController.instance.driverPos.position;

            TruckController.instance.enabled = true;

            if (controller != null)
                controller.enabled = false;
        }

        truckRb = TruckController.instance.GetComponent<Rigidbody>();
        lastTruckPosition = truckRb.position;

        NetworkTransform netTransform = GetComponent<NetworkTransform>();
        if (netTransform != null)
            netTransform.InLocalSpace = true;
    }
    
    public void ExitTruck(Vector3 exitPosition)
    {
        print("ExitTruck");
        
        SetParentServerRpc(false);
        
        transform.position = exitPosition;
        
        isInTruck = false;
        isDriver = false;
        truckRb = null;
        
        if (controller != null)
            controller.enabled = true;

        NetworkTransform netTransform = GetComponent<NetworkTransform>();
        if (netTransform != null)
            netTransform.InLocalSpace = false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetParentServerRpc(bool setParent)
    {
        if (setParent)
        {
            Transform truckTransform = TruckController.instance.transform;
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

    public void OnTriggerEnter(Collider other)
    {
        if (other.transform.CompareTag("PorteConducteur") && !isInTruck)
        {
            canEnterInTruck = true;
        }
    }
    
    public void OnTriggerExit(Collider other)
    {
        if (other.transform.CompareTag("PorteConducteur"))
        {
            canEnterInTruck = false;
        }
    }
}