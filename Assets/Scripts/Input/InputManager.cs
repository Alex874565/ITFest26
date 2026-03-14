using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [SerializeField] private PlayerInput input;

    public event Action OnPressStarted;
    public event Action OnPressReleased;
    public event EventHandler OnEscapeAction;

    private InputAction press;
    private InputAction escapeAction;

    private void Awake()
    {
        if (input == null)
            input = GetComponent<PlayerInput>();

        if (input == null)
        {
            return;
        }

        press = input.actions.FindAction("Press", true);
        escapeAction = input.actions.FindAction("Escape", true);
    }

    private void OnEnable()
    {
        if (input == null)
            return;

        input.ActivateInput();

        press.started += HandlePressStarted;
        press.canceled += HandlePressReleased;
        escapeAction.performed += HandleEscapePerformed;
    }

    private void OnDisable()
    {
        if (press != null)
        {
            press.started -= HandlePressStarted;
            press.canceled -= HandlePressReleased;
        }

        if (escapeAction != null)
        {
            escapeAction.performed -= HandleEscapePerformed;
        }
    }

    private void HandleEscapePerformed(InputAction.CallbackContext context)
    {
        OnEscapeAction?.Invoke(this, EventArgs.Empty);
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