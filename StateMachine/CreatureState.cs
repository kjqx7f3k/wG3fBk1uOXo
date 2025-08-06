using UnityEngine;

public abstract class CreatureState
{
    protected Creature creature;
    protected StateMachine stateMachine;
    
    public CreatureState(Creature creature, StateMachine stateMachine)
    {
        this.creature = creature;
        this.stateMachine = stateMachine;
    }
    
    public virtual void EnterState()
    {
        // 進入狀態時的邏輯
    }
    
    public virtual void ExitState()
    {
        // 退出狀態時的邏輯
    }
    
    public virtual void FrameUpdate()
    {
        // 每幀更新的邏輯
    }
    
    public virtual void PhysicsUpdate()
    {
        // 固定時間更新的邏輯
    }
} 