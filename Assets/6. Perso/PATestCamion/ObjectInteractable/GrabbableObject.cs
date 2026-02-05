using UnityEngine;

public class GrabbableObject : Interactable
{
    protected override void Interact(Transform playerTransform, bool enableCallbacks = true)
    {
        base.Interact(playerTransform, enableCallbacks);
        if (Reference.TryGetObject(out PlayerInterface pi))
            pi.GrabPoint.Grab(this.gameObject);
    }

    public override bool CanInteract(Transform playerTransform)
    {
        return
            base.CanInteract(playerTransform) &&
            Reference.TryGetObject(out PlayerInterface pi) &&
            pi.GrabPoint.IsFree;
    }
}