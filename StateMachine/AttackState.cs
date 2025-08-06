using UnityEngine;

public class AttackState : CreatureState
{
    public AttackState(Creature creature, StateMachine stateMachine) : base(creature, stateMachine)
    {
    }

    public override void EnterState()
    {
        Debug.Log($"{creature.name} entered Attack state");
    }

    public override void ExitState()
    {
        Debug.Log($"{creature.name} exited Attack state");
    }

    public override void FrameUpdate()
    {
        // 攻擊狀態的每幀更新邏輯
        // 例如：處理攻擊動畫、攻擊判定等
    }

    public override void PhysicsUpdate()
    {
        // 攻擊狀態的物理更新邏輯
        // 例如：攻擊範圍檢測、傷害計算等
    }
}
