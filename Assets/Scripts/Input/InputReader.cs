// Scripts/Input/InputReader.cs
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public enum GameInputState
{
    Gameplay, // Estado normal, jogador controla o personagem
    UI,       // Estado de UI, jogador está interagindo com um menu, chat, etc.
}

[CreateAssetMenu(fileName = "InputReader", menuName = "Game/Input Reader")]
public class InputReader : ScriptableObject, PlayerControls.IPlayerActions, PlayerControls.IUIActions, PlayerControls.ICameraActions
{
    // --- EVENTOS ---
    public event Action<Vector2> MoveEvent = delegate { };
    public event Action JumpEvent = delegate { };
    public event Action<int> ActionBarEvent = delegate { };
    public event Action TabTargetEvent = delegate { };
    public event Action TargetClickEvent = delegate { };
    public event Action ClearTargetEvent = delegate { };
    public event Action InventoryEvent = delegate { };
    public event Action CharacterPanelEvent = delegate { };
    public event Action SpellBookEvent = delegate { };
    public event Action QuestLogEvent = delegate { };
    public event Action GameMenuEvent = delegate { };
    public event Action<Vector2> LookEvent = delegate { };
    public event Action<float> ZoomEvent = delegate { };
    public event Action<bool> RotateCameraEvent = delegate { };
    public event Action<bool> RotatePlayerAndCameraEvent = delegate { };
    public event Action InteractEvent = delegate { };

    public event Action ToggleNoclipEvent = delegate { };
    public event Action<bool> GmFlyUpEvent = delegate { };
    public event Action<bool> GmFlyDownEvent = delegate { };

    private PlayerControls _playerControls;

    private void OnEnable()
    {
        if (_playerControls == null)
        {
            _playerControls = new PlayerControls();
            _playerControls.Player.SetCallbacks(this);
            _playerControls.UI.SetCallbacks(this);
            _playerControls.Camera.SetCallbacks(this);
        }

        // Ativa os mapas que devem estar sempre ligados no modo de jogo normal
        _playerControls.Player.Enable();
        _playerControls.UI.Enable();
        _playerControls.Camera.Enable();
        SetInputState(GameInputState.Gameplay);
    }

    public void SetInputState(GameInputState newState)
    {
        switch (newState)
        {
            case GameInputState.Gameplay:
                _playerControls.Player.Enable();
                _playerControls.Camera.Enable();
                break;
            case GameInputState.UI:
                _playerControls.Player.Disable();
                _playerControls.Camera.Disable();
                break;
        }
    }

    private void OnDisable()
    {
        _playerControls.Player.Disable();
        _playerControls.UI.Disable();
        _playerControls.Camera.Disable();
    }


