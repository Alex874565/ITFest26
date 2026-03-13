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
    private InputAction _escapeAction;

    private void Awake()
    {
        press = input.actions["Press"];
        _escapeAction = input.actions["Escape"];
    }

    private void Start()
    {
        OnEscapeAction += ServiceLocator.Instance.GameManager.InputManager_OnEscapeAction;
    }

    private void OnEnable()
    {
        press.started += HandlePressStarted;
        press.canceled += HandlePressReleased;
        _escapeAction.performed += Escape_performed;
        press.Enable();
    }

    private void OnDisable()
    {
        press.started -= HandlePressStarted;
        press.canceled -= HandlePressReleased;
        _escapeAction.performed -= Escape_performed;
        press.Disable();
    }

    private void OnDestroy()
    {
        OnEscapeAction -= ServiceLocator.Instance.GameManager.InputManager_OnEscapeAction;
    }

    private void Escape_performed(InputAction.CallbackContext context)
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