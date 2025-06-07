using UnityEngine;

namespace Scripts.Enemies.Movement.SteeringBehaviors
{
    /// <summary>
    /// A struct that holds the result of a steering behavior calculation.
    /// It contains the desired velocity and a flag indicating if the agent should orient itself.
    /// </summary>
    public struct SteeringOutput
    {
        public Vector2 DesiredVelocity;
        public bool ShouldOrient;

        public SteeringOutput(Vector2 velocity, bool orient)
        {
            DesiredVelocity = velocity;
            ShouldOrient = orient;
        }

        public static SteeringOutput Zero => new SteeringOutput(Vector2.zero, false);
    }
}
