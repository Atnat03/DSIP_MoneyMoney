using UnityEngine;

public class GrabbableObject : MonoBehaviour
{
    /*public override void Interact(Transform playerTransform, bool enableCallbacks = true)
    {
        base.Interact(playerTransform, enableCallbacks);
        if (Reference.TryGetObject(out PlayerInterface pi))
            pi.GrabPoint.TryGrab(this.gameObject);
    }

    public override bool CanInteract(Transform playerTransform)
    {
        return
            base.CanInteract(playerTransform) &&
            Reference.TryGetObject(out PlayerInterface pi) &&
            pi.GrabPoint.IsFree;
    }*/
    
    private void OnEnable()
    {
        Interact.OnInteract += HitInteract;
    }

    private void OnDisable()
    {
        Interact.OnInteract -= HitInteract;
    }

    private void HitInteract(GameObject obj,  GameObject player)
    {

        if (obj.gameObject.GetInstanceID() == gameObject.GetInstanceID())
        {

            gameObject.GetComponent<GrabPoint>().TryGrab(player);
        }
    }
    
}