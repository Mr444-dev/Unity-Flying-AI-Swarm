using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace FlyingAI.Flocking
{
    [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
    public struct FormationJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> SlotOffsets;
        [ReadOnly] public NativeArray<int> EntitySlotIndices;
        public float3 FormationCenter;
        public float3 FormationForward;
        public float3 FormationUp;
        public float ScaleFactor;
        [WriteOnly] public NativeArray<float3> TargetPositions;
        
        public void Execute(int index)
        {
            int slotIndex = EntitySlotIndices[index];
            if (slotIndex < 0 || slotIndex >= SlotOffsets.Length)
            {
                TargetPositions[index] = FormationCenter;
                return;
            }
            
            float3 slotOffset = SlotOffsets[slotIndex] * ScaleFactor;
            quaternion rotation = quaternion.LookRotationSafe(FormationForward, FormationUp);
            float3 worldOffset = math.mul(rotation, slotOffset);
            TargetPositions[index] = FormationCenter + worldOffset;
        }
    }

    [BurstCompile]
    public struct ApplyFlockingResultsJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<FlockingResult> Results;
        [ReadOnly] public NativeArray<float3> AvoidanceForces;
        public float AvoidanceWeight;
        public float DeltaTime;
        public float MaxSpeed;
        public NativeArray<float3> FinalPositions;
        public NativeArray<float3> FinalVelocities;
        public NativeArray<quaternion> FinalRotations;
        
        public void Execute(int index)
        {
            FlockingResult result = Results[index];
            float3 avoidance = AvoidanceForces[index];
            float3 velocity = result.NewVelocity + avoidance * AvoidanceWeight;
            float speed = math.length(velocity);
            if (speed > MaxSpeed) velocity = (velocity / speed) * MaxSpeed;
            float3 position = result.NewPosition + avoidance * AvoidanceWeight * DeltaTime;
            quaternion rotation = quaternion.identity;
            if (speed > 0.01f) rotation = quaternion.LookRotationSafe(math.normalizesafe(velocity), new float3(0,1,0));
            FinalPositions[index] = position;
            FinalVelocities[index] = velocity;
            FinalRotations[index] = rotation;
        }
    }

    [BurstCompile]
    public struct SmoothPositionJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> TargetPositions;
        public NativeArray<float3> CurrentPositions;
        public float SmoothSpeed;
        public float DeltaTime;
        
        public void Execute(int index)
        {
            CurrentPositions[index] = math.lerp(CurrentPositions[index], TargetPositions[index], SmoothSpeed * DeltaTime);
        }
    }

    [BurstCompile]
    public struct SmoothRotationJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<quaternion> TargetRotations;
        public NativeArray<quaternion> CurrentRotations;
        public float SmoothSpeed;
        public float DeltaTime;
        
        public void Execute(int index)
        {
            CurrentRotations[index] = math.slerp(CurrentRotations[index], TargetRotations[index], SmoothSpeed * DeltaTime);
        }
    }
}
