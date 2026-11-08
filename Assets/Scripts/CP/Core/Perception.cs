using System;

namespace AI.Core
{
    public class Perception
    {
        public string Name { get; }
        private readonly Func<bool> _check;

        public Perception(string name, Func<bool> check)
        {
            Name = name;
            _check = check ?? throw new ArgumentNullException(nameof(check));
        }

        public bool Check() => _check();

        //COMBINADORES PARA CONDICIONES COMPUESTAS, QUE NO QUIERO HACER CLASES NUEVAS
        public Perception And(Perception other) =>
            new Perception($"({Name}) && {other.Name})", () => Check() && other.Check());

        public Perception Or(Perception other) =>
            new Perception($"({Name}) || {other.Name})", () => Check() || other.Check());

        public Perception Not() =>
            new Perception($"!{Name}", () => !Check());

        public override string ToString() => Name;
    }
}
