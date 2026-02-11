using System.Collections.Generic;
using UnityEngine;

public class PiscineDeBillet : MonoBehaviour
{
    public List<GameObject> treasure;
    

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Treasure"))
        {
            treasure.Add(other.gameObject);
        }
    }
    
    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Treasure"))
        {
            treasure.Remove(other.gameObject);
        }
    }
}
