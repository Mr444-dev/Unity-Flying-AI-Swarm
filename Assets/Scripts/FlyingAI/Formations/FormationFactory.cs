using FlyingAI.Core;
using FlyingAI.ScriptableObjects;
using FlyingAI.Utils;

namespace FlyingAI.Formations
{
    /// <summary>
    /// Factory for creating formation instances.
    /// Uses factory pattern for decoupled creation.
    /// </summary>
    public static class FormationFactory
    {
        /// <summary>
        /// Creates a formation instance based on configuration.
        /// </summary>
        public static IFormation Create(FormationConfig config)
        {
            if (config == null)
            {
                DebugLogger.LogWarning("Null config passed to FormationFactory, creating default V formation", LogCategory.Formation);
                return new VFormation(null);
            }
            
            IFormation formation = config.Type switch
            {
                FormationType.V => new VFormation(config),
                FormationType.Circle => new CircleFormation(config),
                FormationType.Grid => new GridFormation(config),
                FormationType.Line => new LineFormation(config),
                FormationType.Diamond => new DiamondFormation(config),
                FormationType.Custom => new CustomFormation(config),
                _ => new VFormation(config)
            };
            
            DebugLogger.Log($"Created {config.Type} formation with {formation.MaxSlotCount} slots", LogCategory.Formation);
            return formation;
        }
        
        /// <summary>
        /// Creates a formation of specified type with default settings.
        /// </summary>
        public static IFormation Create(FormationType type)
        {
            return type switch
            {
                FormationType.V => new VFormation(null),
                FormationType.Circle => new CircleFormation(null),
                FormationType.Grid => new GridFormation(null),
                FormationType.Line => new LineFormation(null),
                FormationType.Diamond => new DiamondFormation(null),
                _ => new VFormation(null)
            };
        }
    }
}
