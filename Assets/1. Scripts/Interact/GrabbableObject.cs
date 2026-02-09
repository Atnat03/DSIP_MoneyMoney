using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class GrabbableObject : NetworkBehaviour, IGrabbable, IParentable
{
    public NetworkVariable<bool> IsGrabbed = new(false);

    public Transform Transform => transform;
    public NetworkObject NetworkObject => GetComponent<NetworkObject>();

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
}


public interface IGrabbable
{ }