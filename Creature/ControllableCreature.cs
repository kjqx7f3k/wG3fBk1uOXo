using UnityEngine;
using System;
using System.Collections.Generic;

public class ControllableCreature : Creature, IControllable
{
    // 玩家控制相關屬性
    [Header("控制設定")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private bool isControllable = true;

    [Header("互動設定")]
    [SerializeField] private float interactionRadius = 3f;
    [SerializeField] private LayerMask interactableLayerMask = -1;

    [Header("地面檢測")]
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundLayerMask = 1; // Default layer
    [SerializeField] private Transform groundCheckPoint;

    [Header("背包設定")]
    [SerializeField] private int inventorySize = 20;
    [SerializeField] private List<Item> startingItems = new List<Item>();

    private Rigidbody rb;
    private bool isPlayerControlled = false;
    private bool isGrounded = false;

    // 背包系統
    private CreatureInventory creatureInventory;
    public CreatureInventory Inventory => creatureInventory;

    // IControllable 介面屬性
    public bool IsControllable => isControllable && !isDead && !IsInDialog() && !IsUIBlocking();


    private Animator animator;


    protected override void Awake()
    {
        base.Awake(); // 呼叫父類別的 Awake
        GetRequiredComponents(); // 獲取 ControllableCreature 獨有的組件
        animator = GetComponent<Animator>();
        InitializeInventory();
    }

    protected override void Update()
    {
        base.Update(); // 呼叫父類別的 Update

        // 如果是玩家控制的生物，執行額外檢查
        if (isPlayerControlled)
        {   
            // 計算 XZ 平面速度
            Vector3 xzVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            float currentSpeed = animator.GetFloat("Blend");
            float targetSpeed = xzVelocity.magnitude;
            float smoothedSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 9f);
            animator.SetFloat("Blend", smoothedSpeed);

            // // 將速度設置到 Animator 的參數（例如 "Speed"）
            // animator.SetFloat("Blend", speed);
            CheckGrounded();
        }
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate(); // 呼叫父類別的 FixedUpdate
    }

