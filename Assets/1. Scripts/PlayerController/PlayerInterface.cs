

using Shooting;
using UnityEngine;

/// <summary>
/// This class serves as a container for every player related script
/// </summary>
public class PlayerInterface : MonoBehaviour
{
    #region Fields

    public FPSController FPSController;
    public ShooterComponent ShooterComponent;
    public GrabPoint GrabPoint;
    public HealthComponent HealthComponent;

    #endregion
}