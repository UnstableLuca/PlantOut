using UnityEngine;

public class EnergyBar : MonoBehaviour
{
    public CharacterUI characterData;
    public bool isLeftSide;

    void Awake()
    {
        if(isLeftSide)
        {
            characterData.leftEnergyBarPrefab = gameObject;
        }
        else
        {
            characterData.rightEnergyBarPrefab = gameObject;
        }
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
