using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SplashScreenController : MonoBehaviour
{
    [SerializeField] private string mainMenuScene = "Menu";
    private bool isLoading = false;
    [SerializeField] private BlinkingText pressButtonText;

    private void Update()
    {
        if (isLoading && (Keyboard.current.anyKey.wasPressedThisFrame || Gamepad.current?.wasUpdatedThisFrame == true || Pointer.current?.wasUpdatedThisFrame == true))
        {
            LoadMainMenu();
        }
    }

    private void LoadMainMenu()
    {
        isLoading = true;

        pressButtonText.LoadMenu();

        SceneManager.LoadScene(mainMenuScene);
    }
}
