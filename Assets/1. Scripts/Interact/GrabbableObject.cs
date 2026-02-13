using System;
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

    private float lastReleaseTime = -999f;
    private const float REGRAB_COOLDOWN = 0.3f;

    public override void OnNetworkSpawn()
    {
        Interact.OnInteract += HitInteract;
        
        IsGrabbed.OnValueChanged += OnGrabbedChanged;
    }

    private void OnGrabbedChanged(bool oldValue, bool newValue)
    {
        SFX_Manager.instance.PlaySFX(14);
        
        if (oldValue == true && newValue == false)
        {
            lastReleaseTime = Time.time;
        }
    }

    public void OnEnable()
    {
        TruckController.instance.AddInParent(this);
    }

    private void OnDisable()
    {
        Interact.OnInteract -= HitInteract;
        IsGrabbed.OnValueChanged -= OnGrabbedChanged;
    }

    public void ResetGrabState()
    {
        IsGrabbed.Value = false;
    }
    
    private void HitInteract(GameObject obj, GameObject player)
    {
        if (obj.GetInstanceID() != gameObject.GetInstanceID()) return;
        
        if (Time.time - lastReleaseTime < REGRAB_COOLDOWN)
        {
            return;
        }
        
        if (IsGrabbed.Value) return;

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
        get { return outline; }
        set { }
    }

    public Outline[] outline;
}

public interface IGrabbable { }