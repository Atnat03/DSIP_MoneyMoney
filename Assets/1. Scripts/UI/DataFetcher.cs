using Shooting;

public enum Data
{
    None,
    AmmoCount,
    MaxAmmoCount,
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
        }
        return "";
    }
}