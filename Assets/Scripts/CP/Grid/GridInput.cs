using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class GridInput : MonoBehaviour
{
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private LayerMask placementLayermask;

    public event Action OnClicked;
    public event Action OnExit;

    private Vector3 lastPosition;

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            OnClicked?.Invoke();
        }

        if ((Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
            (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame))
        {
            OnExit?.Invoke();
        }
    }

    public Vector3 GetSelectedPosition()
    {
        if (Mouse.current == null) return lastPosition;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mousePos = new Vector3(mouseScreenPos.x, mouseScreenPos.y, sceneCamera.nearClipPlane);

        Ray ray = sceneCamera.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementLayermask))
        {
            lastPosition = hit.point;
        }

        return lastPosition;
    }

    public bool IsPointerOverUI()
        => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
}