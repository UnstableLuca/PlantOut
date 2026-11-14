using UnityEngine;

public class MatchSetupManager : MonoBehaviour
{
    [Header("Contenedores de Escena")]
    public Transform playerLeft;
    public Transform playerRight;

    [Tooltip("Para test rápido sin seleccionar personajes anteriormente")]
    [Header("Prefabs por defecto")]
    public GameObject defaultLeftPrefab;
    public GameObject defaultRightPrefab;

    private void Awake()
    {
        GameObject prefabL = GameSessionData.PlayerLeftPrefab != null ? GameSessionData.PlayerLeftPrefab : defaultLeftPrefab;
        GameObject prefabR = GameSessionData.PlayerRightPrefab != null ? GameSessionData.PlayerRightPrefab : defaultRightPrefab;

        Fighter fighterL = SpawnCharacter(playerLeft, prefabL, isLeft: true);
        Fighter fighterR = SpawnCharacter(playerRight, prefabR, isLeft: false);

        if (fighterL != null && fighterR != null)
        {
            fighterL.SetRival(fighterR);
            fighterR.SetRival(fighterL);
        }
    }

    private Fighter SpawnCharacter(Transform playerRoot, GameObject characterPrefab, bool isLeft)
    {
        if (playerRoot == null)
        {
            Debug.LogError("[MatchSetupManager] Falta asignar playerLeft o playerRight en el Inspector.");
            return null;
        }

        // Limpiar hijos previos si quedaron en edición
        for (int i = playerRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(playerRoot.GetChild(i).gameObject);
        }

        Fighter fighter = playerRoot.GetComponent<Fighter>();
        if (fighter != null)
        {
            fighter.isLeftPlayer = isLeft;
        }

        if (characterPrefab != null)
        {
            GameObject instance = Instantiate(characterPrefab, playerRoot);
            instance.name = characterPrefab.name;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogWarning($"[MatchSetupManager] No hay prefab asignado para {(isLeft ? "PlayerLeft" : "PlayerRight")}.");
        }

        return fighter;
    }
}
