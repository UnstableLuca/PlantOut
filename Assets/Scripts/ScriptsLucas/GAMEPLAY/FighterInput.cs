using UnityEngine;
using UnityEngine.InputSystem;

public enum KeyboardLayout { Player1_WASD, Player2_IJKL, Custom }
[RequireComponent(typeof(ComboBuffer))]
public class FighterInput : MonoBehaviour
{
    [Header("Teclado")]
    [SerializeField] KeyboardLayout keyboardLayout = KeyboardLayout.Player1_WASD;

    [Tooltip("Solo con Layout = Custom. Formato del Input System, p.ej. <Keyboard>/j")]
    [SerializeField] string customAttackLeft = "<Keyboard>/j";
    [SerializeField] string customAttackRight = "<Keyboard>/l";
    [SerializeField] string customBlock = "<Keyboard>/k";
    [SerializeField] string customSkill = "<Keyboard>/i";

    [Header("Mando")]
    [SerializeField] bool useGamepad = true;
    [Tooltip("-1 = cualquier mando. 0 = primer mando conectado, 1 = el segundo... Con dos jugadores usa 0 y 1.")]
    [SerializeField] int gamepadIndex = -1;

    ComboBuffer buffer;
    InputAction attackLeft, attackRight, block, skill;

    void Awake()
    {
        buffer = GetComponent<ComboBuffer>();

        GetKeys(out string l, out string r, out string b, out string s);

        // Mando: X = izquierda, B = derecha, A = bloquear (mantener), Y = habilidad
        attackLeft = Make("AttackLeft", l, "<Gamepad>/buttonWest");
        attackRight = Make("AttackRight", r, "<Gamepad>/buttonEast");
        block = Make("Block", b, "<Gamepad>/buttonSouth");
        skill = Make("Skill", s, "<Gamepad>/buttonNorth");
    }

    void OnEnable()
    {
        attackLeft.performed += OnAttackLeft;
        attackRight.performed += OnAttackRight;
        block.performed += OnBlockPressed;
        block.canceled += OnBlockReleased;
        skill.performed += OnSkill;

        attackLeft.Enable();
        attackRight.Enable();
        block.Enable();
        skill.Enable();
    }

    void OnDisable()
    {
        attackLeft.Disable();
        attackRight.Disable();
        block.Disable();
        skill.Disable();

        attackLeft.performed -= OnAttackLeft;
        attackRight.performed -= OnAttackRight;
        block.performed -= OnBlockPressed;
        block.canceled -= OnBlockReleased;
        skill.performed -= OnSkill;
    }

    void OnDestroy()
    {
        attackLeft?.Dispose();
        attackRight?.Dispose();
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

    void GetKeys(out string left, out string right, out string blockKey, out string skillKey)
    {
        switch (keyboardLayout)
        {
            case KeyboardLayout.Player1_WASD:
                left = "<Keyboard>/a"; right = "<Keyboard>/d";
                blockKey = "<Keyboard>/s"; skillKey = "<Keyboard>/w";
                break;
            case KeyboardLayout.Player2_IJKL:
                left = "<Keyboard>/j"; right = "<Keyboard>/l";
                blockKey = "<Keyboard>/k"; skillKey = "<Keyboard>/i";
                break;
            default:
                left = customAttackLeft; right = customAttackRight;
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

    void OnAttackLeft(InputAction.CallbackContext ctx) { if (Accept(ctx)) buffer.PressAttack(AttackSide.Left); }
    void OnAttackRight(InputAction.CallbackContext ctx) { if (Accept(ctx)) buffer.PressAttack(AttackSide.Right); }
    void OnBlockPressed(InputAction.CallbackContext ctx) { if (Accept(ctx)) buffer.PressBlock(); }
    void OnBlockReleased(InputAction.CallbackContext ctx) { if (Accept(ctx)) buffer.ReleaseBlock(); }
    void OnSkill(InputAction.CallbackContext ctx) { if (Accept(ctx)) buffer.PressSkill(); }
}
