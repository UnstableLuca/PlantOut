using UnityEngine;

// STUB de ejemplo: si ya tienes tu clase Plant, borra este archivo
// y añade a la tuya lo que use la enfermera (Curar, EstaCurada, AsignarCamilla, DarAlta).
public class Plant : MonoBehaviour
{
    public float salud = 20f;
    public float saludMax = 100f;

    public bool EstaCurada => salud >= saludMax;

    public void Curar(float cantidad)
    {
        salud = Mathf.Min(saludMax, salud + cantidad);
    }

    public void AsignarCamilla(Bed camilla)
    {
        // Versión simple: se teletransporta. Si la planta tiene su propia IA, aquí la mandarías andando.
        transform.position = camilla.PuntoPaciente;
    }

    public void DarAlta()
    {
        // Aquí: pagar, irse de la tienda, destruirse, etc.
        Debug.Log($"{name} ha sido curada.", this);
    }
}
