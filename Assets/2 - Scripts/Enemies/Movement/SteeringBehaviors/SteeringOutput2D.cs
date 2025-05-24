using UnityEngine;

namespace Scripts.Enemies.Movement.SteeringBehaviors
{
    public struct SteeringOutput2D
    {
        public Vector2 DesiredVelocity;
        public bool ShouldOrient;

        public SteeringOutput2D(Vector2 velocity, bool orient)
        {
            DesiredVelocity = velocity;
            ShouldOrient = orient;
        }

        public static SteeringOutput2D Zero => new SteeringOutput2D(Vector2.zero, false);
    }
}
