using System;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public enum TipAxis { PlusX, MinusX, PlusY, MinusY, PlusZ, MinusZ }

    [Tooltip("Segundos que tarda en 'generarse' (crece de 0 a su tamano) antes de salir disparado")]
    [SerializeField, Min(0f)] float growTime = 0.12f;
    [Tooltip("Distancia al objetivo a la que se considera que impacta")]
    [SerializeField, Min(0.01f)] float hitDistance = 0.25f;
    [Tooltip("Por seguridad: se destruye si no llega en este tiempo")]
    [SerializeField, Min(0.1f)] float maxLifetime = 3f;

    [Header("Orientacion")]
    [SerializeField] bool rotateToDirection = true;
    [Tooltip("Eje del modelo hacia el que apunta la punta. Pruebalo en Play hasta que salga recto")]
    [SerializeField] TipAxis tipAxis = TipAxis.PlusX;
    [Tooltip("Giro extra en grados para ajustar a ojo (se aplica despues de apuntar)")]
    [SerializeField] Vector3 extraRotation = Vector3.zero;

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

        FaceTarget();
    }

    void Update()
    {
        age += Time.deltaTime;

        if (target == null || age > maxLifetime)   // el objetivo desaparecio
        {
            Destroy(gameObject);
            return;
        }

        FaceTarget();

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

    void FaceTarget()
    {
        if (!rotateToDirection || target == null) return;

        Vector3 dir = target.position - transform.position;
        if (dir.sqrMagnitude < 0.0001f) return;

        Vector3 up = Mathf.Abs(Vector3.Dot(dir.normalized, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
        transform.rotation = Quaternion.LookRotation(dir, up) * TipToForward() * Quaternion.Euler(extraRotation);
    }

    // Giro que lleva la punta del modelo al eje +Z (el que usa LookRotation)
    Quaternion TipToForward()
    {
        switch (tipAxis)
        {
            case TipAxis.PlusX: return Quaternion.Euler(0f, -90f, 0f);
            case TipAxis.MinusX: return Quaternion.Euler(0f, 90f, 0f);
            case TipAxis.PlusY: return Quaternion.Euler(90f, 0f, 0f);
            case TipAxis.MinusY: return Quaternion.Euler(-90f, 0f, 0f);
            case TipAxis.MinusZ: return Quaternion.Euler(0f, 180f, 0f);
            default: return Quaternion.identity;
        }
    }
}