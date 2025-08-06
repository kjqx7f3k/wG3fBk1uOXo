using UnityEngine;

public class StateMachine
{
    public CreatureState CurrentCreatureState{get; set;}

    public StateMachine()
    {}

    public void Initialize(CreatureState state)
    {
        this.CurrentCreatureState = state;
    }
    
    public void ChangeState(CreatureState newState)
    {
        CurrentCreatureState.ExitState();
        CurrentCreatureState = newState;
        CurrentCreatureState.EnterState();
    }
} 