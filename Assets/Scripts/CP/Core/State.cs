using System;

namespace AI.Core
{
    public abstract class State
    {
        public virtual string Name => GetType().Name;

        public virtual void Enter() { }

        public virtual void Update() { }

        public virtual void Exit() { }
    }

    public class ActionState : State
    {
        private readonly string _name;

        private readonly Action _enter, _update, _exit;

        public ActionState(string name, Action enter = null, Action update = null, Action exit = null)
        {
            _name = name;
            _enter = enter;
            _update = update;
            _exit = exit;
        }

        public override string Name => _name;

        public override void Enter() => _enter?.Invoke();
        public override void Update() => _update?.Invoke();
        public override void Exit() => _exit?.Invoke();
    }


}
