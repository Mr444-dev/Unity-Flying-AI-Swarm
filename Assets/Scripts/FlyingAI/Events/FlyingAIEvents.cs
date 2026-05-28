using UnityEngine;
using System;
using FlyingAI.Core;

namespace FlyingAI.Events
{
    /// <summary>
    /// Event arguments for enemy-related events.
    /// </summary>
    public class EnemyEventArgs : EventArgs
    {
        public FlyingEnemyBase Enemy { get; }
        public EnemyEventArgs(FlyingEnemyBase enemy) => Enemy = enemy;
    }

    public class EnemyDamageEventArgs : EnemyEventArgs
    {
        public DamageInfo DamageInfo { get; }
        public EnemyDamageEventArgs(FlyingEnemyBase enemy, DamageInfo info) : base(enemy) => DamageInfo = info;
    }

    public class EnemyPriorityEventArgs : EnemyEventArgs
    {
        public int OldPriority { get; }
        public int NewPriority { get; }
        public EnemyPriorityEventArgs(FlyingEnemyBase enemy, int oldPri, int newPri) : base(enemy)
        {
            OldPriority = oldPri;
            NewPriority = newPri;
        }
    }

    public class FormationEventArgs : EventArgs
    {
        public IFormation Formation { get; }
        public FormationEventArgs(IFormation formation) => Formation = formation;
    }

    /// <summary>
    /// Static event hub for flying AI system events.
    /// Provides decoupled communication between systems.
    /// </summary>
    public static class FlyingAIEvents
    {
        #region Enemy Events
        
        /// <summary>Fired when an enemy is spawned.</summary>
        public static event Action<FlyingEnemyBase> OnEnemySpawned;
        
        /// <summary>Fired when an enemy dies.</summary>
        public static event Action<FlyingEnemyBase> OnEnemyDied;
        
        /// <summary>Fired when an enemy takes damage.</summary>
        public static event Action<FlyingEnemyBase, DamageInfo> OnEnemyDamaged;
        
        /// <summary>Fired when enemy priority changes.</summary>
        public static event Action<FlyingEnemyBase, int, int> OnPriorityChanged;
        
        /// <summary>Fired when enemy changes state.</summary>
        public static event Action<FlyingEnemyBase, IState, IState> OnStateChanged;
        
        #endregion

        #region Formation Events
        
        /// <summary>Fired when a formation is created.</summary>
        public static event Action<IFormation> OnFormationCreated;
        
        /// <summary>Fired when a formation is disbanded.</summary>
        public static event Action<IFormation> OnFormationDisbanded;
        
        /// <summary>Fired when formation slots are reassigned.</summary>
        public static event Action<IFormation> OnFormationReassigned;
        
        #endregion

        #region Wave Events
        
        /// <summary>Fired when a wave starts.</summary>
        public static event Action<int> OnWaveStarted;
        
        /// <summary>Fired when a wave is completed.</summary>
        public static event Action<int, int> OnWaveCompleted; // waveIndex, killCount
        
        /// <summary>Fired when all waves are complete.</summary>
        public static event Action OnAllWavesComplete;
        
        #endregion

        #region Invoke Methods
        
        public static void RaiseEnemySpawned(FlyingEnemyBase enemy) => OnEnemySpawned?.Invoke(enemy);
        public static void RaiseEnemyDied(FlyingEnemyBase enemy) => OnEnemyDied?.Invoke(enemy);
        public static void RaiseEnemyDamaged(FlyingEnemyBase enemy, DamageInfo info) => OnEnemyDamaged?.Invoke(enemy, info);
        public static void RaisePriorityChanged(FlyingEnemyBase enemy, int old, int newPri) => OnPriorityChanged?.Invoke(enemy, old, newPri);
        public static void RaiseStateChanged(FlyingEnemyBase enemy, IState prev, IState curr) => OnStateChanged?.Invoke(enemy, prev, curr);
        
        public static void RaiseFormationCreated(IFormation formation) => OnFormationCreated?.Invoke(formation);
        public static void RaiseFormationDisbanded(IFormation formation) => OnFormationDisbanded?.Invoke(formation);
        public static void RaiseFormationReassigned(IFormation formation) => OnFormationReassigned?.Invoke(formation);
        
        public static void RaiseWaveStarted(int waveIndex) => OnWaveStarted?.Invoke(waveIndex);
        public static void RaiseWaveCompleted(int waveIndex, int kills) => OnWaveCompleted?.Invoke(waveIndex, kills);
        public static void RaiseAllWavesComplete() => OnAllWavesComplete?.Invoke();
        
        #endregion

        #region Cleanup
        
        /// <summary>Clears all event subscriptions. Call on scene unload.</summary>
        public static void ClearAll()
        {
            OnEnemySpawned = null;
            OnEnemyDied = null;
            OnEnemyDamaged = null;
            OnPriorityChanged = null;
            OnStateChanged = null;
            OnFormationCreated = null;
            OnFormationDisbanded = null;
            OnFormationReassigned = null;
            OnWaveStarted = null;
            OnWaveCompleted = null;
            OnAllWavesComplete = null;
        }
        
        #endregion
    }
}
