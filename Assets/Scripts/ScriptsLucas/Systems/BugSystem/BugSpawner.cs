using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BugSpawner : MonoBehaviour
{
    public static BugSpawner Instance { get; private set; }

    [Serializable]
    public class Lane
    {
        public Fighter fighter;
        public Transform spawnPoint;    // de donde sale el bicho
        [Tooltip("Puntos de salida extra. Se elige uno al azar entre estos y Spawn Point")]
        public Transform[] extraSpawnPoints;

        [Tooltip("Donde flota el bicho cerca de la planta")]
        public Transform attackPoint;
        [Tooltip("Puntos de flote extra. Cada bicho ocupa el mas libre (Attack Point incluido)")]
        public Transform[] extraAttackPoints;

        [Tooltip("Donde pica para golpear a la planta. Vacio = el AimPoint o posicion de la planta")]
        public Transform strikePoint;

        [NonSerialized] public float nextSpawn;
        [NonSerialized] public int alive;
        [NonSerialized] public int[] slotUse;   // cuantos bichos hay en cada punto de flote
    }

    [SerializeField] List<Bug> bugPrefabs;
    [Tooltip("Opcional: para dar puntos al matar bichos y para el sesgo hacia el que va ganando")]
    [SerializeField] ScoreSystem score;
    [SerializeField] Lane[] lanes;

    [Header("Frecuencia")]
    [SerializeField, Min(0f)] float firstSpawnDelay = 5f;
    [SerializeField, Min(0.5f)] float minInterval = 6f;
    [SerializeField, Min(0.5f)] float maxInterval = 12f;
    [SerializeField, Min(1)] int maxAlivePerLane = 2;

    [Header("Separacion")]
    [Tooltip("Si varios bichos comparten punto de flote, se separan hasta esta distancia")]
    [SerializeField, Min(0f)] float crowdOffset = 0.5f;

    [Header("Comeback")]
    [Tooltip("Multiplica el intervalo del que va ganando por hasta este valor (<1 = mas bichos para el lider). 1 = sin sesgo")]
    [SerializeField, Range(0.3f, 1f)] float leaderIntervalFactor = 0.7f;

    private int idx = 0;

    private readonly List<Bug> spawned = new List<Bug>();

    public bool Running { get; set; } = true;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        foreach (var lane in lanes)
            lane.nextSpawn = Time.time + firstSpawnDelay + UnityEngine.Random.Range(0f, minInterval);
    }

    void Update()
    {
        if (!Running || bugPrefabs == null || bugPrefabs.Count == 0) return;

        foreach (var lane in lanes)
        {
            if (!LaneReady(lane)) continue;
            if (Time.time < lane.nextSpawn) continue;

            if (lane.alive < maxAlivePerLane) Spawn(lane);
            ScheduleNext(lane);
        }
    }

    bool LaneReady(Lane lane)
    {
        return lane.fighter != null && lane.spawnPoint != null && lane.attackPoint != null;
    }

    #region Puntos
    Transform PickSpawnPoint(Lane lane)
    {
        int extra = lane.extraSpawnPoints != null ? lane.extraSpawnPoints.Length : 0;
        int pick = UnityEngine.Random.Range(0, extra + 1);
        if (pick == 0) return lane.spawnPoint;

        Transform t = lane.extraSpawnPoints[pick - 1];
        return t != null ? t : lane.spawnPoint;
    }

    int HoverSlotCount(Lane lane)
    {
        return 1 + (lane.extraAttackPoints != null ? lane.extraAttackPoints.Length : 0);
    }

    Transform HoverPoint(Lane lane, int slot)
    {
        if (slot == 0) return lane.attackPoint;
        Transform t = lane.extraAttackPoints[slot - 1];
        return t != null ? t : lane.attackPoint;
    }

    int TakeHoverSlot(Lane lane)
    {
        int count = HoverSlotCount(lane);
        if (lane.slotUse == null || lane.slotUse.Length != count) lane.slotUse = new int[count];

        // El punto con menos bichos; si empatan, uno al azar
        int best = -1;
        int bestUse = int.MaxValue;
        int ties = 0;
        for (int i = 0; i < count; i++)
        {
            int use = lane.slotUse[i];
            if (use < bestUse)
            {
                best = i;
                bestUse = use;
                ties = 1;
            }
            else if (use == bestUse && UnityEngine.Random.Range(0, ++ties) == 0)
            {
                best = i;
            }
        }

        lane.slotUse[best]++;
        return best;
    }

    void ReleaseHoverSlot(Lane lane, int slot)
    {
        if (lane.slotUse != null && slot >= 0 && slot < lane.slotUse.Length && lane.slotUse[slot] > 0)
            lane.slotUse[slot]--;
    }
    #endregion

    void Spawn(Lane lane)
    {
        if (bugPrefabs == null || bugPrefabs.Count == 0) return;

        // El lado de los bichos es el contrario al que mira al rival
        AttackSide side = lane.fighter.RivalSide == AttackSide.Left ? AttackSide.Right : AttackSide.Left;

        Bug prefab = bugPrefabs[idx % bugPrefabs.Count];
        idx++;
        if (prefab == null) return;

        Transform spawn = PickSpawnPoint(lane);
        int slot = TakeHoverSlot(lane);
        Vector3 hover = HoverPoint(lane, slot).position;

        // Si el punto ya tenia bichos, separar un poco a este
        if (lane.slotUse[slot] > 1 && crowdOffset > 0f)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * crowdOffset;
            hover += new Vector3(offset.x, offset.y, 0f);
        }

        Bug bug = Instantiate(prefab, spawn.position, spawn.rotation);
        Vector3 fallbackStrike = lane.fighter.AimPoint != null
            ? lane.fighter.AimPoint.position
            : lane.fighter.transform.position;
        Vector3 strike = lane.strikePoint != null ? lane.strikePoint.position : fallbackStrike;

        bug.Init(lane.fighter, side, hover, strike);

        spawned.Add(bug);

        lane.alive++;
        bug.Died += (b, killer) =>
        {
            spawned.Remove(b);
            lane.alive--;
            ReleaseHoverSlot(lane, slot);
            if (score != null && killer is Fighter f) score.AddPoints(f, b.killPoints);
        };
    }

    public int AliveTotal
    {
        get
        {
            int n = 0;
            foreach (var lane in lanes) n += lane.alive;
            return n;
        }
    }

    public void SummonBugs(Fighter plant, int count, float interval)
    {
        if (bugPrefabs == null || bugPrefabs.Count == 0 || count <= 0) return;

        foreach (var lane in lanes)
            if (lane.fighter == plant)
            {
                StartCoroutine(SummonRoutine(lane, count, interval));
                return;
            }
    }

    public void ClearAll()
    {
        StopAllCoroutines();
        foreach (Bug b in spawned)
            if (b != null) Destroy(b.gameObject);
        spawned.Clear();
        foreach (var lane in lanes)
        {
            lane.alive = 0;
            if (lane.slotUse != null) Array.Clear(lane.slotUse, 0, lane.slotUse.Length);
        }
    }

    public void RestartSchedule()
    {
        foreach (var lane in lanes)
            lane.nextSpawn = Time.time + firstSpawnDelay + UnityEngine.Random.Range(0f, minInterval);
    }

    IEnumerator SummonRoutine(Lane lane, int count, float interval)
    {
        for (int i = 0; i < count; i++)
        {
            if (!LaneReady(lane)) yield break;

            Spawn(lane);
            if (i < count - 1) yield return new WaitForSeconds(interval);
        }
    }

    void ScheduleNext(Lane lane)
    {
        float interval = UnityEngine.Random.Range(minInterval, Mathf.Max(minInterval, maxInterval));

        if (score != null)
            interval *= Mathf.Lerp(1f, leaderIntervalFactor, score.GetLead(lane.fighter));

        lane.nextSpawn = Time.time + interval;
    }
}