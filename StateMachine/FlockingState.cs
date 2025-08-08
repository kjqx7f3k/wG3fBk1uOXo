using UnityEngine;

/// <summary>
/// Boids群集行為狀態
/// 頻率控制已移至 BoidsManager 統一管理
/// </summary>
public class FlockingState : CreatureState
{
    private Boids boids;

    public FlockingState(Creature creature, StateMachine stateMachine) : base(creature, stateMachine)
    {
        boids = creature as Boids;
    }
    
    public override void EnterState()
    {
        if (boids != null)
        {
            Debug.Log($"{boids.name} 進入群集狀態");
        }
    }
    
    public override void ExitState()
    {
        if (boids != null)
        {
            Debug.Log($"{boids.name} 離開群集狀態");
        }
    }
    
    public override void FrameUpdate()
    {
        // 每幀調用群集更新，頻率控制已移至 Boids 內部
        // 由 BoidsManager 統一管理所有頻率設定
        if (boids != null && !boids.IsDead)
        {
            boids.UpdateFlocking();
        }
    }
    
    public override void PhysicsUpdate()
    {
        // 物理更新每幀執行，確保移動平順
        if (boids != null && !boids.IsDead)
        {
            boids.ApplyMovement();
        }
    }
}