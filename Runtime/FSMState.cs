using System;
using UnityEngine;

public abstract class FSMState<S, M>
                            where S : struct, IConvertible, IComparable
                            where M : FSMBehaviour<S, M> {

    protected M FSM { get; private set; }
    public abstract S State { get; }
    public virtual void OnInitialize() { }
    public virtual S OnEnter() { return State; }
    public virtual void OnLeave() { }
    public virtual S FixedUpdate() { return State; }
    public virtual S Update() { return State; }
    public virtual S LateUpdate() { return State; }
    public virtual void OnTriggerEvent<T>(T t) { }
    public virtual void OnTriggerEvent<T1, T2>(T1 t1, T2 t2) { }
    public virtual void OnTriggerEvent<T1, T2, T3>(T1 t1, T2 t2, T3 t3) { }

    public void Initialize(M machine) {
        Debug.AssertFormat(machine != null, "[{0}] Initialize : Machine must be not null.", typeof(S).ToString());
        FSM = machine;
        OnInitialize();
    }
}