using UnityEngine;

// STUB de ejemplo: si ya tienes tu clase de camilla, borra este archivo.
public class Bed : MonoBehaviour
{
    [SerializeField, Tooltip("Dónde se coloca el paciente.")]
    private Transform puntoPaciente;

    [SerializeField, Tooltip("Dónde se coloca la enfermera para curar.")]
    private Transform puntoEnfermera;

    public Vector3 PuntoPaciente => puntoPaciente != null ? puntoPaciente.position : transform.position;
    public Vector3 PuntoEnfermera => puntoEnfermera != null ? puntoEnfermera.position : transform.position + transform.right;
}
