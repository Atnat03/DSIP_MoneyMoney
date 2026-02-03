
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Shooting
{
    public interface IShooter
    {
        public UnityEvent OnShoot { get; }
        public UnityEvent<TargetInfo> OnTargetHit { get; }
        public bool TryShoot(Vector3 Pos, Vector3 Dir, out List<BulletInfo> bulletInfos);
    }

}