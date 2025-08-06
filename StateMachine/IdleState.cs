using UnityEngine;

public class IdleState : CreatureState
{
    public IdleState(Creature creature, StateMachine stateMachine) : base(creature, stateMachine)
    {
    }

    public override void EnterState()
    {
        Debug.Log($"{creature.name} entered Idle state");
    }

    public override void ExitState()
    {
        Debug.Log($"{creature.name} exited Idle state");
    }

    public override void FrameUpdate()
    {
        // 閒置狀態的每幀更新邏輯
        // 例如：檢查是否需要切換到移動或攻擊狀態
    }

    public override void PhysicsUpdate()
    {
        // 閒置狀態的物理更新邏輯
    }
}
