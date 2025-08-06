using UnityEngine;
using System;
using System.Collections.Generic;

// 假設這些介面已經存在
// public interface IDamagable { void TakeDamage(int damage); void Heal(int amount); }
// public interface IControllable { /* ... 介面方法 ... */ }

public class Creature : MonoBehaviour, IDamagable
{
    [Header("生命值設定")]
    [SerializeField] private int maxHealth = 100;
    protected int currentHealth; // 改為 protected，讓子類可存取
    protected bool isDead = false; // 改為 protected

    protected StateMachine stateMachine;
    protected CreatureState IdleState;
    protected CreatureState MoveState;
    protected CreatureState AttackState;
    protected CreatureState DeathState;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    // 生命值變化事件
    public event Action<int> OnHealthChanged;
    public event Action OnDeath;

    protected virtual void Awake() // 改為 protected virtual 讓子類可覆寫
    {
        currentHealth = maxHealth;
        stateMachine = new StateMachine();
        
        // 狀態初始化 (在子類中可能會添加更多狀態，或者在 Creature 內部初始化基礎狀態)
        // 為了通用性，將其放在 Awake，但在更複雜的系統中，子類可以定義自己的狀態。
        IdleState = new IdleState(this, stateMachine);
        MoveState = new MoveState(this, stateMachine);
        AttackState = new AttackState(this, stateMachine);
        DeathState = new DeathState(this, stateMachine);
        
        GetRequiredComponents();
    }

    protected virtual void Start() // 改為 protected virtual 讓子類可覆寫
    {
        stateMachine.Initialize(IdleState);
    }

    protected virtual void Update() // 改為 protected virtual 讓子類可覆寫
    {
        stateMachine.CurrentCreatureState?.FrameUpdate(); // 使用 ?. 避免空引用
    }

    protected virtual void FixedUpdate() // 改為 protected virtual 讓子類可覆寫
    {
        stateMachine.CurrentCreatureState?.PhysicsUpdate(); // 使用 ?. 避免空引用
    }

    /// <summary>
    /// 改變生物的狀態
    /// </summary>
    /// <param name="newState">要切換到的新狀態</param>
    public void ChangeState(CreatureState newState)
    {
        stateMachine.ChangeState(newState);
    }

    /// <summary>
    /// 生物受到傷害
    /// </summary>
    /// <param name="damage">傷害量</param>
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        Debug.Log($"{name} 受到 {damage} 點傷害，當前生命值: {currentHealth}");
    }

    /// <summary>
    /// 生物恢復生命
    /// </summary>
    /// <param name="amount">恢復量</param>
    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth);
        Debug.Log($"{name} 恢復 {amount} 點生命，當前生命值: {currentHealth}");
    }

    /// <summary>
    /// 生物死亡
    /// </summary>
    public void Die()
    {
        if (isDead) return;

        isDead = true;
        OnDeath?.Invoke();

        // 切換到死亡狀態
        ChangeState(DeathState);
        Debug.Log($"{name} 死亡了。");

        // 這裡可以添加死亡時的具體行為，例如：
        // - 播放死亡動畫
        // - 掉落物品 (如果非可控生物需要)
        // - 銷毀物件 (或禁用，取決於遊戲設計)
    }

    /// <summary>
    /// 重置生命值（用於復活等情況）
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        OnHealthChanged?.Invoke(currentHealth);

        // 復活時切換回閒置狀態
        ChangeState(IdleState);
        Debug.Log($"{name} 生命值已重置，復活。");
    }

    /// <summary>
    /// 設置最大生命值
    /// </summary>
    /// <param name="newMaxHealth">新的最大生命值</param>
    public void SetMaxHealth(int newMaxHealth)
    {
        if (newMaxHealth <= 0)
        {
            Debug.LogWarning($"嘗試將 {name} 的最大生命值設置為非正數 ({newMaxHealth})，操作被忽略。");
            return;
        }

        float healthPercentage = (float)currentHealth / maxHealth;
        maxHealth = newMaxHealth;
        currentHealth = Mathf.RoundToInt(maxHealth * healthPercentage);
        OnHealthChanged?.Invoke(currentHealth);
        Debug.Log($"{name} 最大生命值變更為 {maxHealth}，當前生命值為 {currentHealth}");
    }

    /// <summary>
    /// 獲取生物的Transform組件
    /// </summary>
    /// <returns>Transform組件</returns>
    public Transform GetTransform()
    {
        return transform;
    }

    /// <summary>
    /// 獲取必要的組件，例如Rigidbody等
    /// </summary>
    protected virtual void GetRequiredComponents()
    {
        // 基礎 Creature 可能不需要 Rigidbody，或讓子類決定
        // 如果所有 Creature 都需要 Rigidbody，則可以在這裡獲取
    }

    // 可以在這裡添加一些通用的 Gizmos 繪製，例如顯示血量條的基礎位置等
    protected virtual void OnDrawGizmosSelected()
    {
        // 可選：顯示生物的邊界框或中心點
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}