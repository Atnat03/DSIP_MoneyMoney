using Unity.Netcode;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Sangles : MonoBehaviour, IInteractible
{
    public Transform stayPos;
    
    public string InteractionName
    {
        get { return interactionName; }
        set { interactionName = value; }
    }
    
    [SerializeField]private GameObject interactionObject = null;
    private Rigidbody rb;
    private Collider collider;

    public string interactionName;
    
    private void OnEnable()
    {
        Interact.OnInteract += HitInteract;
    }

    private void OnDisable()
    {
        Interact.OnInteract -= HitInteract;
    }

    private void HitInteract(GameObject obj, GameObject player)
    {
        if (obj.GetInstanceID() != gameObject.GetInstanceID()) return;

        Debug.Log("Hit interact");

        if (interactionObject == null)
        {
            StockObjct(player);
        }
        else
        {
            ReleaseObject(player, true);
        }
    }

    private void ReleaseObject(GameObject player, bool isGrabbing)
    {
        interactionObject.gameObject.layer = LayerMask.NameToLayer("Interactable");
        
        GrabPoint grabPoint = player.GetComponent<GrabPoint>();
        if (grabPoint != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb = null;
            
            collider.enabled = true;
            
            if(isGrabbing)
                grabPoint.TryGrab(interactionObject.GetComponent<NetworkObject>());
        }

        interactionObject = null;
    }

    private void StockObjct(GameObject player)
    {
        GameObject handObject = null;
        
        GrabPoint grabPoint = player.GetComponent<GrabPoint>();
        if (grabPoint != null)
        {
            handObject = grabPoint.GetCurrentObjectInHand();
            grabPoint.Throw();
            rb = handObject.GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            
            collider = handObject.GetComponent<Collider>();
            collider.enabled = false;
        }

        if (handObject != null)
        {
            handObject.transform.position = stayPos.position;
        }
        
        interactionObject = handObject;
    }
}
