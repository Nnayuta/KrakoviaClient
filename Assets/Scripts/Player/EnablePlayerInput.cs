using UnityEngine;
using UnityEngine.InputSystem;

// Coloque este script no mesmo GameObject que o seu componente PlayerInput.
[RequireComponent(typeof(PlayerInput))]
public class EnablePlayerInput : MonoBehaviour
{
    private PlayerInput playerInput;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        // Esta é a linha mágica. Ela garante que todas as ações
        // gerenciadas por este PlayerInput sejam "ligadas".
        playerInput.actions.Enable();
    }

    private void OnDisable()
    {
        // Boa prática: desligar as ações quando o objeto for desativado.
        playerInput.actions.Disable();
    }
}