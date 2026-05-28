using UnityEngine;
using System.Collections.Generic;
using FlyingAI.Core;
using FlyingAI.Utils;

namespace FlyingAI.States
{
    /// <summary>
    /// Advanced evasion state with multiple evasion patterns.
    /// Uses TweenHelper for plugin-safe animations.
    /// </summary>
    public class AdvancedEvadeState : FlyingAIStateBase
    {
        #region Evasion Patterns
        
        public enum EvasionPattern
        {
            BarrelRoll,
            SplitS,
            Immelmann,
            Corkscrew,
            JinkManeuver,
            BreakTurn,
            Scissors
        }
        
        #endregion

        #region Private Fields
        
        private EvasionPattern _currentPattern;
        private float _patternTimer;
        private float _patternDuration;
        private Vector3 _initialDirection;
        private Vector3 _evadeAxis;
        private float _rollAngle;
        private int _patternPhase;
        
        #endregion

        #region Constructor
        
        public AdvancedEvadeState(FlyingEnemyBase owner, FlyingAIStateMachine sm) : base(owner, sm)
        {
        }
        
        public AdvancedEvadeState(FlyingEnemyBase owner, FlyingAIStateMachine sm, EvasionPattern pattern) : base(owner, sm)
        {
            _currentPattern = pattern;
        }
        
        #endregion

        #region State Lifecycle
        
        public override void Enter()
        {
            base.Enter();
            
            _patternTimer = 0f;
            _patternPhase = 0;
            _initialDirection = _owner.Forward;
            
            // Choose random pattern if not specified
            if (_currentPattern == default)
            {
                _currentPattern = (EvasionPattern)Random.Range(0, 7);
            }
            
            SetupPattern();
            
            DebugLogger.Log($"Starting evasion: {_currentPattern}", LogCategory.StateMachine);
        }
        
        public override void Execute()
        {
            _patternTimer += Time.deltaTime;
            
            if (_patternTimer >= _patternDuration)
            {
                ExitEvasion();
                return;
            }
            
            ExecutePattern();
        }
        
        public override void Exit()
        {
            base.Exit();
            TweenHelper.StopAll(_owner.transform);
        }
        
        #endregion

        #region Pattern Setup
        
        private void SetupPattern()
        {
            switch (_currentPattern)
            {
                case EvasionPattern.BarrelRoll:
                    SetupBarrelRoll();
                    break;
                case EvasionPattern.SplitS:
                    SetupSplitS();
                    break;
                case EvasionPattern.Immelmann:
                    SetupImmelmann();
                    break;
                case EvasionPattern.Corkscrew:
                    SetupCorkscrew();
                    break;
                case EvasionPattern.JinkManeuver:
                    SetupJink();
                    break;
                case EvasionPattern.BreakTurn:
                    SetupBreakTurn();
                    break;
                case EvasionPattern.Scissors:
                    SetupScissors();
                    break;
            }
        }
        
        private void SetupBarrelRoll()
        {
            _patternDuration = 1.5f;
            _evadeAxis = _initialDirection;
            _rollAngle = Random.value > 0.5f ? 360f : -360f;
        }
        
        private void SetupSplitS()
        {
            _patternDuration = 2f;
            _evadeAxis = -Vector3.up;
        }
        
        private void SetupImmelmann()
        {
            _patternDuration = 2f;
            _evadeAxis = Vector3.up;
        }
        
        private void SetupCorkscrew()
        {
            _patternDuration = 3f;
            _evadeAxis = _initialDirection;
        }
        
        private void SetupJink()
        {
            _patternDuration = 1f;
            _evadeAxis = Random.value > 0.5f ? _owner.transform.right : -_owner.transform.right;
        }
        
        private void SetupBreakTurn()
        {
            _patternDuration = 1.5f;
            _evadeAxis = Random.value > 0.5f ? _owner.transform.right : -_owner.transform.right;
        }
        
        private void SetupScissors()
        {
            _patternDuration = 2f;
        }
        
        #endregion

        #region Pattern Execution
        
        private void ExecutePattern()
        {
            float t = _patternTimer / _patternDuration;
            float speed = _owner.Config?.BaseSpeed ?? 10f;
            
            switch (_currentPattern)
            {
                case EvasionPattern.BarrelRoll:
                    ExecuteBarrelRoll(t, speed);
                    break;
                case EvasionPattern.SplitS:
                    ExecuteSplitS(t, speed);
                    break;
                case EvasionPattern.Immelmann:
                    ExecuteImmelmann(t, speed);
                    break;
                case EvasionPattern.Corkscrew:
                    ExecuteCorkscrew(t, speed);
                    break;
                case EvasionPattern.JinkManeuver:
                    ExecuteJink(t, speed);
                    break;
                case EvasionPattern.BreakTurn:
                    ExecuteBreakTurn(t, speed);
                    break;
                case EvasionPattern.Scissors:
                    ExecuteScissors(t, speed);
                    break;
            }
        }
        
        private void ExecuteBarrelRoll(float t, float speed)
        {
            // Maintain forward movement while rolling
            float rollT = t * _rollAngle * Mathf.Deg2Rad;
            
            Vector3 right = Vector3.Cross(_initialDirection, Vector3.up);
            Vector3 up = Vector3.Cross(right, _initialDirection);
            
            // Spiral around forward axis
            float radius = 3f;
            Vector3 offset = (right * Mathf.Cos(rollT) + up * Mathf.Sin(rollT)) * radius;
            
            Vector3 velocity = _initialDirection * speed + offset.normalized * speed * 0.3f;
            _owner.SetVelocity(velocity);
        }
        
