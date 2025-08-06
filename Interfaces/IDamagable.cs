using UnityEngine;

public interface IDamagable
{
    int MaxHealth { get; }
    int CurrentHealth { get; }
    
    void TakeDamage(int damage);
    void Heal(int amount);
    void Die();
    bool IsDead { get; }
} 