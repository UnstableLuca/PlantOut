using System;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Tooltip("Segundos que tarda en 'generarse' (crece de 0 a su tamano) antes de salir disparado")]
    [SerializeField, Min(0f)] float growTime = 0.12f;
    [Tooltip("Distancia al objetivo a la que se considera que impacta")]
    [SerializeField, Min(0.01f)] float hitDistance = 0.25f;
    [Tooltip("Por seguridad: se destruye si no llega en este tiempo")]
    [SerializeField, Min(0.1f)] float maxLifetime = 3f;
    [SerializeField] bool rotateToDirection = true;

    Transform target;
    float speed;
    Action onArrive;
    float age;
    Vector3 fullScale;

    public void Launch(Transform targetPoint, float flightSpeed, Action arrive)
    {
        target = targetPoint;
        speed = flightSpeed;
        onArrive = arrive;

        fullScale = transform.localScale;
        transform.localScale = growTime > 0f ? Vector3.zero : fullScale;
    }

    void Update()
    {
        age += Time.deltaTime;

        if (target == null || age > maxLifetime)   // el objetivo desaparecio
        {
            Destroy(gameObject);
            return;
        }

        // Fase 1: se genera pegado al brazo
        if (age < growTime)
        {
            transform.localScale = fullScale * (age / growTime);
            return;
        }
        transform.localScale = fullScale;

        // Fase 2: vuela hacia el objetivo
        Vector3 toTarget = target.position - transform.position;
        float step = speed * Time.deltaTime;

        if (rotateToDirection && toTarget.sqrMagnitude > 0.0001f)
            transform.right = toTarget;

        if (toTarget.magnitude <= hitDistance + step)
        {
            Action arrive = onArrive;
            onArrive = null;
            arrive?.Invoke();
            Destroy(gameObject);
            return;
        }

        transform.position += toTarget.normalized * step;
    }
}