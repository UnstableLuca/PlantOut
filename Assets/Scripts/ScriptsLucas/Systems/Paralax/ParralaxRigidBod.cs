using System;
using UnityEngine;

public class ParralaxRigidBod : MonoBehaviour
{
    [SerializeField] MatchTimer matchTimer;
    [SerializeField] float travelDistance = 20f;
    [SerializeField] AnimationCurve progressCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private float speed = 0.1f;

    private Rigidbody rb;
    private Vector3 startPosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        startPosition = rb.position;
    }

    void FixedUpdate()
    {
        Vector3 target = matchTimer != null
            ? startPosition + Vector3.down * travelDistance * progressCurve.Evaluate(matchTimer.Progress)
            : rb.position + Vector3.down * speed * Time.fixedDeltaTime;
        rb.MovePosition(target);
    }
}
