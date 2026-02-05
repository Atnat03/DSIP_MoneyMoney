public interface IManager
{
    // Register should always be the only method called in a manager's constructor
    public void Register();
    // Reference.SetManager(this);
    public void Unregister();
    // Reference.RemoveManager(this);

}