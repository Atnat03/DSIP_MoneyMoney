using System;
using System.Collections.Generic;
using UnityEngine;

public class LineDebugger
{
    public List<Tuple<Vector3, Vector3, Color>> Lines = new();
    
    public void Draw()
    {
        foreach(var line in Lines)
        {
            Gizmos.color = line.Item3;
            Gizmos.DrawLine(line.Item1, line.Item2);
        }
    }
    public void Clear()
    {
        Lines.Clear();
    }
    public void Add(Vector3 start,  Vector3 end, Color color) => Lines.Add(Tuple.Create(start, end, color));
}