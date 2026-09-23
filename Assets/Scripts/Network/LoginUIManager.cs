using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem; // Essencial para o parâmetro do método

public class LoginUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField _usernameField;
    [SerializeField] private TMP_InputField _passwordField;
    [SerializeField] private Button _loginButton;
    [SerializeField] private Button _registerButton;
    [SerializeField] private Toggle _rememberMeToggle;
    [SerializeField] private TextMeshProUGUI _feedbackText;

    private TCPNetworkClient _tcpClient;

    // Chaves para salvar os dados no PlayerPrefs
    private const string UsernameKey = "RememberedUsername";
    private const string PasswordKey = "RememberedPassword";
    private const string RememberMeKey = "RememberMeOption";

    void Start()
    {
        _tcpClient = GameFlowManager.Instance.TcpClient;

        _loginButton.onClick.AddListener(OnLoginClicked);
        _registerButton.onClick.AddListener(OnRegisterClicked);

        _tcpClient.OnLoginResponse += HandleLoginResponse;
        _tcpClient.OnRegisterResponse += HandleRegisterResponse;
        _tcpClient.OnConnected += HandleConnected;
        _tcpClient.OnDisconnected += HandleDisconnected;

        LoadCredentials();
        SetUIInteractable(true);

        if (!string.IsNullOrEmpty(GameFlowManager.DisconnectReason))
        {
            ShowError(GameFlowManager.DisconnectReason);
            GameFlowManager.ClearDisconnectReason();
        }
    }

    // O Update() foi removido! O PlayerInput e o EventSystem cuidam de tudo.

    /// <summary>
    /// Método público chamado pelo evento "Submit" do PlayerInput.
    /// </summary>
    public void OnSubmit(InputAction.CallbackContext context)
    {
        // Garante que a ação só seja executada uma vez por pressionamento (não no hold ou release)
        // E verifica se o botão de login está ativo para evitar login em momentos inadequados.
        if (context.performed && _loginButton.interactable)
        {
            OnLoginClicked();
        }
    }

    private void ShowError(string message)
    {
        _feedbackText.text = message;
        _feedbackText.gameObject.SetActive(true);
    }

    void OnDestroy()
    {
        // A limpeza dos listeners de evento de rede continua sendo uma boa prática
        if (_tcpClient != null)
        {
            _tcpClient.OnLoginResponse -= HandleLoginResponse;
            _tcpClient.OnRegisterResponse -= HandleRegisterResponse;
            _tcpClient.OnConnected -= HandleConnected;
            _tcpClient.OnDisconnected -= HandleDisconnected;
        }
    }

    private async void OnLoginClicked()
    {
        if (_rememberMeToggle.isOn)
        {
            SaveCredentials();
        }
        else
        {
            ClearCredentials();
        }

        SetUIInteractable(false);
        _feedbackText.text = "Conectando ao servidor...";

        await _tcpClient.ConnectToAuthServerAsync();

        if (_tcpClient.IsConnected)
        {
            _feedbackText.text = "Autenticando...";
            var request = new LoginRequest
            {
                Command = "login",
                Username = _usernameField.text,
                Password = _passwordField.text,
                ClientVersion = ConfigManager.GameVersion
            };
            await _tcpClient.SendTcpRequest(request);
        }
        else
        {
            _feedbackText.text = "Falha ao conectar. Verifique sua internet ou tente novamente mais tarde.";
            SetUIInteractable(true);
        }
    }

    private async void OnRegisterClicked()
    {
        SetUIInteractable(false);
        _feedbackText.text = "Conectando para registrar...";

        await _tcpClient.ConnectToAuthServerAsync();

        if (_tcpClient.IsConnected)
        {
            _feedbackText.text = "Enviando dados de registro...";
            var request = new RegisterRequest
            {
                Command = "register",
                Username = _usernameField.text,
                Password = _passwordField.text
            };
            await _tcpClient.SendTcpRequest(request);
        }
        else
        {
            _feedbackText.text = "Falha ao conectar para registrar.";
            SetUIInteractable(true);
        }
    }

    #region Handlers e Métodos Auxiliares (permanecem inalterados)

    private void HandleRegisterResponse(BaseResponse response)
    {
        _feedbackText.text = response.Message;
        SetUIInteractable(true);
        _tcpClient.Disconnect();
    }

    private void HandleLoginResponse(CharacterListResponse response)
    {
        _feedbackText.text = response.Message;
        if (response.Success)
        {
            GameFlowManager.Instance.OnLoginSuccess(response);
        }
        else
        {
            SetUIInteractable(true);
            _tcpClient.Disconnect();
        }
    }

    private void HandleConnected()
    {
        _feedbackText.text = "Conexão estabelecida.";
    }

    private void HandleDisconnected()
    {
        SetUIInteractable(true);
    }

    private void SetUIInteractable(bool isInteractable)
    {
        _loginButton.interactable = isInteractable;
        _registerButton.interactable = isInteractable;
        _usernameField.interactable = isInteractable;
        _passwordField.interactable = isInteractable;
    }

    private void SaveCredentials()
    {
        PlayerPrefs.SetString(UsernameKey, _usernameField.text);
        PlayerPrefs.SetString(PasswordKey, _passwordField.text);
        PlayerPrefs.SetInt(RememberMeKey, _rememberMeToggle.isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void LoadCredentials()
    {
        if (PlayerPrefs.GetInt(RememberMeKey, 0) == 1)
        {
            _usernameField.text = PlayerPrefs.GetString(UsernameKey, "");
            _passwordField.text = PlayerPrefs.GetString(PasswordKey, "");
            _rememberMeToggle.isOn = true;
        }
    }

    private void ClearCredentials()
    {
        PlayerPrefs.DeleteKey(UsernameKey);
        PlayerPrefs.DeleteKey(PasswordKey);
        PlayerPrefs.DeleteKey(RememberMeKey);
        PlayerPrefs.Save();
    }

    #endregion
}