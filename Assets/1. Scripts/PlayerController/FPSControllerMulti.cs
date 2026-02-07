using System;
using System.Numerics;
using Shooting;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;
using Vector2 = UnityEngine.Vector2;

[DefaultExecutionOrder(-1)]
public class FPSControllerMulti : NetworkBehaviour
{
    [Header("References")]
    private Rigidbody truckRb;
    [SerializeField] Transform cameraTarget;
    [SerializeField] Transform cameraTransform;
    [SerializeField] Camera myCamera;
    [SerializeField] private CapsuleCollider capsule;
    
    [Header("Move Settings")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float sprintSpeed = 12f;
    private float speed;
    [SerializeField, Range(0.1f, 10)] float mouseSensibility;
    [SerializeField] Vector2 verticalLimit = new Vector2(-80, 80);
    [SerializeField] float cameraSmoothFollow = 15f;
    [SerializeField] float gravity = -9.8f;
    
    [Header("Jump Settings")]
    [SerializeField] private Transform groundedPoint;
    [SerializeField] private float distanceToGround;
    [SerializeField] private float jumpForce = 10;
    [SerializeField] bool isGrounded = false;
    [SerializeField] LayerMask groundLayer;
    private float verticalVelocity;

    [Header("Headbob Settings")]
    [SerializeField, Range(0, 0.2f)] float _amplitude = 0.05f;
    [SerializeField, Range(0, 30)] float frequency = 10f;
    [SerializeField] private Vector3 startPos;
    [SerializeField, Range(0, 2f)] float sprintAmplitudeMultiplier = 1.5f;
    [SerializeField, Range(0, 2f)] float sprintFrequencyMultiplier = 1.5f;

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
    public GameObject textReload;
    public LayerMask maskCameraPlayer;

    private bool canReload = false;
    ShooterComponent shooter;
    
    public GameObject meshRenderer;

    public bool canSit = false;
    public bool isSitting = false;
    private Transform sittingPos;
    
    public Camera MyCamera()
    {
        return myCamera;
    }
    
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            Color r = Random.ColorHSV();
            
            foreach (Transform child in meshRenderer.transform)
            {
                child.GetComponent<MeshRenderer>().material.color = r;
                child.gameObject.layer = LayerMask.NameToLayer("Default");
            }
            
            myCamera.gameObject.SetActive(false);
            
            ui.SetActive(false);
            return;
        }
        
        myCamera.cullingMask = maskCameraPlayer;
        startPos = cameraTransform.localPosition;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        nearbyTruck = TruckController.instance.GetComponent<TruckInteraction>();

        yaw = transform.eulerAngles.y;
        pitch = cameraTransform.localEulerAngles.x;
        
        speed = moveSpeed;
        
