using System;
using System.Collections.Generic;
using System.Data;
using UnityEngine.XR;

namespace AI.Core
{
    public class StateMachine
    {
        private class Transition
        {
            public readonly State To;

            public readonly Perception Condition;

            public Transition(State to, Perception condition)
            {
                To = to;
                Condition = condition;
            }
        }
        private static readonly List<Transition> NoTransition = new List<Transition>(0);

        private readonly Dictionary<State, List<Transition>> _transitions = new Dictionary<State, List<Transition>>();
        private readonly List<Transition> _anyTransitions = new List<Transition>();
        private List<Transition> _currentTransitions = NoTransition;

        public State Current { get; private set; }
        public State Previous { get; private set; }

        //estado anterior, estado nuevo, percepción que lo provocó o null si fue manual
        public event Action<State, State, Perception> StateChanged;

        //Transición desde un estado concreto
        public void AddTransition(State from, State to, Perception condition)
        {
            if(!_transitions.TryGetValue(from, out var list))
            {
                list = new List<Transition>();
                _transitions[from] = list;
            }

            list.Add(new Transition(to, condition));
        }

        public void AddAnyTransition(State to, Perception condition)
        {
            _anyTransitions.Add(new Transition(to, condition));
        }

        public void SetInitialState(State state) => ChangeState(state);

        public void Tick()
        {
            EvaluateTransitions();
            UpdateState();
        }

        public void EvaluateTransitions()
        {
            if (Current == null) return;

            foreach (var t in _anyTransitions)
            {
                if (t.To != Current && t.Condition.Check())
                {
                    ChangeState(t.To, t.Condition);
                    return;
                }
            }

            foreach (var t in _currentTransitions)
            {
                if (t.Condition.Check())
                {
                    ChangeState(t.To, t.Condition);
                    return;
                }
            }
        }

        public void UpdateState() => Current?.Update();

        public void ChangeState(State next, Perception reason = null)
        {
            if (next == null || next == Current) return;

            Current?.Exit();
            Previous = Current;
            Current = next;
            _currentTransitions = _transitions.TryGetValue(next, out var list) ? list : NoTransition;
            Current.Enter();

            StateChanged?.Invoke(Previous, Current, reason);
        }
    }
}