        private void ExecuteSplitS(float t, float speed)
        {
            // Half-roll then dive
            if (t < 0.3f)
            {
                // Initial turn
                Vector3 turnDir = -_owner.transform.right;
                _owner.SetVelocity((_initialDirection + turnDir * 0.5f).normalized * speed);
            }
            else
            {
                // Dive and level out
                float diveT = (t - 0.3f) / 0.7f;
                Vector3 diveDir = Vector3.Lerp(Vector3.down, -_initialDirection, diveT);
                _owner.SetVelocity(diveDir.normalized * speed * 1.5f);
            }
        }
        
        private void ExecuteImmelmann(float t, float speed)
        {
            // Climb then half-roll to reverse direction
            if (t < 0.5f)
            {
                // Climb
                float climbT = t / 0.5f;
                Vector3 climbDir = Vector3.Lerp(_initialDirection, Vector3.up, climbT);
                _owner.SetVelocity(climbDir.normalized * speed);
            }
            else
            {
                // Level out in opposite direction
                float levelT = (t - 0.5f) / 0.5f;
                Vector3 levelDir = Vector3.Lerp(Vector3.up, -_initialDirection, levelT);
                _owner.SetVelocity(levelDir.normalized * speed);
            }
        }
        
        private void ExecuteCorkscrew(float t, float speed)
        {
            // Continuous spiral while moving forward
            float spiralT = t * Mathf.PI * 4f;
            float radius = 2f + Mathf.Sin(t * Mathf.PI) * 2f;
            
            Vector3 right = Vector3.Cross(_initialDirection, Vector3.up);
            Vector3 up = Vector3.Cross(right, _initialDirection);
            
            Vector3 spiralOffset = (right * Mathf.Cos(spiralT) + up * Mathf.Sin(spiralT)) * radius;
            Vector3 velocity = _initialDirection * speed + spiralOffset.normalized * speed * 0.5f;
            
            _owner.SetVelocity(velocity);
        }
        
        private void ExecuteJink(float t, float speed)
        {
            // Sharp lateral movement
            float jinkIntensity = Mathf.Sin(t * Mathf.PI) * 2f;
            Vector3 velocity = _initialDirection * speed * 0.5f + _evadeAxis * speed * jinkIntensity;
            _owner.SetVelocity(velocity);
        }
        
        private void ExecuteBreakTurn(float t, float speed)
        {
            // High-G turn away from threat
            float turnAngle = t * 180f * Mathf.Deg2Rad;
            Vector3 turnedDir = Quaternion.AngleAxis(turnAngle * Mathf.Rad2Deg, Vector3.up) * _initialDirection;
            
            // Add slight climb
            turnedDir += Vector3.up * 0.2f;
            
            _owner.SetVelocity(turnedDir.normalized * speed * 1.3f);
        }
        
        private void ExecuteScissors(float t, float speed)
        {
            // Weaving left-right pattern
            int phase = Mathf.FloorToInt(t * 4f);
            float phaseT = (t * 4f) % 1f;
            
            Vector3 weaveDir = (phase % 2 == 0) ? _owner.transform.right : -_owner.transform.right;
            float weaveIntensity = Mathf.Sin(phaseT * Mathf.PI);
            
            Vector3 velocity = _initialDirection * speed * 0.7f + weaveDir * speed * weaveIntensity;
            
            // Add slight vertical variation
            velocity.y = Mathf.Sin(t * Mathf.PI * 2f) * 2f;
            
            _owner.SetVelocity(velocity);
        }
        
        #endregion

        #region Exit
        
        private void ExitEvasion()
        {
            if (HasTarget() && DistanceToTarget() < _owner.Config.DetectionRadius)
            {
                _stateMachine.ChangeState(new AttackState(_owner, _stateMachine));
            }
            else
            {
                _stateMachine.ChangeState(new RegroupState(_owner, _stateMachine));
            }
        }
        
        #endregion
    }

    /// <summary>
    /// Defensive formation state for when under heavy fire.
    /// </summary>
    public class DefensiveFormationState : FlyingAIStateBase
    {
        private Vector3 _formationCenter;
        private float _orbitAngle;
        private float _orbitRadius = 8f;
        private float _orbitSpeed = 90f;
        
        public DefensiveFormationState(FlyingEnemyBase owner, FlyingAIStateMachine sm) : base(owner, sm) { }
        
        public override void Enter()
        {
            base.Enter();
            
            if (_owner.CurrentFormation != null)
            {
                _formationCenter = _owner.CurrentFormation.CenterPosition;
            }
            else
            {
                _formationCenter = _owner.Position;
            }
            
            // Start at current angle relative to center
            Vector3 toOwner = _owner.Position - _formationCenter;
            _orbitAngle = Mathf.Atan2(toOwner.z, toOwner.x) * Mathf.Rad2Deg;
        }
        
        public override void Execute()
        {
            // Orbit defensively around formation center
            _orbitAngle += _orbitSpeed * Time.deltaTime;
            
            float rad = _orbitAngle * Mathf.Deg2Rad;
            Vector3 targetPos = _formationCenter + new Vector3(
                Mathf.Cos(rad) * _orbitRadius,
                0f,
                Mathf.Sin(rad) * _orbitRadius
            );
            
            Vector3 toTarget = targetPos - _owner.Position;
            float speed = _owner.Config?.BaseSpeed ?? 10f;
            _owner.SetVelocity(toTarget.normalized * speed);
            
            // Exit after one full orbit or if threat clears
            if (_stateMachine.TimeInCurrentState > 4f)
            {
                _stateMachine.ChangeState(new RegroupState(_owner, _stateMachine));
            }
        }
    }
}
