using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class GrabbableObject : NetworkBehaviour
{
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
            grabPoint.TryGrab(GetComponent<NetworkObject>());
        }
    }
}