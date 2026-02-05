
using System;
using System.Collections.Generic;
using System.Linq;

public class EventBus
{
    private static EventBus This
    {
        get
        {
            if (_instance == null)
                _instance = new EventBus();
            return _instance;
        }
    }
    private static EventBus _instance;

    private  Dictionary<string, List<Action>> actionsNoArgs = new();
    /// <summary>
    /// Adds a callable that will be invoked whenever "name" is invoked
    /// </summary>
    /// <param name="name"></param>
    /// <param name="callable"></param>
    public static void Register(string name, Action callable)
    {
        if (This.actionsNoArgs == null)
            This.actionsNoArgs = new ();

        if (!This.actionsNoArgs.ContainsKey(name))
            This.actionsNoArgs.Add(name, new());

        This.actionsNoArgs[name].Add(callable);
    }
    /// <summary>
    /// Remove a method or callable from the list of invoked callables associated with name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="callable"></param>
    /// <returns>Wether or not the callable could be removed</returns>
    public static bool Unregister(string name, Action callable)
    {
        if (This.actionsNoArgs == null)
            return false;

        if (!This.actionsNoArgs.ContainsKey(name))
            return false;

        var callables = This.actionsNoArgs[name];

        if (callables == null)
            return false;

        if (callables.Contains(callable))
        {
            callables.Remove(callable);
            return false;
        }

        return false;
    }

    public static void Invoke(string name)
    {
        if (This.actionsNoArgs == null)
            return;
        if (!This.actionsNoArgs.ContainsKey(name))
            return;
        if (This.actionsNoArgs[name] == null)
            return;
        foreach (var call in This.actionsNoArgs[name])
        {
            if (call == null) continue;
            call.Invoke();
        }
    }

    public static List<string> GetRegisteredEventNames()
    {
        return This.actionsNoArgs.Keys.ToList();
    }

    /// <summary>
    /// Prints to the console a list of all the registered events
    /// </summary>
    public void DebugRegisteredEvents()
    {
        string result = "EventBus content :\n";
        foreach (var kvp  in This.actionsNoArgs)
        {
            int count = 0;
            if (kvp.Value != null)
                count = kvp.Value.Count;
            result += kvp.Key + " : " + count + " registered callbacks\n";
        }
        UnityEngine.Debug.Log(result);
    }
}