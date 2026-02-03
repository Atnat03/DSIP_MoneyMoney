using System.Collections.Generic;
using UnityEngine;

namespace Shooting
{
    public struct BulletInfo
    {
        public IShooter Shooter;
        public bool HasHit;
        public List<ITarget> HitTargets;
        public List<Vector3> Positions;

        public BulletInfo(bool autoInit)
        {
            Shooter = null;
            HasHit = false;
            if (autoInit)
            {
                HitTargets = new();
                Positions = new();
            }
            else
            {
                HitTargets = null;
                Positions = null;
            }
        }
    }

}