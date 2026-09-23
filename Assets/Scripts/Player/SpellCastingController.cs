// Cliente/Scripts/Player/SpellCastingController.cs
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(NetworkCharacter), typeof(Animator))]
public class SpellCastingController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InputReader inputReader;

    public bool IsCasting { get; private set; }

    // Estado do casting
    private float _castTimer;
    private float _currentCastTime;
    private Ability _currentAbility; // Usado apenas para habilidades reais
    private string _currentActionName; // (NOVO) Para armazenar o nome de ações genéricas
    private Coroutine _actionTimeoutCoroutine;

    // Referências de componentes
    private NetworkCharacter _networkCharacter;
    private Animator _animator;

    private void Awake()
    {
        _networkCharacter = GetComponent<NetworkCharacter>();
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (IsCasting)
        {
            _castTimer += Time.deltaTime;
            if (_castTimer > _currentCastTime) _castTimer = _currentCastTime;
            UI_CastBar.Instance.UpdateCastTime(_castTimer);
        }
    }

    private void OnEnable()
    {
        if (inputReader == null) return;
        inputReader.GameMenuEvent += OnCancelInput;
    }

    private void OnDisable()
    {
        if (inputReader == null) return;
        inputReader.GameMenuEvent -= OnCancelInput;
    }

    private void OnCancelInput()
    {
        if (IsCasting)
        {
            RequestInterruptCasting();
        }
    }

    public void RequestCasting(Ability ability, string targetId)
    {
        if (IsCasting) return;
        _networkCharacter.SendAbilityRequest(ability.ID, targetId);
    }

    /// <summary>
    /// CHAMADO PELO UDPClient quando o servidor autoriza o cast de uma HABILIDADE.
    /// </summary>
    public void HandleServerCastStarted(string abilityID, float castTime)
    {
        Ability ability = GameDatabase.Instance.GetAbility(abilityID);
        if (ability == null) return;

        // Inicia o estado de casting
        _currentAbility = ability;
        _currentActionName = ability.abilityName;
        StartCastingState(castTime);

        // Inicia a UI e animações
        UI_CastBar.Instance.StartCasting(_currentActionName, _currentCastTime);
        _networkCharacter.StartVisualCasting(abilityID);
    }


    // =========================================================================
    // <<< NOVO MÉTODO PARA COLETA E OUTRAS AÇÕES >>>
    // =========================================================================
    /// <summary>
    /// CHAMADO PELO UDPClient para ações genéricas que usam a barra de progresso, como coletar.
    /// </summary>
    public void StartGenericAction(string actionName, float duration)
    {
        if (IsCasting) return;

        // Inicia o estado de casting
        _currentAbility = null;
        _currentActionName = actionName;
        StartCastingState(duration);

        // Inicia a UI
        UI_CastBar.Instance.StartCasting(_currentActionName, _currentCastTime);
    }

    private void StartCastingState(float duration)
    {
        _currentCastTime = duration;
        _castTimer = 0f;
        IsCasting = true;

        // Inicia a corrotina de segurança que vai forçar o cancelamento
        // se a conclusão não chegar a tempo. Adicionamos 0.5s de margem para a latência da rede.
        if (_actionTimeoutCoroutine != null) StopCoroutine(_actionTimeoutCoroutine);
        _actionTimeoutCoroutine = StartCoroutine(ActionTimeout(duration + 0.5f));
    }

    private IEnumerator ActionTimeout(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Se esta corrotina terminar, significa que o pacote de conclusão/falha do servidor foi perdido.
        // Forçamos a limpeza do estado local para "destravar" o jogador.
        Debug.LogWarning("[Casting Timeout] O servidor não confirmou o fim da ação. Forçando reset do estado do cliente.");
        FinishClientCasting();
    }


    public void RequestInterruptCasting()
    {
        if (!IsCasting) return;
        _networkCharacter.SendCancelCastRequest();
    }

    public void OnServerExecute()
    {
        if (!IsCasting) return;

        // O servidor confirmou a conclusão, então podemos parar o timeout de segurança.
        if (_actionTimeoutCoroutine != null) StopCoroutine(_actionTimeoutCoroutine);
        _actionTimeoutCoroutine = null;

        FinishClientCasting();
    }

    public void HandleServerCastCanceled(string reason)
    {
        if (!IsCasting) return;

        // O servidor confirmou o cancelamento, paramos o timeout.
        if (_actionTimeoutCoroutine != null) StopCoroutine(_actionTimeoutCoroutine);
        _actionTimeoutCoroutine = null;

        UI_FeedbackManager.Instance?.ShowFeedback(reason);
        FinishClientCasting();
    }

    private void FinishClientCasting()
    {
        // Se já não estivermos em casting, não faz nada para evitar bugs.
        if (!IsCasting) return;

        // Garante que o timeout seja parado, caso esta função seja chamada de outro lugar.
        if (_actionTimeoutCoroutine != null)
        {
            StopCoroutine(_actionTimeoutCoroutine);
            _actionTimeoutCoroutine = null;
        }

        IsCasting = false;
        _currentAbility = null;
        _currentActionName = null;

        UI_CastBar.Instance.StopCasting();
        _networkCharacter.StopVisualCasting();
    }
}