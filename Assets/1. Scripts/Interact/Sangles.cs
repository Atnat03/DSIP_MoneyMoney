using UnityEngine;

public class Sangles : MonoBehaviour, IInteractible
{
    public Transform stayPos;
    
    public string InteractionName
    {
        get { return interactionName; }
        set { interactionName = value; }
    }

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

        GameObject handObject = null;
        
        GrabPoint grabPoint = player.GetComponent<GrabPoint>();
        if (grabPoint != null)
        {
            handObject = grabPoint.GetCurrentObjectInHand();
            
            grabPoint.Throw();
        }

        if (handObject != null)
        {
            Destroy(handObject.GetComponent<Rigidbody>());
            handObject.transform.position = stayPos.position;
        }
        
    }
}
