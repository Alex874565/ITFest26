using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [SerializeField] private PlayerInput input;

    public event Action OnPressStarted;
    public event Action OnPressReleased;

    private InputAction press;

    private void Awake()
    {
        press = input.actions["Press"];
    }

    private void OnEnable()
    {
        press.started += HandlePressStarted;
        press.canceled += HandlePressReleased;
        press.Enable();
    }

    private void OnDisable()
    {
        press.started -= HandlePressStarted;
        press.canceled -= HandlePressReleased;
        press.Disable();
    }

    private void HandlePressStarted(InputAction.CallbackContext ctx)
    {
        OnPressStarted?.Invoke();
    }

    private void HandlePressReleased(InputAction.CallbackContext ctx)
    {
        OnPressReleased?.Invoke();
    }
}