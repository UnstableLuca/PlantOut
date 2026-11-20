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
        [Tooltip("Donde flota el bicho cerca de la planta")]
        public Transform attackPoint;
        [Tooltip("Donde pica para golpear a la planta. Vacio = el AimPoint o posicion de la planta")]
        public Transform strikePoint;

        [NonSerialized] public float nextSpawn;
        [NonSerialized] public int alive;
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
        if (!Running || bugPrefabs == null) return;

        foreach (var lane in lanes)
        {
            if (lane.fighter == null || lane.spawnPoint == null || lane.attackPoint == null) continue;
            if (Time.time < lane.nextSpawn) continue;

            if (lane.alive < maxAlivePerLane) Spawn(lane);
            ScheduleNext(lane);
        }
    }

    void Spawn(Lane lane)
    {
        // El lado de los bichos es el contrario al que mira al rival
        AttackSide side = lane.fighter.RivalSide == AttackSide.Left ? AttackSide.Right : AttackSide.Left;
        idx++;

        Bug bug = Instantiate(bugPrefabs[idx%2], lane.spawnPoint.position, lane.spawnPoint.rotation);
        Vector3 fallbackStrike = lane.fighter.AimPoint != null
            ? lane.fighter.AimPoint.position
            : lane.fighter.transform.position;
        Vector3 strike = lane.strikePoint != null ? lane.strikePoint.position : fallbackStrike;

        bug.Init(lane.fighter, side, lane.attackPoint.position, strike);

        spawned.Add(bug);

        lane.alive++;
        bug.Died += (b, killer) =>
        {
            spawned.Remove(b);
            lane.alive--;
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
        if (bugPrefabs == null || count <= 0) return;

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
        foreach (var lane in lanes) lane.alive = 0;
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
            if (lane.fighter == null || lane.spawnPoint == null || lane.attackPoint == null) yield break;

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