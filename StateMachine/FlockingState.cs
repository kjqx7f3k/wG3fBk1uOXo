using UnityEngine;

/// <summary>
/// Boids群集行為狀態
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
        if (boids != null && !boids.IsDead)
        {
            boids.UpdateFlocking();
        }
    }
    
    public override void PhysicsUpdate()
    {
        if (boids != null && !boids.IsDead)
        {
            boids.ApplyMovement();
        }
    }
}