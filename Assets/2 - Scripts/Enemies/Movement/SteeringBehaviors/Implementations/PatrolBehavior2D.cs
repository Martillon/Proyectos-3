using UnityEngine;

namespace Scripts.Enemies.Movement.SteeringBehaviors.Implementations
{
    /// <summary>
    /// A steering behavior that moves the agent back and forth, waiting at each end.
    /// It automatically reverses direction when hitting a wall or an edge.
    /// </summary>
    public class PatrolBehavior : ISteeringBehavior
    {
        private readonly float _speed;
        private readonly float _waitTime;
        private readonly float _moveTime;

        private float _timer;
        private bool _isWaiting;
        private int _patrolDirection = 1; // 1 for right, -1 for left

        public PatrolBehavior(float speed, float waitTime, float moveTime)
        {
            _speed = speed;
            _waitTime = waitTime;
            _moveTime = moveTime;
            _timer = moveTime; // Start in a "moving" state
        }

        public SteeringOutput GetSteering(EnemyMovementComponent context)
        {
            _timer += Time.deltaTime;

            if (_isWaiting)
            {
                if (_timer >= _waitTime)
                {
                    _isWaiting = false;
                    _timer = 0;
                }
                // While waiting, do not move.
                return SteeringOutput.Zero;
            }
            else // Is moving
            {
                // Check for reasons to stop and turn around
                bool shouldTurn = (_timer >= _moveTime) || context.IsNearWall || context.IsNearEdge;
                
                if (shouldTurn)
                {
                    _isWaiting = true;
                    _timer = 0;
                    _patrolDirection *= -1; // Flip direction
                    return SteeringOutput.Zero;
                }

                // If no reason to stop, continue moving.
                Vector2 desiredVelocity = new Vector2(_patrolDirection * _speed, 0);
                return new SteeringOutput(desiredVelocity, true);
            }
        }
    }
}
