namespace GameSystem
{
    public static class TrackedTypes
    {
        public static System.Type[] GetTypes()
        {
            return new System.Type[]
            {
                // Add types to be tracked here

                typeof(Shooting.ShooterComponent),

                // ...
            };
        }
    }
}