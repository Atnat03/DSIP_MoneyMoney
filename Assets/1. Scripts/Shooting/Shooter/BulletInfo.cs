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
        public float Damage;

        public BulletInfo(bool autoInit)
        {
            Shooter = null;
            HasHit = false;
            Damage = 1;
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