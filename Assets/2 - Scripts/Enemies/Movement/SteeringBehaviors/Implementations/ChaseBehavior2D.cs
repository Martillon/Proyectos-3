using UnityEngine;

namespace Scripts.Enemies.Movement.SteeringBehaviors.Implementations
{
    /// <summary>
    /// A steering behavior that moves the agent towards a target transform,
    /// stopping at a specified distance.
    /// </summary>
    public class ChaseBehavior : ISteeringBehavior
    {
        private float _speed;
        private Transform _target;
        private float _stoppingDistance;

        public ChaseBehavior(float speed, Transform target, float stoppingDistance)
        {
            _speed = speed;
            _target = target;
            _stoppingDistance = stoppingDistance;
        }

        public SteeringOutput GetSteering(EnemyMovementComponent context)
        {
            if (_target == null || context == null)
            {
                return SteeringOutput.Zero;
            }

            Vector2 directionToTarget = _target.position - context.transform.position;
            float distance = directionToTarget.magnitude;

            // If within stopping distance, the chase behavior is "done".
            if (distance <= _stoppingDistance)
            {
                return SteeringOutput.Zero;
            }
            
            // If there's a wall or an edge in the way, stop.
            if (context.IsNearWall || context.IsNearEdge)
            {
                // This simple check works well for ground enemies. It prevents them from walking off ledges
                // or into walls while chasing. More complex pathfinding would be needed for more complex levels.
                return SteeringOutput.Zero;
            }

            // Move towards the target.
            Vector2 desiredVelocity = directionToTarget.normalized * _speed;
            
            return new SteeringOutput(desiredVelocity, true);
        }
    }
}
