using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

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

    private Fighter fighter;
    private InputActions inputActions;
    private InputAction melee, ranged, block, skill;

    void Awake()
    {
        fighter = GetComponent<Fighter>();
        inputActions = new InputActions();

        // Obtiene el Action Map "Combat" del asset InputActions
        InputActionMap combatMap = inputActions.asset.FindActionMap("Combat", throwIfNotFound: true);

        melee = combatMap.FindAction("Melee", throwIfNotFound: true);
        ranged = combatMap.FindAction("Ranged", throwIfNotFound: true);
        block = combatMap.FindAction("Block", throwIfNotFound: true);
        skill = combatMap.FindAction("Skill", throwIfNotFound: true);

        // Si se usa Layout Custom, añadimos sus bindings personalizados al asset en tiempo de ejecución
        if (keyboardLayout == KeyboardLayout.Custom)
        {
            if (!string.IsNullOrEmpty(customMelee)) melee.AddBinding(customMelee);
            if (!string.IsNullOrEmpty(customRanged)) ranged.AddBinding(customRanged);
            if (!string.IsNullOrEmpty(customBlock)) block.AddBinding(customBlock);
            if (!string.IsNullOrEmpty(customSkill)) skill.AddBinding(customSkill);
        }

        ConfigureDevices();
    }

    private void ConfigureDevices()
    {
        var devicesList = new System.Collections.Generic.List<InputDevice>();

        if (Keyboard.current != null)
            devicesList.Add(Keyboard.current);

        if (useGamepad)
        {
            var gamepads = Gamepad.all;
            if (gamepadIndex >= 0 && gamepadIndex < gamepads.Count)
            {
                devicesList.Add(gamepads[gamepadIndex]);
            }
            else if (gamepadIndex < 0)
            {
                for (int i = 0; i < gamepads.Count; i++)
                    devicesList.Add(gamepads[i]);
            }
        }

        if (devicesList.Count > 0)
        {
            inputActions.devices = new ReadOnlyArray<InputDevice>(devicesList.ToArray());
        }
    }

    void OnEnable()
    {
        melee.performed += OnMelee;
        ranged.performed += OnRanged;
        block.performed += OnBlockPressed;
        block.canceled += OnBlockReleased;
        skill.performed += OnSkillPressed;
        skill.canceled += OnSkillReleased;

        inputActions.asset.FindActionMap("Combat").Enable();
    }

    void OnDisable()
    {
        inputActions.asset.FindActionMap("Combat").Disable();

        melee.performed -= OnMelee;
        ranged.performed -= OnRanged;
        block.performed -= OnBlockPressed;
        block.canceled -= OnBlockReleased;
        skill.performed -= OnSkillPressed;
        skill.canceled -= OnSkillReleased;
    }

    void OnDestroy()
    {
        inputActions?.Dispose();
    }

    private bool Accept(InputAction.CallbackContext ctx)
    {
        if (ctx.control.device is Gamepad pad)
        {
            if (!useGamepad) return false;
            if (gamepadIndex < 0) return true;
            return gamepadIndex < Gamepad.all.Count && Gamepad.all[gamepadIndex] == pad;
        }

        if (ctx.control.device is Keyboard)
        {
            string path = "<Keyboard>/" + ctx.control.name.ToLower();

            switch (keyboardLayout)
            {
                case KeyboardLayout.Player1_WASD:
                    return path == "<Keyboard>/d" || path == "<Keyboard>/a" ||
                           path == "<Keyboard>/s" || path == "<Keyboard>/w";

                case KeyboardLayout.Player2_IJKL:
                    return path == "<Keyboard>/j" || path == "<Keyboard>/l" ||
                           path == "<Keyboard>/k" || path == "<Keyboard>/i";

                case KeyboardLayout.Custom:
                    return path == customMelee.ToLower() || path == customRanged.ToLower() ||
                           path == customBlock.ToLower() || path == customSkill.ToLower();
            }
        }

        return false;
    }

    void OnMelee(InputAction.CallbackContext ctx) { if (Accept(ctx)) fighter.RequestAttack(AttackKind.Melee); }
    void OnRanged(InputAction.CallbackContext ctx) { if (Accept(ctx)) fighter.RequestAttack(AttackKind.Ranged); }
    void OnBlockPressed(InputAction.CallbackContext ctx) { if (Accept(ctx)) fighter.RequestBlockPress(); }
    void OnBlockReleased(InputAction.CallbackContext ctx) { if (Accept(ctx)) fighter.RequestBlockRelease(); }
    void OnSkillPressed(InputAction.CallbackContext ctx) { if (Accept(ctx)) fighter.RequestSkillPress(); }
    void OnSkillReleased(InputAction.CallbackContext ctx) { if (Accept(ctx)) fighter.RequestSkillRelease(); }
}