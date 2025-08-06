using UnityEngine;

public interface IControllable
{
    // 基本移動控制
    void HandleMovementInput(Vector2 movementInput);
    void HandleJumpInput(bool jumpPressed);
    
    // 攻擊和互動
    void HandleAttackInput(bool attackPressed);
    void HandleInteractInput(bool interactPressed);

    // Just for fun
    void HandleNumberKeyInput(int keyIndex);

    // 控制狀態
    void OnControlGained();
    void OnControlLost();
    
    // 獲取生物資訊
    Transform GetTransform();
    bool IsControllable { get; }
}
