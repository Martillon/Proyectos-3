// En una carpeta, ej: Scripts/Enemies/Movement/SteeringBehaviors/Implementations
using UnityEngine;

namespace Scripts.Enemies.Movement.SteeringBehaviors.Implementations
{
    public class PatrolBehavior2D : ISteeringBehavior2D
    {
        private float _patrolSpeed;
        private float _waitTime;
        private float _moveTime;

        private int _patrolDirection = 1; // 1 for right, -1 for left
        private float _timer;
        private bool _isWaiting;

        public PatrolBehavior2D(float patrolSpeed, float waitTime, float moveTime)
        {
            _patrolSpeed = patrolSpeed;
            _waitTime = waitTime;
            _moveTime = moveTime;
            _timer = 0f; // Inicia moviéndose
            _isWaiting = false;
        }
        
        public void UpdatePatrolParameters(float speed, float waitTime, float moveTime)
        {
            _patrolSpeed = speed;
            _waitTime = waitTime;
            _moveTime = moveTime;
        }


        public SteeringOutput2D GetSteering(EnemyMovementComponent context)
        {
            _timer += Time.deltaTime;

            if (_isWaiting)
            {
                if (_timer >= _waitTime)
                {
                    _isWaiting = false;
                    _timer = 0f;
                    // La dirección ya se cambió cuando decidió esperar
                }
                return SteeringOutput2D.Zero; // No moverse mientras espera
            }
            else // Está moviéndose
            {
                bool shouldStopAndWait = _timer >= _moveTime;
                
                // Comprobar si hay que detenerse por bordes o paredes usando el contexto
                // Nota: context.transform.position es la posición actual del enemigo
                if (context.IsNearEdge || context.IsNearWall || !context.IsGrounded)
                {
                    // Solo considera borde/pared si está en la dirección del movimiento
                    if ((_patrolDirection > 0 && (context.IsNearEdge || context.IsNearWall)) || // Moviéndose a la derecha y hay borde/pared a la derecha
                        (_patrolDirection < 0 && (context.IsNearEdge || context.IsNearWall)))   // Moviéndose a la izquierda y hay borde/pared a la izquierda
                    {
                        shouldStopAndWait = true;
                    }
                    else if (!context.IsGrounded) // Si no está en el suelo, también parar
                    {
                        shouldStopAndWait = true;
                    }
                }

                if (shouldStopAndWait)
                {
                    _isWaiting = true;
                    _timer = 0f;
                    _patrolDirection *= -1; // Invertir dirección para la próxima vez
                    return SteeringOutput2D.Zero; // Detenerse
                }

                // Si no hay que detenerse, seguir moviéndose
                return new SteeringOutput2D(new Vector2(_patrolDirection * _patrolSpeed, 0f), true);
            }
        }
    }
}
