using UnityEngine;
using FlyingAI.Core;

namespace FlyingAI.States
{
    /// <summary>
    /// Charge state - kamikaze-style rushing attack.
    /// </summary>
    public class ChargeState : FlyingAIStateBase
    {
        private Vector3 _chargeDirection;
        private float _chargeTimer;
        private float _windupTime = 0.5f;
        private float _maxChargeTime = 3f;
        private bool _isCharging;
        
        public ChargeState(FlyingEnemyBase owner, FlyingAIStateMachine sm) : base(owner, sm) { }
        
        public override void Enter()
        {
            base.Enter();
            _chargeTimer = 0f;
            _isCharging = false;
            _owner.SetVelocity(Vector3.zero);
            
            if (HasTarget())
            {
                _chargeDirection = DirectionToTarget();
            }
            else
            {
                _chargeDirection = _owner.Forward;
            }
        }
        
        public override void Execute()
        {
            _chargeTimer += Time.deltaTime;
            
            // Windup phase
            if (_chargeTimer < _windupTime)
            {
                // Pull back slightly
                _owner.SetVelocity(-_chargeDirection * 2f);
                return;
            }
            
            _isCharging = true;
            
            // Charge forward at high speed
            float chargeSpeed = (_owner.Config?.BaseSpeed ?? 10f) * (_owner.Config?.ChargeSpeedMultiplier ?? 3f);
            _owner.SetVelocity(_chargeDirection * chargeSpeed);
            
            // End charge after max time or if too far from target
            if (_chargeTimer > _windupTime + _maxChargeTime)
            {
                _stateMachine.ChangeState(new RegroupState(_owner, _stateMachine));
                return;
            }
            
            // Hit detection handled by collision
        }
        
        public override void Exit()
        {
            base.Exit();
            _isCharging = false;
        }
    }

    /// <summary>
    /// Evade state - evasive maneuvers when damaged or too close.
    /// </summary>
    public class EvadeState : FlyingAIStateBase
    {
        private Vector3 _evadeDirection;
        private float _evadeTimer;
        private float _evadeDuration = 1.5f;
        
        public EvadeState(FlyingEnemyBase owner, FlyingAIStateMachine sm) : base(owner, sm) { }
        
        public override void Enter()
        {
            base.Enter();
            _evadeTimer = 0f;
            
            // Choose random evasion direction away from target
            if (HasTarget())
            {
                Vector3 awayFromTarget = -DirectionToTarget();
                Vector3 randomOffset = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(0.2f, 0.8f),
                    Random.Range(-1f, 1f)
                ).normalized;
                
                _evadeDirection = (awayFromTarget + randomOffset).normalized;
            }
            else
            {
                _evadeDirection = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(0.2f, 0.8f),
                    Random.Range(-1f, 1f)
                ).normalized;
            }
        }
        
        public override void Execute()
        {
            _evadeTimer += Time.deltaTime;
            
            if (_evadeTimer >= _evadeDuration)
            {
                if (HasTarget() && DistanceToTarget() < _owner.Config.DetectionRadius)
                {
                    _stateMachine.ChangeState(new AttackState(_owner, _stateMachine));
                }
                else
                {
                    _stateMachine.ChangeState(new RegroupState(_owner, _stateMachine));
                }
                return;
            }
            
            // Fast evasive movement with some randomness
            float evadeSpeed = (_owner.Config?.BaseSpeed ?? 10f) * 1.5f;
            
            // Add barrel roll effect
            float rollIntensity = Mathf.Sin(_evadeTimer * 10f) * 0.3f;
            Vector3 rollOffset = Vector3.Cross(_evadeDirection, Vector3.up) * rollIntensity;
            
            _owner.SetVelocity((_evadeDirection + rollOffset).normalized * evadeSpeed);
        }
    }

    /// <summary>
    /// Regroup state - return to formation when scattered.
    /// </summary>
    public class RegroupState : FlyingAIStateBase
    {
        private float _regroupTimer;
        private float _maxRegroupTime = 5f;
        
        public RegroupState(FlyingEnemyBase owner, FlyingAIStateMachine sm) : base(owner, sm) { }
        
        public override void Enter()
        {
            base.Enter();
            _regroupTimer = 0f;
        }
        
        public override void Execute()
        {
            _regroupTimer += Time.deltaTime;
            
            // Check for nearby targets while regrouping
            if (HasTarget() && DistanceToTarget() < _owner.Config.DetectionRadius * 0.7f)
            {
                _stateMachine.ChangeState(new ChaseState(_owner, _stateMachine));
                return;
            }
            
            // Timeout - go to idle
            if (_regroupTimer >= _maxRegroupTime)
            {
                _stateMachine.ChangeState(new IdleState(_owner, _stateMachine));
                return;
            }
            
            // Move towards formation position
            if (_owner.CurrentFormation != null)
            {
                Vector3 formationPos = _owner.GetFormationTargetPosition();
                float distance = Vector3.Distance(_owner.Position, formationPos);
                
                // Reached formation position
                if (distance < 1f)
                {
                    _stateMachine.ChangeState(new PatrolState(_owner, _stateMachine));
                    return;
                }
                
                Vector3 direction = (formationPos - _owner.Position).normalized;
                float speed = _owner.Config?.BaseSpeed ?? 10f;
                _owner.SetVelocity(direction * speed);
            }
            else
            {
                // No formation, just hover
                _stateMachine.ChangeState(new IdleState(_owner, _stateMachine));
            }
        }
    }
}
