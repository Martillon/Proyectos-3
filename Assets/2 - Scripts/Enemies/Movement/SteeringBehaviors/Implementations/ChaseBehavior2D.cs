// En Scripts/Enemies/Movement/SteeringBehaviors/Implementations/ChaseBehavior2D.cs
using UnityEngine;

namespace Scripts.Enemies.Movement.SteeringBehaviors.Implementations
{
    public class ChaseBehavior2D : ISteeringBehavior2D
    {
        private float _chaseSpeed;
        private Transform _target;
        private float _stoppingDistance;

        public ChaseBehavior2D(float chaseSpeed, Transform target, float stoppingDistance)
        {
            _chaseSpeed = chaseSpeed;
            _target = target;
            _stoppingDistance = stoppingDistance;
        }

        public void UpdateTarget(Transform newTarget)
        {
            _target = newTarget;
        }

        public void UpdateChaseSpeed(float newSpeed)
        {
            _chaseSpeed = newSpeed;
        }

        public void UpdateStoppingDistance(float newDistance)
        {
            _stoppingDistance = newDistance;
        }

        public SteeringOutput2D GetSteering(EnemyMovementComponent context)
        {
            if (_target == null || context == null)
            {
                return SteeringOutput2D.Zero; // No hay objetivo o contexto, no moverse
            }

            Vector2 currentPosition = context.transform.position;
            Vector2 targetPosition = _target.position;
            float distanceToTarget = Vector2.Distance(currentPosition, targetPosition);

            if (distanceToTarget <= _stoppingDistance)
            {
                // Ya está en el rango de "stopping distance" (engagement range)
                // AIController decidirá si atacar o esperar. El behavior de chase indica "detente".
                return SteeringOutput2D.Zero;
            }

            Vector2 directionToTarget = (targetPosition - currentPosition).normalized;
            float desiredHorizontalSpeed = 0f;
            bool shouldOrient = true; // Por defecto, orientarse hacia el movimiento

            // Determinar la dirección horizontal del movimiento deseado (-1, 0, 1)
            float moveInputDirection = 0f;
            if (Mathf.Abs(directionToTarget.x) > 0.01f) // Solo moverse si hay una componente horizontal significativa
            {
                moveInputDirection = Mathf.Sign(directionToTarget.x);
            }

            if (moveInputDirection != 0) // Si hay intención de moverse horizontalmente
            {
                bool obstacleInPath = false;

                // ¿Está el enemigo mirando en la dirección que quiere moverse?
                bool isFacingMovementDirection = (moveInputDirection > 0 && context.CurrentFacingRight) ||
                                                 (moveInputDirection < 0 && !context.CurrentFacingRight);

                if (isFacingMovementDirection)
                {
                    // Solo comprobar pared/borde si está mirando en la dirección del movimiento
                    if (context.IsNearWall || context.IsNearEdge)
                    {
                        obstacleInPath = true;
                    }
                }
                else if(context.IsNearWall) // Si NO está mirando en la dirección del movimiento, pero hay una pared justo detrás
                {
                    // Esto es para evitar que se quede pegado si se gira y tiene una pared detrás.
                    // Es una condición un poco más compleja. Si se mueve hacia la pared de su espalda.
                    // Por ejemplo, si mira a la derecha, y quiere ir a la izquierda, pero hay una pared a la izquierda.
                    // context.IsNearWall comprueba la pared en la dirección que enfrenta.
                    // Aquí necesitamos una forma de saber si hay pared en la dirección opuesta.
                    // Por simplicidad, la lógica actual de IsNearWall es solo para el frente.
                    // Si esta situación se vuelve un problema, necesitaríamos una comprobación de "pared trasera".
                    // Por ahora, asumimos que si no enfrenta el movimiento Y hay una pared frontal, no es un problema para ESTE movimiento.
                }


                if (!context.IsGrounded && distanceToTarget > 0.2f) // Si no está en el suelo (cayendo)
                {
                    // Si está cayendo, no aplicar velocidad horizontal controlada por IA
                    // para permitir que la gravedad actúe naturalmente o que el jugador lo evite.
                    // Podrías querer que siga intentando moverse horizontalmente en el aire hacia el jugador.
                    // Esto depende del diseño. Por ahora, no le damos velocidad horizontal si no está en suelo.
                    // desiredHorizontalSpeed = 0f;
                    // obstacleInPath = true; // Comentado para permitir movimiento aéreo si se desea

                    // Comportamiento alternativo: permitir control aéreo
                    desiredHorizontalSpeed = moveInputDirection * _chaseSpeed;
                }
                else if (obstacleInPath)
                {
                    desiredHorizontalSpeed = 0f; // Detenerse si hay un obstáculo frontal
                }
                else
                {
                    desiredHorizontalSpeed = moveInputDirection * _chaseSpeed;
                }
            }
            else // No hay componente horizontal significativa hacia el objetivo (está casi verticalmente alineado)
            {
                desiredHorizontalSpeed = 0f;
                shouldOrient = false; // No necesita reorientarse si está verticalmente alineado y no se mueve horizontalmente
            }

            return new SteeringOutput2D(new Vector2(desiredHorizontalSpeed, 0f), shouldOrient);
        }
    }
}
