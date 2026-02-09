using System;
using Unity.Netcode;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Random = UnityEngine.Random;

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
    
    [Header("Timer before drop")]
    [SerializeField] float max_TimeBeforeDrop = 10f;
    [SerializeField] float mini_TimeBeforeDrop = 5f;
    private float t = 0;

    public string interactionName;

    public bool IsStock()
    {
        return interactionObject != null;
    }
    
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
        print("Release");
        
        interactionObject.gameObject.layer = LayerMask.NameToLayer("Interactable");
        
        GrabPoint grabPlayer = player.GetComponent<GrabPoint>();
        if (grabPlayer != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            
            collider.enabled = true;

            
            if(isGrabbing)
                grabPlayer.TryGrab(interactionObject.GetComponent<NetworkObject>());
        }
        
        rb = null;
        collider = null;
        interactionObject = null;
    }

    private void Drop()
    {
        Debug.Log("Drop");
        
        interactionObject.gameObject.layer = LayerMask.NameToLayer("Interactable");
        rb.isKinematic = false;
        rb.useGravity = true;
        collider.enabled = true;
        
        collider =  null;
        rb = null;
        interactionObject = null;
    }

    private void StockObjct(GameObject player)
    {
        print("Stock");
        
        GrabPoint grabPoint = player.GetComponent<GrabPoint>();
        if (grabPoint != null)
        {
            interactionObject = grabPoint.GetCurrentObjectInHand();
            interactionObject = grabPoint.GetCurrentObjectInHand();
            grabPoint.Throw();
            rb = interactionObject.GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            
            collider = interactionObject.GetComponent<Collider>();
            collider.enabled = false;
        }
        
        interactionObject.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        
        if (interactionObject != null)
        {
            interactionObject.transform.position = stayPos.position;
        }

        t = Random.Range(mini_TimeBeforeDrop, max_TimeBeforeDrop);
    }

    private void Update()
    {
        if (interactionObject == null)
            return;
        
        if (t > 0)
        {
            t -= Time.deltaTime;
        }
        else
        {
            Debug.Log("End of timer before drop");
            Drop();
        }
    }
}
