using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public interface IInteractible
{
    string InteractionName { get; set; }
    Outline[] Outline { get; set; }
}

public class PlayerRayCast : NetworkBehaviour
{
    public static PlayerRayCast LocalInstance;

    public UIController uiController;
    public float durationRepair;
    public Image circleCD;
    public bool hasMaterial;
    public GameObject materialVisual;
    public float hitDistance = 2f;
    public string RepearInteractionName = "Réparer";
    public Material TransparentMaterial;

    private MeshRenderer mesh;
    private KnockOut targetKO;
    private bool isReviving = false;
    private bool isRepairing = false;
    private IInteractible lastInteractible;
    private Camera mycam;
    private FPSControllerMulti playerFPS;
    private GrabPoint playerGrab;
    private TruckPart truckPart;

    private void Start()
    {
        uiController = VariableManager.instance.uiController;
        circleCD = VariableManager.instance.circleCD;
        playerFPS = GetComponent<FPSControllerMulti>();
        materialVisual.transform.parent = playerFPS.MyCamera().transform;
        mycam = playerFPS.MyCamera();
        playerGrab = playerFPS.GetComponent<GrabPoint>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            LocalInstance = this;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (mycam == null) return;

        RaycastHit hit;
        Ray ray = new Ray(mycam.transform.position, mycam.transform.forward);
        Debug.DrawRay(mycam.transform.position, mycam.transform.forward, Color.darkRed);

        int layerMask = ~LayerMask.GetMask("IgnoreRaycast", "Skin");
        bool hitInteractible = false;

        if (Physics.Raycast(ray, out hit, hitDistance, layerMask))
        {
            // Réanimer
            if (hit.collider.TryGetComponent<KnockOut>(out KnockOut ko) || hit.collider.gameObject.layer == LayerMask.NameToLayer("PlayerRagdoll"))
            {
                targetKO = ko ?? hit.transform.GetComponentInParent<KnockOut>();
                if (targetKO != null && targetKO.isKnockedOut.Value)
                {
                    uiController?.OnInteract();
                    uiController?.SetText("Réanimer");

                    if (Input.GetKeyDown(KeyCode.E) && !isReviving)
                    {
                        isReviving = true;
                        playerFPS.StartFreeze();
                        targetKO.StartMateReviveServerRpc(NetworkManager.Singleton.LocalClientId);
                    }
                    if (Input.GetKeyUp(KeyCode.E) && isReviving)
                    {
                        playerFPS.StopFreeze();
                        isReviving = false;
                        targetKO.StopMateReviveServerRpc();
                    }
                }
                else
                {
                    ResetRevive();
                }
            }

            // Interaction
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Interactable"))
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (hit.collider.CompareTag("TruckPart") && hasMaterial && !isRepairing)
                    {
                        TruckPart part = hit.collider.GetComponent<TruckPart>();
                        if (part != null && part.isBroke.Value)
                        {
                            StartCoroutine(RepairPart(part));
                            return;
                        }
                    }
                    else if (hit.collider.CompareTag("Material"))
                    {
                        TakeMaterial();
                    }
                    else if (!hit.collider.CompareTag("TruckPart"))
                    {
                        Interact.RayInteract(hit.collider.gameObject, gameObject, RepearInteractionName);
                    }
                }
                else if (Input.GetKeyUp(KeyCode.E) && isRepairing)
                {
                    StopRepair();
                }
                else if ((!hit.collider.CompareTag("TruckPart") ||
                         (hit.collider.CompareTag("TruckPart") && hit.collider.GetComponent<TruckPart>()?.isBroke.Value == true && hasMaterial)))
                {
                    if (hit.collider.CompareTag("Sangles"))
                    {
                        if (!playerGrab.IsSacInHand() && !hit.collider.GetComponent<Sangles>()?.IsStock() == true)
                        {
                            DisableLastOutline();
                            return;
                        }
                    }

                    if (hit.collider.TryGetComponent<IInteractible>(out var interactible))
                    {
                        hitInteractible = true;

                        if (lastInteractible != null && lastInteractible != interactible)
                        {
                            DisableLastOutline();
                        }

                        lastInteractible = interactible;
                        uiController?.OnInteract();
                        uiController?.SetText(interactible.InteractionName);

                        TruckPart part = hit.collider.GetComponent<TruckPart>();
                        if (part != null && part.isBroke.Value)
                        {
                            part.mesh.enabled = true;
                            part.mesh.material = TransparentMaterial;
                            truckPart = part;
                        }

                        foreach (Outline o in interactible.Outline)
                        {
                            if (o != null) o.enabled = true;
                        }
                    }
                }
            }
        }

        if (!hitInteractible)
        {
            DisableLastOutline();
            uiController?.OnStopInteract();
        }

        if (Input.GetButtonDown("Fire2") && hasMaterial && !playerFPS.IsFreeze)
        {
            TakeMaterial();
        }
    }

    private void DisableLastOutline()
    {
        if (lastInteractible == null) return;

        if (truckPart != null)
        {
            truckPart.mesh.enabled = false;
            truckPart = null;
        }

        foreach (Outline o in lastInteractible.Outline)
        {
            if (o != null) o.enabled = false;
        }

        lastInteractible = null;
    }

    public void TakeMaterial()
    {
        hasMaterial = !hasMaterial;
        playerFPS.hasSomethingInHand = hasMaterial;
        materialVisual.SetActive(hasMaterial);
    }

    private IEnumerator RepairPart(TruckPart part)
    {
        isRepairing = true;
        GetComponent<FPSControllerMulti>().animator.SetBool("Repare", true);
        playerFPS.StartFreeze();

        float count = durationRepair;
        while (count > 0 && isRepairing)
        {
            count -= Time.deltaTime;
            circleCD.fillAmount = count / durationRepair;
            yield return null;
        }

        if (isRepairing)
        {
            RepairPartServerRpc(part.NetworkObjectId);
            playerFPS.StopFreeze();
            TakeMaterial();
            circleCD.fillAmount = 0;
        }

        GetComponent<FPSControllerMulti>().animator.SetBool("Repare", false);
        isRepairing = false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RepairPartServerRpc(ulong partNetworkId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(partNetworkId, out var networkObject))
        {
            Debug.LogWarning($"TruckPart avec NetworkObjectId {partNetworkId} introuvable");
            return;
        }

        TruckPart part = networkObject.GetComponent<TruckPart>();
        if (part != null && part.isBroke.Value)
        {
            part.Repair();
        }
    }

    private void StopRepair()
    {
        if (!isRepairing) return;
        isRepairing = false;
        playerFPS.StopFreeze();
        circleCD.fillAmount = 0;
        GetComponent<FPSControllerMulti>().animator.SetBool("Repare", false);
    }

    private void ResetRevive()
    {
        if (isReviving && targetKO != null)
        {
            targetKO.StopMateReviveServerRpc();
        }
        playerFPS.StopFreeze();
        isReviving = false;
        targetKO = null;
        uiController?.OnStopInteract();
    }

    [SerializeField] private Image reviveHUDImage;
    public void UpdateReviveUI(float value, bool isActive)
    {
        if (reviveHUDImage == null) return;
        reviveHUDImage.transform.parent.gameObject.SetActive(isActive);
        reviveHUDImage.fillAmount = Mathf.Clamp01(value);
    }
}