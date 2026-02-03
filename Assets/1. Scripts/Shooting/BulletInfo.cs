using System.Collections.Generic;
using UnityEngine;

namespace Shooting
{
    public struct BulletInfo
    {
        public IShooter Shooter;
        public bool HasHit;
        public List<TargetInfo> HitTargetsInfo;
        public List<ITarget> HitTargets;
        public List<Vector3> Positions;
    }

}