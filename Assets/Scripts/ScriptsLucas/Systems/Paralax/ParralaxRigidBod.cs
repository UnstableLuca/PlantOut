using System;
using UnityEngine;

public class ParralaxRigidBod : MonoBehaviour
{
    [SerializeField] private float speed = 0.1f;
    private Rigidbody rb;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.isKinematic = true;

        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 newPosition = rb.position + Vector3.down * speed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
    }
}
