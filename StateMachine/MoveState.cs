using UnityEngine;

public class MoveState : CreatureState
{
    public MoveState(Creature creature, StateMachine stateMachine) : base(creature, stateMachine)
    {
    }

    public override void EnterState()
    {
        Debug.Log($"{creature.name} entered Move state");
    }

    public override void ExitState()
    {
        Debug.Log($"{creature.name} exited Move state");
    }

    public override void FrameUpdate()
    {
        // 移動狀態的每幀更新邏輯
        // 例如：處理移動輸入、更新動畫等
    }

    public override void PhysicsUpdate()
    {
        // 移動狀態的物理更新邏輯
        // 例如：實際的移動計算、碰撞檢測等
    }
}
