
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shooting
{
    public interface IShooter
    {
        public bool EnableCallbacks { get; set; }
        public Action OnShoot { get; set; }
        public Action<ITarget> OnTargetHit { get; set; }
        public bool TryShoot(Vector3 Pos, Vector3 Dir, out List<BulletInfo> bulletInfos);
    }
}