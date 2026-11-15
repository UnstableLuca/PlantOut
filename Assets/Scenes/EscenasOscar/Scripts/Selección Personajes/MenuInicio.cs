using UnityEngine;

public class MenuInicio : MonoBehaviour
{
    [Header("Player")]
    public Transform player;

    [Header("Posicion Menu Partida Rapida")]
    public GameObject targetPartidaRapida;

    public void PartidaRapida()
    {
        
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
        }

        player.position = targetPartidaRapida.transform.position;

        if (cc != null)
        {
            cc.enabled = true;
        }
    }
}