using Shooting;
using UnityEngine;
using UnityEngine.Events;

public class ReloadStation : Interactable
{
    [Header("Reload station")]
    [SerializeField] private UnityEvent _onReload;

    protected override void Interact(Transform playerTransform, bool enableCallbacks = true)
    {
        base.Interact(playerTransform, enableCallbacks);

        if (playerTransform.TryGetComponent(out ShooterComponent comp))
        {
            comp.Reload();
            if (enableCallbacks)
                _onReload.Invoke();
        }
    }
}