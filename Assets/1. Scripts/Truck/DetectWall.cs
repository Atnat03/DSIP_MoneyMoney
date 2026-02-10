using System;
using UnityEngine;

public class DetectWall : MonoBehaviour
{
    public TruckController truckController;
    
    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            truckController.CheckVelecityToApplyShake();
        }
    }
}
