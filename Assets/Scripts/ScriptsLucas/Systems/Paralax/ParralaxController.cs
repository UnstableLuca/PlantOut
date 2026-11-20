using UnityEngine;

public class ParralaxController : MonoBehaviour
{
    [Header("Ligado a la partida (recomendado)")]
    [Tooltip("Si se asigna, la capa baja segun el tiempo y llega al final justo al acabar la partida")]
    [SerializeField] MatchTimer matchTimer;
    [Tooltip("Cuanto baja esta capa en toda la partida. Capas lejanas = menos distancia")]
    [SerializeField] float travelDistance = 20f;
    [Tooltip("Forma del avance. Lineal por defecto; puedes hacer que frene al llegar a la luz")]
    [SerializeField] AnimationCurve progressCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Sin MatchTimer")]
    [SerializeField] private float speed = 0.1f;

    private Vector3 startPosition;

    void Awake()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        if (matchTimer != null)
        {
            float t = progressCurve.Evaluate(matchTimer.Progress);
            transform.position = startPosition + Vector3.down * travelDistance * t;
        }
        else
        {
            transform.position += Vector3.down * speed * Time.deltaTime;
        }
    }
}
