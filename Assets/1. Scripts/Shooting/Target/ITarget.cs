using System;
using UnityEngine;

namespace Shooting
{
    public interface ITarget
    {
        public Action<BulletInfo> OnShot { get; }
        public Collider Collider { get; }
    }
}