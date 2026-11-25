using System.Collections.Generic;
using UnityEngine;

public class TestColaPacientes : MonoBehaviour
{
    [SerializeField] private Pharmacy pharmacy;
    [SerializeField] private List<Plant> plantas;

    public void Test()
    {
        foreach (var p in plantas) pharmacy.Encolar(p);
    }
}