    // E o método de callback
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            InteractEvent.Invoke();
        }
    }

    // --- Implementação da Interface IPlayerActions ---
    public void OnMove(InputAction.CallbackContext context) => MoveEvent.Invoke(context.ReadValue<Vector2>());
    public void OnJump(InputAction.CallbackContext context) { if (context.performed) JumpEvent.Invoke(); }

    public void OnToggleNoclip(InputAction.CallbackContext context) { if (context.performed) ToggleNoclipEvent.Invoke(); }
    public void OnGmFlyUp(InputAction.CallbackContext context) => GmFlyUpEvent.Invoke(context.ReadValueAsButton());
    public void OnGmFlyDown(InputAction.CallbackContext context) => GmFlyDownEvent.Invoke(context.ReadValueAsButton());

    public void OnActionBar1(InputAction.CallbackContext context) { if (context.performed) ActionBarEvent.Invoke(0); }
    public void OnActionBar2(InputAction.CallbackContext context) { if (context.performed) ActionBarEvent.Invoke(1); }
    public void OnActionBar3(InputAction.CallbackContext context) { if (context.performed) ActionBarEvent.Invoke(2); }
    public void OnActionBar4(InputAction.CallbackContext context) { if (context.performed) ActionBarEvent.Invoke(3); }
    public void OnActionBar5(InputAction.CallbackContext context) { if (context.performed) ActionBarEvent.Invoke(4); }
    public void OnActionBar6(InputAction.CallbackContext context) { if (context.performed) ActionBarEvent.Invoke(5); }
    public void OnActionBar7(InputAction.CallbackContext context) { if (context.performed) ActionBarEvent.Invoke(6); }
    public void OnActionBar8(InputAction.CallbackContext context) { if (context.performed) ActionBarEvent.Invoke(7); }
    public void OnActionBar9(InputAction.CallbackContext context) { if (context.performed) ActionBarEvent.Invoke(8); }
    public void OnActionBar10(InputAction.CallbackContext context) { if (context.performed) ActionBarEvent.Invoke(9); }
    public void OnActionBar11(InputAction.CallbackContext context) { if (context.performed) ActionBarEvent.Invoke(10); }
    public void OnActionBar12(InputAction.CallbackContext context) { if (context.performed) ActionBarEvent.Invoke(11); }

    /// <summary>
    /// Procura uma ação da barra de ações pelo seu índice e retorna a string de exibição do seu atalho.
    /// </summary>
    /// <param name="slotIndex">O índice do slot (0-11).</param>
    /// <returns>A string do atalho (ex: "1", "ALPHA2", "F1") ou "?" se não for encontrada.</returns>
    public string GetBindingDisplayString(int slotIndex)
    {
        if (_playerControls == null) return "?";

        // O nome da ação no seu Input Asset é "ActionBar" + número (1-12)
        string actionName = $"ActionBar{slotIndex + 1}";

        // CORREÇÃO: Chama o FindAction no objeto _playerControls, não em _playerControls.Player
        InputAction action = _playerControls.FindAction(actionName);

        if (action != null)
        {
            // Pega o primeiro atalho associado a esta ação.
            // O restante do código funciona da mesma forma.
            int bindingIndex = action.GetBindingIndexForControl(action.controls[0]);

            // Adiciona uma verificação para o caso de não haver controles (ex: ação não mapeada)
            if (action.controls.Count > 0)
            {
                bindingIndex = action.GetBindingIndexForControl(action.controls[0]);
                if (bindingIndex >= 0)
                {
                    // ToDisplayString() converte o atalho para um texto legível
                    // ToUpper() é para deixar esteticamente mais bonito (ex: "f1" vira "F1")
                    return action.bindings[bindingIndex].ToDisplayString().ToUpper();
                }
            }
        }

        return "?"; // Retorna "?" se algo der errado ou a ação não for encontrada
    }

    public void OnTabTarget(InputAction.CallbackContext context) { if (context.performed) TabTargetEvent.Invoke(); }
    public void OnTargetClick(InputAction.CallbackContext context) { if (context.performed) TargetClickEvent.Invoke(); }
    public void OnClearTarget(InputAction.CallbackContext context) { if (context.performed) ClearTargetEvent.Invoke(); }
    public void OnRotateCamera(InputAction.CallbackContext context) => RotateCameraEvent.Invoke(context.ReadValueAsButton());
    public void OnRotatePlayerAndCamera(InputAction.CallbackContext context) => RotatePlayerAndCameraEvent.Invoke(context.ReadValueAsButton());

    // --- Implementação da Interface IUIActions ---
    public void OnInventory(InputAction.CallbackContext context) { if (context.performed) InventoryEvent.Invoke(); }
    public void OnCharacterPanel(InputAction.CallbackContext context) { if (context.performed) CharacterPanelEvent.Invoke(); }
    public void OnSpellBook(InputAction.CallbackContext context) { if (context.performed) SpellBookEvent.Invoke(); }
    public void OnQuestLog(InputAction.CallbackContext context) { if (context.performed) QuestLogEvent.Invoke(); }
    public void OnGameMenu(InputAction.CallbackContext context) { if (context.performed) GameMenuEvent.Invoke(); }

    // Métodos de navegação padrão da UI (deixe-os vazios)
    public void OnNavigate(InputAction.CallbackContext context) { }
    public void OnSubmit(InputAction.CallbackContext context) { }
    public void OnCancel(InputAction.CallbackContext context) { }
    public void OnPoint(InputAction.CallbackContext context) { }
    public void OnClick(InputAction.CallbackContext context) { }
    public void OnRightClick(InputAction.CallbackContext context) { }
    public void OnMiddleClick(InputAction.CallbackContext context) { }
    public void OnScrollWheel(InputAction.CallbackContext context) { }
    public void OnTrackedDevicePosition(InputAction.CallbackContext context) { }
    public void OnTrackedDeviceOrientation(InputAction.CallbackContext context) { }

    // --- Implementação da Interface ICameraActions ---
    public void OnLook(InputAction.CallbackContext context) => LookEvent.Invoke(context.ReadValue<Vector2>());
    public void OnZoom(InputAction.CallbackContext context) { if (context.performed) ZoomEvent.Invoke(context.ReadValue<float>()); }
}