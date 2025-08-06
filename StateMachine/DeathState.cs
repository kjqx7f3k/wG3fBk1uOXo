using UnityEngine;

public class DeathState : CreatureState
{
    public DeathState(Creature creature, StateMachine stateMachine) : base(creature, stateMachine)
    {
    }

    public override void EnterState()
    {
        Debug.Log($"{creature.name} entered Death state");
        // 死亡狀態進入時的邏輯
        // 例如：播放死亡動畫、停用碰撞器等
    }

    public override void ExitState()
    {
        Debug.Log($"{creature.name} exited Death state");
        // 離開死亡狀態時的邏輯（例如復活時）
    }

    public override void FrameUpdate()
    {
        // 死亡狀態的每幀更新邏輯
        // 通常死亡狀態不需要太多更新邏輯
        // 可能只需要處理復活條件或清理工作
    }

    public override void PhysicsUpdate()
    {
        // 死亡狀態的物理更新邏輯
        // 通常死亡狀態下不需要物理更新
    }
}
