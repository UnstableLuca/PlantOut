using System;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.CullingGroup;

namespace AI.Core
{
    [DisallowMultipleComponent]
    public abstract class AIAgent : MonoBehaviour
    {
        [Header("IA - General")]
        [SerializeField, Tooltip("Segundos entre evaluaciones de transiciones. 0 = cada frame.")]
        private float thinkInterval = 0f;

        [SerializeField] private bool logStateChanges = true;

        [Header("IA - Movimiento")]
        [SerializeField, Tooltip("Opcional. Si no hay NavMeshAgent se mueve en línea recta.")]
        protected NavMeshAgent navAgent;

        [SerializeField] protected float moveSpeed = 3f;
        [SerializeField] protected float arriveDistance = 0.3f;

        protected StateMachine Fsm { get; private set; }
        public string CurrentStateName => Fsm?.Current?.Name ?? "-";

        private float _thinkTimer;
        private Vector3? _destination;

        protected virtual void Awake()
        {
            if (navAgent == null) navAgent = GetComponent<NavMeshAgent>();
        }

        protected virtual void Start()
        {
            Fsm = new StateMachine();
            Fsm.StateChanged += OnStateChanged;
            BuildStateMachine(Fsm);

            if (Fsm.Current == null)
                Debug.LogError($"[{name}] BuildStateMachine no llamó a SetInitialState.", this);
        }

        protected abstract void BuildStateMachine(StateMachine fsm);



        protected virtual void Update()
        {
            if (Fsm == null) return;

            _thinkTimer -= Time.deltaTime;
            if (_thinkTimer <= 0f)
            {
                _thinkTimer = thinkInterval;
                Fsm.EvaluateTransitions();
            }

            Fsm.UpdateState();
            UpdateManualMovement();
        }

        protected virtual void OnStateChanged(State from, State to, Perception reason)
        {
            if (!logStateChanges) return;
            string porQue = reason != null ? $" (por {reason.Name})" : "";
            Debug.Log($"[{name}] {(from != null ? from.Name : "Inicio")} → {to.Name}{porQue}", this);
        }

        // ---------------- Movimiento común ----------------

        public void MoveTo(Vector3 target)
        {
            _destination = target;
            if (UsingNavMesh) navAgent.SetDestination(target);
        }

        public void StopMoving()
        {
            _destination = null;
            if (UsingNavMesh) navAgent.ResetPath();
        }

        public bool HasArrived
        {
            get
            {
                if (_destination == null) return true;

                if (UsingNavMesh)
                    return !navAgent.pathPending &&
                           navAgent.remainingDistance <= Mathf.Max(arriveDistance, navAgent.stoppingDistance);

                return Vector3.Distance(transform.position, _destination.Value) <= arriveDistance;
            }
        }
        private bool UsingNavMesh => navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh;

        private void UpdateManualMovement()
        {
            if (_destination == null || UsingNavMesh) return;
            transform.position = Vector3.MoveTowards(transform.position, _destination.Value, moveSpeed * Time.deltaTime);
        }

#if UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, CurrentStateName);
        }
#endif
    }
}
