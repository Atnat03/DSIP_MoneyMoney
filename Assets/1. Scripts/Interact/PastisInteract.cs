using UnityEngine;

public class PastisInteract : MonoBehaviour, IInteractible
{
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

    void Start()
    {
        Interact.OnInteract += HitInteract;
    }

    void OnDisable()
    {
        Interact.OnInteract -= HitInteract;
    }

    private void HitInteract(GameObject obj, GameObject player)
    {
        if (obj.GetInstanceID() != gameObject.GetInstanceID()) return;

        PlayerPastis pPastis =  player.GetComponent<PlayerPastis>();

        if (!pPastis.hasBottleInHand)
        {
            pPastis.TakeABottle();
        }
    }
}
