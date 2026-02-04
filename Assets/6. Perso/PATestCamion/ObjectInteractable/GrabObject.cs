using UnityEngine;

public class GrabObject : MonoBehaviour
{
    private void OnEnable()
    {
        Interact.OnInteract += HitInteract;
    }
    private void OnDisable()
    {
        Interact.OnInteract -= HitInteract;
    }
    
    private void HitInteract(GameObject obj)
    {
        obj.GetComponent<GrabPoint>().Grab();
    }
}
