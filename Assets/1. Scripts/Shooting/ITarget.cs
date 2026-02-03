using UnityEngine;
using UnityEngine.Events;

namespace Shooting
{
    public interface ITarget
    {
        public UnityEvent OnShot { get; }
        public Collider Collider { get; }
        public TargetInfo TargetInfo { get; }
    }
}