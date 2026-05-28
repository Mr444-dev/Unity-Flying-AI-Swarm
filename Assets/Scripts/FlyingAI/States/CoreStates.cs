using UnityEngine;
using FlyingAI.Core;

namespace FlyingAI.States
{
    /// <summary>
    /// Idle state - hovering in place with subtle animation.
    /// </summary>
    public class IdleState : FlyingAIStateBase
    {
        private float _hoverTime;
        private Vector3 _basePosition;
        
        public IdleState(FlyingEnemyBase owner, FlyingAIStateMachine sm) : base(owner, sm) { }
        
        public override void Enter()
        {
            base.Enter();
            _hoverTime = 0f;
            _basePosition = _owner.Position;
            _owner.SetVelocity(Vector3.zero);
        }
        
        public override void Execute()
        {
            _hoverTime += Time.deltaTime;
            
            // Check for targets
            if (HasTarget() && DistanceToTarget() < _owner.Config.DetectionRadius)
            {
                _stateMachine.ChangeState(new ChaseState(_owner, _stateMachine));
                return;
            }
            
            // Subtle hover movement
            if (_owner.Config != null)
            {
                float hoverY = Mathf.Sin(_hoverTime * _owner.Config.HoverFrequency) * _owner.Config.HoverAmplitude;
                Vector3 hoverVelocity = new Vector3(0, hoverY, 0);
                _owner.SetVelocity(hoverVelocity);
            }
        }
    }

    /// <summary>
    /// Patrol state - waypoint-based movement in formation.
    /// </summary>
    public class PatrolState : FlyingAIStateBase
    {
        private Vector3[] _waypoints;
        private int _currentWaypoint;
        private float _waypointThreshold = 2f;
        
        public PatrolState(FlyingEnemyBase owner, FlyingAIStateMachine sm, Vector3[] waypoints = null) : base(owner, sm)
        {
            _waypoints = waypoints ?? new Vector3[] { owner.Position };
        }
        
        public override void Enter()
        {
            base.Enter();
            _currentWaypoint = 0;
        }
        
        public override void Execute()
        {
            // Check for targets
            if (HasTarget() && DistanceToTarget() < _owner.Config.DetectionRadius)
            {
                _stateMachine.ChangeState(new ChaseState(_owner, _stateMachine));
                return;
            }
            
            // Move towards current waypoint
            if (_waypoints.Length > 0)
            {
                Vector3 target = _waypoints[_currentWaypoint];
                Vector3 direction = (target - _owner.Position).normalized;
                float speed = _owner.Config?.BaseSpeed ?? 10f;
                
                _owner.SetVelocity(direction * speed);
                
                // Check if reached waypoint
                if (Vector3.Distance(_owner.Position, target) < _waypointThreshold)
                {
                    _currentWaypoint = (_currentWaypoint + 1) % _waypoints.Length;
                }
            }
        }
    }

    /// <summary>
    /// Chase state - pursuit behavior with prediction.
    /// </summary>
    public class ChaseState : FlyingAIStateBase
    {
        public ChaseState(FlyingEnemyBase owner, FlyingAIStateMachine sm) : base(owner, sm) { }
        
        public override void Execute()
        {
            if (!HasTarget())
            {
                _stateMachine.ChangeState(new RegroupState(_owner, _stateMachine));
                return;
            }
            
            float distance = DistanceToTarget();
            
            // Transition to attack if in range
            if (distance <= _owner.Config.AttackRange)
            {
                _stateMachine.ChangeState(new AttackState(_owner, _stateMachine));
                return;
            }
            
            // Lost target
            if (distance > _owner.Config.DetectionRadius * 1.5f)
            {
                _stateMachine.ChangeState(new RegroupState(_owner, _stateMachine));
                return;
            }
            
            // Predictive pursuit
            Vector3 targetPos = _owner.CurrentTarget.position;
            Rigidbody targetRb = _owner.CurrentTarget.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                float timeToReach = distance / (_owner.Config?.BaseSpeed ?? 10f);
                targetPos += targetRb.linearVelocity * timeToReach * 0.5f;
            }
            
            Vector3 direction = (targetPos - _owner.Position).normalized;
            float speed = _owner.Config?.BaseSpeed ?? 10f;
            _owner.SetVelocity(direction * speed * 1.2f);
        }
    }

    /// <summary>
    /// Attack state - combat engagement with shooting.
    /// </summary>
    public class AttackState : FlyingAIStateBase
    {
        private float _attackTimer;
        private float _strafeTimer;
        private int _strafeDirection = 1;
        
        public AttackState(FlyingEnemyBase owner, FlyingAIStateMachine sm) : base(owner, sm) { }
        
        public override void Enter()
        {
            base.Enter();
            _attackTimer = 0f;
            _strafeTimer = 0f;
            _strafeDirection = Random.value > 0.5f ? 1 : -1;
        }
        
        public override void Execute()
        {
            if (!HasTarget())
            {
                _stateMachine.ChangeState(new RegroupState(_owner, _stateMachine));
                return;
            }
            
            float distance = DistanceToTarget();
            
            // Out of attack range
            if (distance > _owner.Config.AttackRange * 1.2f)
            {
                _stateMachine.ChangeState(new ChaseState(_owner, _stateMachine));
                return;
            }
            
            // Too close - evade
            if (distance < _owner.Config.MinAttackDistance)
            {
                _stateMachine.ChangeState(new EvadeState(_owner, _stateMachine));
                return;
            }
            
            // Chance to charge
            _attackTimer += Time.deltaTime;
            if (_attackTimer > 3f && Random.value < _owner.Config.ChargeChance * Time.deltaTime)
            {
                _stateMachine.ChangeState(new ChargeState(_owner, _stateMachine));
                return;
            }
            
            // Strafing movement
            _strafeTimer += Time.deltaTime;
            if (_strafeTimer > 2f)
            {
                _strafeDirection *= -1;
                _strafeTimer = 0f;
            }
            
            Vector3 toTarget = DirectionToTarget();
            Vector3 strafeDir = Vector3.Cross(toTarget, Vector3.up) * _strafeDirection;
            Vector3 moveDir = (toTarget * 0.3f + strafeDir * 0.7f).normalized;
            
            float speed = _owner.Config?.BaseSpeed ?? 10f;
            _owner.SetVelocity(moveDir * speed * 0.5f);
        }
    }
}
