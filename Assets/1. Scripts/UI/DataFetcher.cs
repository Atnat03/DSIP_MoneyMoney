using Shooting;

public enum Data
{
    None,
    AmmoCount,
    MaxAmmoCount,
    CurrentHealth,
    MaxHealth,
}

public class DataFetcher
{
    public static string GetString(Data data)
    {
        switch (data)
        {
            case Data.None: return "";
            case Data.AmmoCount: return Reference.GetObject<PlayerInterface>()?.ShooterComponent.AmmoCount.ToString();
            case Data.MaxAmmoCount: return Reference.GetObject<PlayerInterface>()?.ShooterComponent.MaxAmmoCount.ToString();
            case Data.MaxHealth: return Reference.GetObject<PlayerInterface>()?.HealthComponent.MaxHealth.ToString();
            case Data.CurrentHealth: return Reference.GetObject<PlayerInterface>()?.HealthComponent.Health.ToString();
        }
        return "";
    }
}