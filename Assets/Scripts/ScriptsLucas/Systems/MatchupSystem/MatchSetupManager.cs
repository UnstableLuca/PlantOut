using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchSetupManager : MonoBehaviour
{
    [Header("Contenedores de Escena")]
    public Transform playerLeft;
    public Transform playerRight;

    [Tooltip("Para test rápido sin seleccionar personajes anteriormente")]
    [Header("Prefabs por defecto")]
    public GameObject defaultLeftPrefab;
    public GameObject defaultRightPrefab;


    public Transform leftBars;
    public Transform rightBars;


    public TMP_Text leftNameText;
    public TMP_Text rightNameText;
    public Image leftNameBox;
    public Image rightNameBox;

    public Image climbingImageLeft;
    public Image climbingImageRight;

    public CharacterUI LeftCharacter { get; private set; }
    public CharacterUI RightCharacter { get; private set; }

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

        LeftCharacter = SetupUI(leftBars, prefabL, fighterL, leftNameText, leftNameBox, climbingImageLeft);
        RightCharacter = SetupUI(rightBars, prefabR, fighterR, rightNameText, rightNameBox, climbingImageRight);
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

    private CharacterUI SetupUI(Transform barsRoot, GameObject characterPrefab, Fighter fighter,
                                TMP_Text nameText, Image nameBox, Image climbingUI)
    {
        if (barsRoot == null || characterPrefab == null) return null;

        CharacterUI match = null;

        foreach (SkillBarUI bar in barsRoot.GetComponentsInChildren<SkillBarUI>(true))
        {
            bool isMine = match == null
                          && bar.Character != null
                          && bar.Character.characterPrefab == characterPrefab;

            bar.gameObject.SetActive(isMine);

            if (isMine)
            {
                bar.Bind(fighter);
                match = bar.Character;
            }
        }

        if (match == null)
        {
            Debug.LogWarning($"[MatchSetupManager] Ninguna barra en '{barsRoot.name}' tiene un CharacterUI " +
                             $"cuyo Character Prefab sea '{characterPrefab.name}'.");
            return null;
        }

        if (nameText != null) nameText.text = match.plantName;
        if (nameBox != null) nameBox.color = match.backgroundColor;
        if (climbingUI != null) climbingUI.sprite = match.image;

        return match;
    }


}
