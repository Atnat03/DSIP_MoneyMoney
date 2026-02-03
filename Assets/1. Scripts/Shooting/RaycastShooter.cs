using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Shooting
{

    public class RaycastShooter : IShooter
    {
        #region Properties

        public float MaxDistance { get; set; } = 1000f;
        // Invoked whenever the shooter successfully shoots
        public UnityEvent OnShoot { get; }
        // Invoked when a target is hit, with said target as parameter
        public UnityEvent<TargetInfo> OnTargetHit { get; }
        public bool EnableCallbacks { get; set; } = true;

        #endregion

        
        /// <summary>
        /// Checks if it's possible to shoot, and does it if so
        /// </summary>
        /// <param name="Pos"></param>
        /// <param name="Dir"></param>
        /// <param name="bulletInfos"></param>
        /// <returns>True if a shot was taken, false otherwise</returns>
        public bool TryShoot(Vector3 Pos, Vector3 Dir, out List<BulletInfo> bulletInfos)
        {
            bulletInfos = Shoot(Pos, Dir);
            return true;
        }

        private List<BulletInfo> Shoot(Vector3 Pos, Vector3 Dir)
        {
            // Add logic here to replace raycast shots by physics shots
            var result = ShootByRaycast(Pos, Dir);
            OnShoot_Callback();
            return result;
        }

        // Overload in case we don't need the first target specifically
        private List<BulletInfo> ShootByRaycast(Vector3 Pos, Vector3 Dir)
            => ShootByRaycast(Pos, Dir, out TargetInfo tempData);

        /// <summary>
        /// By default, shoots a single bullet. Can be overloaded to shoot multiple bullets (like a shotgun)
        /// </summary>
        /// <param name="Pos"></param>
        /// <param name="Dir"></param>
        /// <param name="firstTarget"></param>
        /// <returns>The infos of all the bullets shot</returns>
        private List<BulletInfo> ShootByRaycast(Vector3 Pos, Vector3 Dir, out TargetInfo firstTarget)
        {
            List<BulletInfo> result = new();
            BulletInfo singleBullet = ShootBulletByRaycast(Pos, Dir, out bool tempData);
            result.Add(singleBullet);

            if (singleBullet.HitTargets.Count > 0)
                firstTarget = singleBullet.HitTargetsInfo[0];
            else
                firstTarget = new TargetInfo();

            return result;
        }

        private BulletInfo ShootBulletByRaycast(Vector3 Pos, Vector3 Dir, out bool hasHit)
        {
            hasHit = false;

            RaycastHit[] hitInfos = Physics.RaycastAll(Pos, Dir, MaxDistance);

            BulletInfo result = new BulletInfo();

            foreach (RaycastHit hitInfo in hitInfos)
            {
                bool hitObjectIsTarget = false;
                ITarget target = null;

                // Find a component that is a target on the collided gameobject
                foreach (var comp in hitInfo.collider.gameObject.GetComponents<Behaviour>())
                {
                    if (comp == null || !comp.isActiveAndEnabled) continue;
                    if (comp is ITarget currTarget)
                    {
                        hitObjectIsTarget = true;
                        target = currTarget;
                        break;
                    }
                }

                // If it is a non-null target, add it to the result list
                if (hitObjectIsTarget && target != null)
                {
                    TargetInfo ti = new TargetInfo(target, hitInfo.transform, hitInfo.collider);
                    result.Shooter = this;
                    result.HasHit = true;
                    result.HitTargetsInfo.Add(ti);
                    result.HitTargets.Add(target);
                    result.Positions.Add(hitInfo.point);
                    hasHit = true;

                    OnTargetHit_Callback(ti);
                }
            }
            return result;
        }

        private void OnShoot_Callback()
        {
            if (EnableCallbacks)
                OnShoot.Invoke();
        }

        private void OnTargetHit_Callback(TargetInfo info)
        {
            if (EnableCallbacks)
                OnTargetHit.Invoke(info);
        }
    }

}