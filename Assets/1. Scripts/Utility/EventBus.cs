
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    private  Dictionary<string, List<Action<DataPacket>>> actionsWithArgs = new();
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

    public static void Invoke(string name, bool invokeActionsWithDefaultPacket = true)
    {
        if (invokeActionsWithDefaultPacket)
            Invoke(name, new DataPacket(), false);

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
    /// <summary>
    /// Adds a callable that will be invoked whenever "name" is invoked
    /// </summary>
    /// <param name="name"></param>
    /// <param name="callable"></param>
    public static void Register(string name, Action<DataPacket> callable)
    {
        if (This.actionsWithArgs == null)
            This.actionsWithArgs = new ();

        if (!This.actionsWithArgs.ContainsKey(name))
            This.actionsWithArgs.Add(name, new());

        This.actionsWithArgs[name].Add(callable);
    }
    /// <summary>
    /// Remove a method or callable from the list of invoked callables associated with name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="callable"></param>
    /// <returns>Wether or not the callable could be removed</returns>
    public static bool Unregister(string name, Action<DataPacket> callable)
    {
        if (This.actionsWithArgs == null)
            return false;

        if (!This.actionsWithArgs.ContainsKey(name))
            return false;

        var callables = This.actionsWithArgs[name];

        if (callables == null)
            return false;

        if (callables.Contains(callable))
        {
            callables.Remove(callable);
            return false;
        }

        return false;
    }

    public static void Invoke(string name, DataPacket packet, bool invokeActionsWithoutPacket = true)
    {
        if (invokeActionsWithoutPacket)
            Invoke(name, false);

        if (This.actionsWithArgs == null)
            return;
        if (!This.actionsWithArgs.ContainsKey(name))
            return;
        if (This.actionsWithArgs[name] == null)
            return;
        foreach (var call in This.actionsWithArgs[name])
        {
            if (call == null) continue;
            call.Invoke(packet);
        }
    }

    public static List<string> GetRegisteredEventNames()
    {
        return This.actionsNoArgs.Keys.ToHashSet()
            .Concat(This.actionsWithArgs.Keys.ToHashSet())
            .ToList();
    }

    /// <summary>
    /// Prints to the console a list of all the registered events
    /// </summary>
    public void DebugRegisteredEvents()
    {
        string result = "EventBus content :\n";
        foreach (string name in GetRegisteredEventNames())
        {
            int count = 0;
            if (!string.IsNullOrEmpty(name))
            {
                if (This.actionsNoArgs != null && This.actionsNoArgs.ContainsKey(name) && This.actionsNoArgs[name] != null)
                    count += This.actionsNoArgs[name].Count;
                if (This.actionsWithArgs != null && This.actionsWithArgs.ContainsKey(name) && This.actionsWithArgs[name] != null)
                    count += This.actionsWithArgs[name].Count;
            }
            result += name + " : " + count + " registered callbacks\n";
        }
        UnityEngine.Debug.Log(result);
    }
}

public struct DataPacket
{
    public int intValue;
    public float floatValue;
    public string stringValue;
    public Vector3 vector3Value;
    public bool boolValue;
    public object objectValue;

    public DataPacket(int value)
    {
        intValue = value;
        floatValue = default;
        stringValue = default;
        vector3Value = default;
        boolValue = default;
        objectValue = default;
    }
    public DataPacket(float value)
    {
        intValue = default;
        floatValue = value;
        stringValue = default;
        vector3Value = default;
        boolValue = default;
        objectValue = default;
    }
    public DataPacket(string value)
    {
        intValue = default;
        floatValue = default;
        stringValue = value;
        vector3Value = default;
        boolValue = default;
        objectValue = default;
    }
    public DataPacket(Vector3 value)
    {
        intValue = default;
        floatValue = default;
        stringValue = default;
        vector3Value = value;
        boolValue = default;
        objectValue = default;
    }
    public DataPacket(bool value)
    {
        intValue = default;
        floatValue = default;
        stringValue = default;
        vector3Value = default;
        boolValue = value;
        objectValue = default;
    }
    public DataPacket(object value)
    {
        intValue = default;
        floatValue = default;
        stringValue = default;
        vector3Value = default;
        boolValue = default;
        objectValue = value;
    }
}