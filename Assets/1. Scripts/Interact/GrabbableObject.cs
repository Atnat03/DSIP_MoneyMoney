using Unity.Netcode;
using UnityEngine;

public enum GrabType {Sac, Lingots}

[RequireComponent(typeof(NetworkObject))]
public class GrabbableObject : NetworkBehaviour, IGrabbable, IParentable, IInteractible
{
    public NetworkVariable<bool> IsGrabbed = new(false);

    public GrabType type;

    public Transform Transform => transform;
    public NetworkObject NetworkObject => GetComponent<NetworkObject>();

    public override void OnNetworkSpawn()
    {
        Reference.AddObject(this as IParentable);
        
        Interact.OnInteract += HitInteract;
    }

    private void OnDisable()
    {
        Interact.OnInteract -= HitInteract;
    }

    private void HitInteract(GameObject obj, GameObject player)
    {
        if (obj.GetInstanceID() != gameObject.GetInstanceID()) return;

        var grabPoint = player.GetComponent<GrabPoint>();
        if (grabPoint != null)
        {
            grabPoint.TryGrab(NetworkObject);
        }
    }

    public void OnParented(Transform parent)
    {
        Debug.Log($"{name} parenté au camion");
    }

    public void OnUnparented()
    {
        Debug.Log($"{name} déparenté du camion");
    }

    public string InteractionName
    {
        get { return interactionName; }
        set { interactionName = value; }
    }

    public string interactionName;
    
    public Outline[] Outline     
    {
        get { return outline; ; }
        set { }
    }

    public Outline[] outline;
}


public interface IGrabbable
{ }