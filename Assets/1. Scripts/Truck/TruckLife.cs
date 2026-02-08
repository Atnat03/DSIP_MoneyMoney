using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Shooting;

public class TruckLife : NetworkBehaviour
{
    public static TruckLife instance;
    
    public List<TruckPart> truckpartsRight = new List<TruckPart>();
    public List<TruckPart> truckpartsLeft = new List<TruckPart>();

    public bool canShootPlayerLeft, canShootPlayerRight;
    [SerializeField] private int partToShootPlayer;

    void Awake()
    {
        instance = this;
    }

    public void DetermineIfShootable()
    {
        canShootPlayerLeft = false;
        canShootPlayerRight = false;
        int count = 0;
        foreach (TruckPart part in truckpartsLeft)
        {
            if (part.isBroke.Value)
            {
                count++;
            }
        }
        if (count >= partToShootPlayer) canShootPlayerLeft = true;
        
        count = 0;
        foreach (TruckPart part in truckpartsRight)
        {
            if (part.isBroke.Value)
            {
                count++;
            }
        }

        if (count >= partToShootPlayer) canShootPlayerRight = true;
    }



}