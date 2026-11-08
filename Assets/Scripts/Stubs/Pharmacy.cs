using System.Collections.Generic;
using UnityEngine;

// STUB de ejemplo: si ya tienes tu clase Pharmacy, borra este archivo
// y asegúrate de que la tuya tenga "clients" y "emptyBeds".
public class Pharmacy : MonoBehaviour
{
    /// <summary>Cola de clientes esperando en el establecimiento.</summary>
    public Queue<Plant> clients = new Queue<Plant>();

    /// <summary>Lista de camas disponibles. Arrastra aquí las camillas en el inspector.</summary>
    public List<Bed> emptyBeds = new List<Bed>();

    public void Encolar(Plant planta) => clients.Enqueue(planta);
}