        shooter = gameObject.GetComponent<ShooterComponent>();
    }

    private Vector3 lastTruckPosition;

    public CharacterController controller;
    public bool canEnterInTruck = false;

    void Update()
    {
        if (!IsOwner) return;
        
        textGoInCamion.SetActive(canEnterInTruck);
        textReload.SetActive(canReload);
        
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            speed = sprintSpeed;
        }else if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            speed = moveSpeed;
        }
        
        if (Input.GetKeyDown(KeyCode.E) && (canEnterInTruck ||isInTruck))
        {
            if (!isInTruck)
            {
                canEnterInTruck = false;
                TruckController.instance.GetComponent<TruckInteraction>().TryEnterTruck(this);
            }
            else if (isInTruck && isDriver)
            {
                print("TryExitTruck");
                nearbyTruck.TryExitTruck(this);
            }
        }

        canReload = CheckCanReload();

        if (Input.GetKeyUp(KeyCode.E))
        {
            if(canReload)
            {
                print("Reload");
                shooter.Reload();
                canReload = false;
            }

            if (TruckController.instance.GetComponent<TruckInteraction>().hasDriver.Value == false)
                return;
            
            canSit = CheckRaycast();
            
            if (isSitting)
            {
                sittingPos = null;
                isSitting = false;
            }
            
            if (canSit)
            {
                print("can sit : " + canSit);
                
                isSitting = true;
                canSit = false;
            }
        }

        if (isDriver && isInTruck)
        {
            if (TruckController.instance != null)
            {
                transform.position = TruckController.instance.driverPos.position;
                
                float h = Input.GetAxis("Horizontal");
                float v = Input.GetAxis("Vertical");
                bool brake = Input.GetKey(KeyCode.Space);
                bool horn = Input.GetKeyDown(KeyCode.H);
                TruckController.instance.SendInputsServerRpc(h, v, brake, horn);
            }
            
            HandleCameraInput();
            return;
        }

        if (isSitting && sittingPos != null)
        {
            transform.position = sittingPos.position;
                
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            
            HandleCameraInput();
            return;
        }

        if (!isInTruck || !isDriver)
        {
            HandleMovement();
        }
    }

    private bool CheckRaycast()
    {
        RaycastHit hit;
        Vector3 origin = MyCamera().transform.position;
        Vector3 direction = MyCamera().transform.forward;
        
        print("CheckRaycast");
        
        if (Physics.Raycast(origin, direction, out hit, 5f))
        {
            Debug.DrawLine(origin, hit.point, Color.cyan);
            
            if (hit.collider.CompareTag("Chairs"))
            {
                sittingPos = hit.collider.GetComponent<Chair>().sittingPos;
                return true;
            }
        }

        return false;
    }

    bool CheckCanReload()
    {
        return Vector3.Distance(transform.position, TruckController.instance.reload.position) < TruckController.instance.raduisToReload;
    }

    public bool isFreeze;
    
    void HandleCameraInput()
    {
        if (!isFreeze)
        {
            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensibility;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensibility;

            yaw += mouseX;
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, verticalLimit.x, verticalLimit.y);
        }
    }

    private void HandleHeadbob()
    {
        if (!controller.isGrounded || new Vector2(horizontalInput, verticalInput).magnitude < 0.1f)
        {
            cameraTransform.localPosition = Vector3.Lerp(
                cameraTransform.localPosition,
                startPos,
                Time.deltaTime
            );
            return;
        }

        // Sprint ?
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);

        float amp = _amplitude * (isSprinting ? sprintAmplitudeMultiplier : 1f);
        float freq = frequency * (isSprinting ? sprintFrequencyMultiplier : 1f);

        Vector3 bobOffset;
        bobOffset.y = Mathf.Sin(Time.time * freq) * amp;
        bobOffset.x = Mathf.Cos(Time.time * freq * 0.5f) * amp * 2f;
        bobOffset.z = 0f;

        cameraTransform.localPosition = startPos + bobOffset;
    }

    void HandleMovement()
    {
        if (!isFreeze)
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
            verticalInput = Input.GetAxisRaw("Vertical");

            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensibility;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensibility;

            yaw += mouseX;
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, verticalLimit.x, verticalLimit.y);

            if (controller.isGrounded)
            {
                if (verticalVelocity < 0)
                    verticalVelocity = -2f;

                if (Input.GetKeyDown(KeyCode.Space))
                {
                    verticalVelocity = jumpForce;
                }
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }

            Vector3 move = Vector3.zero;
            Vector3 localMove = new Vector3(horizontalInput, 0, verticalInput).normalized;
            move = transform.TransformDirection(localMove) * speed;
            move.y = verticalVelocity;

            controller.enabled = true;
            controller.Move(move * Time.deltaTime);
            controller.enabled = false;
        }
    }

    void LateUpdate()
    {
        if (!IsOwner) return;
        
        transform.rotation = UnityEngine.Quaternion.Euler(0, yaw, 0);
        cameraTransform.rotation = UnityEngine.Quaternion.Euler(pitch, yaw, 0);
        
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, cameraTarget.position, Time.deltaTime * cameraSmoothFollow);
    
        if(!isInTruck || !isDriver)
            HandleHeadbob();
    }

    public void EnterTruck(bool asDriver, Vector3 spawnPosition)
    {
        print($"EnterTruck - asDriver: {asDriver}");
        
        isInTruck = true;
        isDriver = asDriver;
        
        transform.localPosition = spawnPosition;
        
        SetParentServerRpc(true);

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
            
            controller.enabled = false;
        }
        else
        {
            NetworkObject netObj = GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.TrySetParent((Transform)null);
            }
            
            controller.enabled = true;
        }
    }
    
        [ServerRpc(RequireOwnership = false)]
    public void SetPassengerModeServerRpc(bool isPassenger, Vector3 desiredLocalPos)
    {
        // Le serveur peut valider si besoin (ex: vraiment dans le camion ?)
        SetPassengerModeClientRpc(isPassenger, desiredLocalPos);
    }

    [ClientRpc]
    private void SetPassengerModeClientRpc(bool isPassenger, Vector3 desiredLocalPos)
    {
        isInTruck = isPassenger;
        isDriver = false;

        if (isPassenger)
        {
            if (controller != null)
            {
                controller.enabled = false;
            }

            var netTransform = GetComponent<NetworkTransform>();
            if (netTransform != null)
            {
                netTransform.InLocalSpace = true; 
            }

            Debug.Log("Client: Mode passager activé - CC disabled + InLocalSpace=true");
        }
        else
        {
            if (controller != null)
            {
                controller.enabled = true;
            }

            var netTransform = GetComponent<NetworkTransform>();
            if (netTransform != null)
            {
                netTransform.InLocalSpace = false;
            }

            Debug.Log("Client: Mode passager désactivé");
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.transform.CompareTag("PorteConducteur") && !isInTruck && TruckController.instance.GetComponent<TruckInteraction>().hasDriver.Value == false)
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