using System;
using UnityEngine;

[Serializable]
public class ComboDefinition
{
    public string name = "Combo";

    [Tooltip("Secuencia de ataques (izq/der), minimo 2. Su longitud = usos de barra que gasta.")]
    public AttackSide[] sequence = { AttackSide.Left, AttackSide.Left, AttackSide.Right };

    [Tooltip("Habilidad que se lanza. Va hacia el lado del ultimo input.")]
    public SkillAction skill;
}

[CreateAssetMenu(fileName = "NewComboSet", menuName = "Fighter/Combo Set")]
public class ComboSet : ScriptableObject
{
    [Tooltip("Segundos que vive un input en el buffer antes de caducar")]
    [Min(0.1f)] public float inputDecay = 1.2f;

    public ComboDefinition[] combos;
}
