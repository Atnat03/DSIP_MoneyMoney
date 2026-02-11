using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PiscineDeBillet : NetworkBehaviour
{
    public List<GameObject> sacs;
    public List<GameObject> lingots;
    public NetworkVariable<int> numberOfSac;
    public NetworkVariable<int> numberOfLingot;

    public void OnTriggerEnter(Collider other)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;
        
        if (other.CompareTag("Treasure"))
        {
            GrabbableObject g =  other.GetComponent<GrabbableObject>();

            if (g.type == GrabType.Sac)
            {
                sacs.Add(other.gameObject);
                numberOfSac.Value = sacs.Count;
            }else if (g.type == GrabType.Lingots)
            {
                lingots.Add(other.gameObject);
                numberOfLingot.Value = lingots.Count;
            }
        }
    }
    
    public void OnTriggerExit(Collider other)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;
        
        if (other.CompareTag("Treasure"))
        {
            GrabbableObject g =  other.GetComponent<GrabbableObject>();

            if (g.type == GrabType.Sac)
            {
                sacs.Remove(other.gameObject);
                numberOfSac.Value = sacs.Count;
            }else if (g.type == GrabType.Lingots)
            {
                lingots.Remove(other.gameObject);
                numberOfLingot.Value = lingots.Count;
            }
        }
    }
}
