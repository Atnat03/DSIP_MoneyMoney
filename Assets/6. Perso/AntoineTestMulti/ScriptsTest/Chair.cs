using System;
using UnityEngine;

public class Chair : MonoBehaviour
{
    public Transform sittingPos;

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
        
        //Sit
    }
}
