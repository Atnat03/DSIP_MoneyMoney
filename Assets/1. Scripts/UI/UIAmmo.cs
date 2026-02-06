using System;
using Shooting;
using TMPro;
using UnityEngine;

public class UIAmmo : MonoBehaviour
{
    public ShooterComponent shooter;
    
    public TextMeshProUGUI currentAmmo;
    public TextMeshProUGUI maxAmmo;

    public void Update()
    {
        currentAmmo.text = "Ammo : " + shooter.AmmoCount;
        maxAmmo.text = " / " + shooter.MaxAmmoCount;
    }
}
