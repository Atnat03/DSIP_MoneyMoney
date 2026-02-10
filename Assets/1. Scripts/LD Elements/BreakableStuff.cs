using System;
using UnityEngine;

public class BreakableStuff : MonoBehaviour
{
    [SerializeField] GameObject IntactObject;
    [SerializeField] GameObject breakObject;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Truck"))
        {
            IntactObject.SetActive(false);
            breakObject.SetActive(true);
        }
    }
}
