using Shooting;
using UnityEngine;
using UnityEngine.Events;

public class ReloadStation : MonoBehaviour
{
    [Header("Reload station")]
    [SerializeField] private UnityEvent _onReload;

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
        Debug.Log("Hit interact");
        if (obj.gameObject.GetInstanceID() == gameObject.GetInstanceID())
        {
            Debug.Log("Reload station");

            if (player.TryGetComponent(out ShooterComponent comp))
            {
                comp.Reload();
                _onReload.Invoke();
            }
        }
    }
}