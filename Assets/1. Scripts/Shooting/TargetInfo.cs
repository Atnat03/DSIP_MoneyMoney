using UnityEngine;

namespace Shooting
{

    public struct TargetInfo
    {
        public ITarget Target;
        public Transform Transform;
        public Collider Collider;

        public bool IsValid
        {
            get
            {
                return
                    Target != null &&
                    Collider != null &&
                    Transform != null;
            }
        }

        public TargetInfo
            (
            ITarget target,
            Transform transform,
            Collider collider
            )
        {
            Target = target;
            Transform = transform;
            Collider = collider;
        }
    }

}