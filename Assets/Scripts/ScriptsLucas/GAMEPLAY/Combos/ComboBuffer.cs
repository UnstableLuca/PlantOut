using UnityEngine;
using System;
using System.Collections.Generic;

public readonly struct BufferedAttack
{
    public readonly AttackSide Side;
    public readonly double Timestamp; //Time.timeAsDouble

    public BufferedAttack(AttackSide side, double timestamp)
    {
        Side = side;
        Timestamp = timestamp;
    }
}

public class ComboBuffer : MonoBehaviour
{
    const float DefaultDecay = 1.2f;

    [Header("Combos de esta planta")]
    [SerializeField] ComboSet comboSet;

    [Header("Reglas")]
    [Tooltip("Si un golpe 'limpio' borra la sequencia")]
    [SerializeField] bool clearOnCleanHit = true;

    Fighter fighter;

    readonly List<BufferedAttack> buffer = new List<BufferedAttack>();
    readonly List<ComboDefinition> sorted = new List<ComboDefinition>();

    int maxLen = 1;

    float Decay => comboSet != null ? comboSet.inputDecay : DefaultDecay;

    public IReadOnlyList<BufferedAttack> Buffer => buffer;

    public event Action BufferChanged;
    public event Action<ComboDefinition> ComboCast;

    private void Awake()
    {
        fighter = GetComponent<Fighter>();
        SetComboSet(comboSet);
    }

    void OnEnable() => fighter.HitReceived += OnHitRecived;
    void OnDisable() => fighter.HitReceived -= OnHitRecived;

    void Update() => Expire(Time.timeAsDouble);

    //CONFIG

    public void SetComboSet(ComboSet set)
    {
        comboSet = set;
        sorted.Clear();

        if (set != null && set.combos != null)
        {
            foreach (var c in set.combos)
            {
                if (c == null || c.sequence == null || c.sequence.Length == 0) continue;

                if (c.sequence.Length > fighter.MaxSkillUses)
                {
                    Debug.LogWarning(
                        $"El combo '{c.name}' tiene {c.sequence.Length} inputs pero la barra solo llega a " +
                        $"{fighter.MaxSkillUses}: no se podria lanzar.", set);
                    continue;
                }
                sorted.Add(c);
            }
        }

        sorted.Sort((a, b) => b.sequence.Length.CompareTo(a.sequence.Length));
        maxLen = sorted.Count > 0 ? sorted[0].sequence.Length : 1;

        while (buffer.Count > maxLen) buffer.RemoveAt(0);
    }

    //Input

    public bool PressAttack(AttackSide side)
    {
        Push(side);
        return fighter.RequestAttack(side);
    }

    public bool PressBlock() => fighter.RequestBlockPress();

    public void ReleaseBlock() => fighter.RequestBlockRelease();

    public bool PressSkill()
    {
        Expire(Time.timeAsDouble);

        foreach (var combo in sorted)
        {
            int len = combo.sequence.Length;
            if (len > fighter.SkillBar) continue;
            if (!EndsWith(combo)) continue;

            AttackSide side = combo.sequence[len - 1];

            bool started = combo.skill != null
                ? fighter.RequestSkill(combo.skill, side, len) 
                : fighter.RequestSkill(side, len);

            if (!started) return false;

            Clear();
            ComboCast.Invoke(combo);
            return true;
        }

        return false;
    }
    
    //Buffer

    void Push(AttackSide side)
    {
        double now = Time.timeAsDouble;
        Expire(now);

        buffer.Add(new BufferedAttack(side, now));

        if (buffer.Count > maxLen) buffer.RemoveAt(0);

        BufferChanged?.Invoke();
    }

    void Expire(double now)
    {
        int expired = 0;
        while (expired < buffer.Count && now - buffer[expired].Timestamp > Decay)
            expired++;

        if (expired == 0) return;

        buffer.RemoveRange(0, expired);
        BufferChanged?.Invoke();
    }

    public void Clear()
    {
        if (buffer.Count == 0) return;
        buffer.Clear();
        BufferChanged?.Invoke();
    }

    bool EndsWith(ComboDefinition combo)
    {
        int len = combo.sequence.Length;
        if (buffer.Count < len) return false;

        int start = buffer.Count - len;
        for (int i = 0; i < len; i++)
            if (buffer[start + i].Side != combo.sequence[i]) return false;

        return true;
    }

    void OnHitRecived(HitReport report)
    {
        if (clearOnCleanHit && report.Result == BlockResult.None) Clear();
    }
}