    /// <summary>
    /// IControllable 介面實作：處理移動輸入
    /// </summary>
    public void HandleMovementInput(Vector2 movementInput)
    {
        if (!IsControllable) return;

        // 將 2D 輸入轉換為 3D 移動
        Vector3 movement = new Vector3(movementInput.x, 0, movementInput.y);
        movement = movement.normalized * moveSpeed;

        // 如果有移動輸入，切換到移動狀態
        if (movement.magnitude > 0.1f)
        {
            if (stateMachine.CurrentCreatureState != MoveState)
            {
                ChangeState(MoveState);
            }

            // 應用移動
            if (rb != null)
            {
                // 對於 Rigidbody，直接設置 velocity 而不是 AddForce 來達到更精確的移動控制
                rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);
            }
            else
            {
                transform.Translate(movement * Time.deltaTime);
            }

            // 讓生物面向移動方向
            if (movement != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(movement, Vector3.up);
            }
        }
        else
        {
            // 沒有移動輸入時切換到閒置狀態
            // 只有在玩家控制時才強制切換到閒置，否則 AI 可能有其他邏輯
            if (stateMachine.CurrentCreatureState != IdleState && isPlayerControlled)
            {
                ChangeState(IdleState);
            }

            // 停止水平移動
            if (rb != null)
            {
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                
            }
        }
    }

    public void HandleNumberKeyInput(int keyIndex)
    {
        if (!IsControllable || keyIndex < 0 || keyIndex >= 10) return;

        // 處理數字鍵輸入（例如快捷鍵使用物品）
        Debug.Log($"{name} 使用了快捷鍵 {keyIndex}");
        switch(keyIndex)
        {
            case 0:
                animator.Play("Idle_In");
                animator.Play("Taunt");
                break;
            case 1:
                animator.Play("Idle_In");
                animator.Play("Laugh");
                break;
            case 2:
                animator.Play("Idle_In");
                animator.Play("Dance_Loop");
                break;
            case 3:
                break;
        }
    }

    /// <summary>
    /// IControllable 介面實作：處理跳躍輸入
    /// </summary>
    public void HandleJumpInput(bool jumpPressed)
    {
        if (!IsControllable || !jumpPressed) return;

        // 只有在地面上才能跳躍
        if (!isGrounded)
        {
            Debug.Log($"{name} 不在地面上，無法跳躍");
            return;
        }

        // 跳躍實作（需要 Rigidbody）
        if (rb != null)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            Debug.Log($"{name} 跳躍");
        }
        else
        {
            Debug.LogWarning($"{name} 嘗試跳躍但沒有 Rigidbody 組件。");
        }
    }

    /// <summary>
    /// IControllable 介面實作：處理攻擊輸入
    /// </summary>
    public void HandleAttackInput(bool attackPressed)
    {
        if (!IsControllable || !attackPressed) return;

        // 切換到攻擊狀態
        // 可以在這裡添加邏輯判斷是否允許攻擊，例如冷卻時間
        ChangeState(AttackState);
    }

    /// <summary>
    /// IControllable 介面實作：處理互動輸入
    /// </summary>
    public void HandleInteractInput(bool interactPressed)
    {
        if (!IsControllable || !interactPressed) return;

        // 檢查是否正在對話中
        if (IsInDialog())
        {
            Debug.Log($"{name} 正在對話中，無法發起新的對話");
            return;
        }

        // 按下E鍵時才偵測周圍的可互動物件
        InteractableObject nearestInteractable = FindNearestInteractable();

        if (nearestInteractable != null)
        {
            nearestInteractable.Interact();
            Debug.Log($"{name} 與 {nearestInteractable.name} 互動");
        }
        else
        {
            Debug.Log($"{name} 附近沒有可互動的物件");
        }
    }

    /// <summary>
    /// IControllable 介面實作：生物獲得玩家控制
    /// </summary>
    public void OnControlGained()
    {
        isPlayerControlled = true;
        Debug.Log($"{name} 獲得玩家控制");
    }

    /// <summary>
    /// IControllable 介面實作：生物失去玩家控制
    /// </summary>
    public void OnControlLost()
    {
        isPlayerControlled = false;
        Debug.Log($"{name} 失去玩家控制");

        // 失去控制時切換到閒置狀態，除非已經死亡
        if (stateMachine.CurrentCreatureState != DeathState)
        {
            ChangeState(IdleState);
        }

        // 停止移動
        if (rb != null)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    /// <summary>
    /// 在 Awake 中獲取 Rigidbody 組件
    /// </summary>
    protected override void GetRequiredComponents()
    {
        base.GetRequiredComponents(); // 呼叫父類的組件獲取
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning($"{name} (ControllableCreature) 沒有 Rigidbody 組件，移動和跳躍可能無法正常工作。");
        }
    }

    /// <summary>
    /// 尋找最近的可互動物件
    /// </summary>
    /// <returns>最近的可互動物件，如果沒有則返回null</returns>
    private InteractableObject FindNearestInteractable()
    {
        // 使用OverlapSphere找到範圍內的所有碰撞器
        Collider[] colliders = Physics.OverlapSphere(transform.position, interactionRadius, interactableLayerMask);

        InteractableObject closestInteractable = null;
        float closestDistance = float.MaxValue;

        // 找到最近的可互動物件
        foreach (Collider collider in colliders)
        {
            InteractableObject interactable = collider.GetComponent<InteractableObject>();
            if (interactable != null)
            {
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestInteractable = interactable;
                }
            }
        }
        return closestInteractable;
    }

    /// <summary>
    /// 檢查是否在地面上
    /// </summary>
    private void CheckGrounded()
    {
        // 確保有設定地面檢測點，否則嘗試使用 Collider 邊界
        Vector3 rayStart = groundCheckPoint != null ? groundCheckPoint.position : transform.position;
        Collider ownCollider = GetComponent<Collider>();

        if (groundCheckPoint == null && ownCollider != null)
        {
            // 如果沒有指定 groundCheckPoint，則從 Collider 的底部中心發射射線
            rayStart = new Vector3(transform.position.x, ownCollider.bounds.min.y + 0.05f, transform.position.z); // 稍微抬高防止射線在地面內部
        }
        else if (groundCheckPoint == null && ownCollider == null)
        {
            Debug.LogWarning($"{name} 沒有設定地面檢測點且沒有 Collider，無法可靠地檢測地面。");
        }

        isGrounded = Physics.Raycast(rayStart, Vector3.down, groundCheckDistance, groundLayerMask);

        // 可選：可以添加 Debug.DrawRay 來在 Scene 視圖中可視化射線
        // Debug.DrawRay(rayStart, Vector3.down * groundCheckDistance, isGrounded ? Color.green : Color.red);
    }

    /// <summary>
    /// 檢查是否正在對話中 (假設 DialogManager 存在且為單例)
    /// </summary>
    /// <returns>是否正在對話中</returns>
    private bool IsInDialog()
    {
        // 直接使用DialogManager的IsInDialog屬性
        return DialogManager.Instance != null && DialogManager.Instance.IsInDialog;
    }

    /// <summary>
    /// 檢查UI是否阻止遊戲輸入
    /// </summary>
    /// <returns>是否被UI阻止</returns>
    private bool IsUIBlocking()
    {
        // 檢查各個可能阻擋角色移動的UI
        if (InventoryManager.Instance != null && InventoryManager.Instance.IsOpen && InventoryManager.Instance.BlocksCharacterMovement)
            return true;
            
        if (GameMenuManager.Instance != null && GameMenuManager.Instance.IsOpen && GameMenuManager.Instance.BlocksCharacterMovement)
            return true;
            
        if (SaveUIController.Instance != null && SaveUIController.Instance.IsOpen && SaveUIController.Instance.BlocksCharacterMovement)
            return true;
            
        if (PlayerGameSettingsUI.Instance != null && PlayerGameSettingsUI.Instance.IsOpen && PlayerGameSettingsUI.Instance.BlocksCharacterMovement)
            return true;
        
        return false;
    }

    /// <summary>
    /// 初始化背包系統
    /// </summary>
    private void InitializeInventory()
    {
        creatureInventory = new CreatureInventory();
        creatureInventory.Initialize(inventorySize, name);

        // 監聽背包變化事件
        creatureInventory.OnInventoryChanged += OnInventoryChanged;
        
        // 添加起始物品
        AddStartingItems();
    }

    /// <summary>
    /// 添加起始物品
    /// </summary>
    private void AddStartingItems()
    {
        if (creatureInventory != null && startingItems != null)
        {
            creatureInventory.AddStartingItems(startingItems);
        }
    }

    /// <summary>
    /// 背包變化事件處理
    /// </summary>
    private void OnInventoryChanged()
    {
        // 如果這個生物是當前控制的生物，更新UI
        if (isPlayerControlled && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RefreshCurrentInventoryUI();
        }
    }

    /// <summary>
    /// 在Scene視圖中顯示互動範圍和地面檢測
    /// </summary>
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected(); // 呼叫父類的 Gizmos 繪製

        // 顯示互動範圍
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);

        // 顯示地面檢測
        Vector3 rayStart = groundCheckPoint != null ? groundCheckPoint.position : transform.position;
        Collider ownCollider = GetComponent<Collider>();
        if (groundCheckPoint == null && ownCollider != null)
        {
            rayStart = new Vector3(transform.position.x, ownCollider.bounds.min.y + 0.05f, transform.position.z);
        }

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(rayStart, Vector3.down * groundCheckDistance);

        // 如果正在對話，顯示對話狀態
        if (IsInDialog())
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRadius * 0.5f);
        }
    }
}
