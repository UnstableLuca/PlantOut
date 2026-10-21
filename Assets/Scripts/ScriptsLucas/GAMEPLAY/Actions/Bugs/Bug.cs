using UnityEngine;

public class Bug : MonoBehaviour, IDamageable
{
    public BlockResult ReceiveHit(in HitData hit)
    {
        throw new System.NotImplementedException();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
