using UnityEngine;
using UnityEngine.InputSystem;

public enum KeyboardLayout { Player1_WASD, Player2_IJKL, Custom }
[RequireComponent(typeof(Fighter))]
public class FighterInput : MonoBehaviour
{
    [Header("Teclado")]
    [SerializeField] KeyboardLayout keyboardLayout = KeyboardLayout.Player1_WASD;

    [Tooltip("Solo con Layout = Custom. Formato del Input System, p.ej. <Keyboard>/j")]
    [SerializeField] string customMelee = "<Keyboard>/j";
    [SerializeField] string customRanged = "<Keyboard>/l";
    [SerializeField] string customBlock = "<Keyboard>/k";
    [SerializeField] string customSkill = "<Keyboard>/i";

    [Header("Mando")]
    [SerializeField] bool useGamepad = true;
    [Tooltip("-1 = cualquier mando. 0 = primer mando conectado, 1 = el segundo... Con dos jugadores usa 0 y 1.")]
    [SerializeField] int gamepadIndex = -1;

    Fighter fighter;
    InputAction melee, ranged, block, skill;

    void Awake()
    {
        fighter = GetComponent<Fighter>();

        GetKeys(out string m, out string r, out string b, out string s);

        // Mando: X = melee, B = rango, A = bloquear (mantener), Y = habilidad (mantener)
        melee = Make("Melee", m, "<Gamepad>/buttonWest");
        ranged = Make("Ranged", r, "<Gamepad>/buttonEast");
        block = Make("Block", b, "<Gamepad>/buttonSouth");
        skill = Make("Skill", s, "<Gamepad>/buttonNorth");
    }

    void OnEnable()
    {
        melee.performed += OnMelee;
        ranged.performed += OnRanged;
        block.performed += OnBlockPressed;
        block.canceled += OnBlockReleased;
        skill.performed += OnSkillPressed;
        skill.canceled += OnSkillReleased;

        melee.Enable();
        ranged.Enable();
        block.Enable();
        skill.Enable();
    }

    void OnDisable()
    {
        melee.Disable();
        ranged.Disable();
        block.Disable();
        skill.Disable();

        melee.performed -= OnMelee;
        ranged.performed -= OnRanged;
        block.performed -= OnBlockPressed;
        block.canceled -= OnBlockReleased;
        skill.performed -= OnSkillPressed;
        skill.canceled -= OnSkillReleased;
    }

    void OnDestroy()
    {
        melee?.Dispose();
        ranged?.Dispose();
        block?.Dispose();
        skill?.Dispose();
    }



    InputAction Make(string actionName, string keyboardBinding, string gamepadBinding)
    {
        var action = new InputAction(actionName, InputActionType.Button);
        if (!string.IsNullOrEmpty(keyboardBinding)) action.AddBinding(keyboardBinding);
        if (useGamepad) action.AddBinding(gamepadBinding);
        return action;
    }

    void GetKeys(out string meleeKey, out string rangedKey, out string blockKey, out string skillKey)
    {
        switch (keyboardLayout)
        {
            case KeyboardLayout.Player1_WASD:
                meleeKey = "<Keyboard>/d"; rangedKey = "<Keyboard>/a";
                blockKey = "<Keyboard>/s"; skillKey = "<Keyboard>/w";
                break;
            case KeyboardLayout.Player2_IJKL:
                meleeKey = "<Keyboard>/j"; rangedKey = "<Keyboard>/l";
                blockKey = "<Keyboard>/k"; skillKey = "<Keyboard>/i";
                break;
            default:
                meleeKey = customMelee; rangedKey = customRanged;
                blockKey = customBlock; skillKey = customSkill;
                break;
        }
    }
    bool Accept(InputAction.CallbackContext ctx)
    {
        if (ctx.control.device is Gamepad pad)
        {
            if (gamepadIndex < 0) return true;
            return gamepadIndex < Gamepad.all.Count && Gamepad.all[gamepadIndex] == pad;
        }
        return true;
    }

    void OnMelee(InputAction.CallbackContext ctx) { if (Accept(ctx)) fighter.RequestAttack(AttackKind.Melee); }
    void OnRanged(InputAction.CallbackContext ctx) { if (Accept(ctx)) fighter.RequestAttack(AttackKind.Ranged); }
    void OnBlockPressed(InputAction.CallbackContext ctx) { if (Accept(ctx)) fighter.RequestBlockPress(); }
    void OnBlockReleased(InputAction.CallbackContext ctx) { if (Accept(ctx)) fighter.RequestBlockRelease(); }
    void OnSkillPressed(InputAction.CallbackContext ctx) { if (Accept(ctx)) fighter.RequestSkillPress(); }
    void OnSkillReleased(InputAction.CallbackContext ctx) { if (Accept(ctx)) fighter.RequestSkillRelease(); }
}