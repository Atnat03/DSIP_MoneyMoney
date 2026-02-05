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
            case Data.AmmoCount: return Reference.GetObject<ShooterComponent>()?.AmmoCount.ToString();
            case Data.MaxAmmoCount: return Reference.GetObject<ShooterComponent>()?.MaxAmmoCount.ToString();
        }
        return "";
    }
}