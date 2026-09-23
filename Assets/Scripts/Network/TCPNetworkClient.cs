using UnityEngine;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Threading;

public class TCPNetworkClient : MonoBehaviour
{
    [Header("Reconnection Settings")]
    private bool _autoReconnectEnabled = true;
    [SerializeField] private float initialReconnectDelay = 1.0f;
    [SerializeField] private float maxReconnectDelay = 30.0f;
    [SerializeField] private float reconnectMultiplier = 2.0f;

    private TcpClient _tcpClient;
    private StreamWriter _streamWriter;
    private StreamReader _streamReader;

    private CancellationTokenSource _reconnectCts;
    private CancellationTokenSource _pingCts; // NOVO: Token para controlar o loop de ping

    private readonly ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();

    // Estado da conexão e eventos para a UI
    public bool IsConnected { get; private set; }
    public Action OnDisconnected;
    public Action OnConnected;

    // Eventos para a UI se inscrever
    public Action<BaseResponse> OnRegisterResponse;
    public Action<CharacterListResponse> OnLoginResponse;
    public Action<SelectCharacterResponse> OnSelectCharacterResponse;
    public Action<CharacterListResponse> OnCreateCharacterResponse;

    void Update()
    {
        while (_mainThreadActions.TryDequeue(out var action))
        {
            action.Invoke();
        }
    }

    public async Task ConnectToAuthServerAsync()
    {
        if (IsConnected) return;

        _reconnectCts?.Cancel();
        _reconnectCts = new CancellationTokenSource();

        try
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(ConfigManager.AuthServerIp, ConfigManager.AuthServerPort);

            var stream = _tcpClient.GetStream();
            _streamWriter = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
            _streamReader = new StreamReader(stream, Encoding.UTF8);

            IsConnected = true;
            _ = Task.Run(ListenForTcpMessages);

            _mainThreadActions.Enqueue(() => OnConnected?.Invoke());

            // NOVO: Inicia o loop de ping após conectar
            _pingCts?.Cancel();
            _pingCts = new CancellationTokenSource();
            _ = Task.Run(() => PingLoopAsync(_pingCts.Token));
        }
        catch (Exception)
        {
            GameFlowManager.Instance.GoToLoginScreen("Não foi possível conectar ao servidor de autenticação.");
            HandleDisconnection();
        }
    }

    private async Task ListenForTcpMessages()
    {
        while (_tcpClient != null && _tcpClient.Connected)
        {
            try
            {
                var jsonResponse = await _streamReader.ReadLineAsync();
                if (jsonResponse == null) break;
                ProcessServerResponse(jsonResponse);
            }
            catch (IOException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (Exception) { break; }
        }
        HandleDisconnection();
    }

    private void HandleDisconnection()
    {
        if (!IsConnected) return;

        IsConnected = false;

        // NOVO: Cancela o loop de ping imediatamente
        _pingCts?.Cancel();

        _tcpClient?.Close();
        _tcpClient = null;
        _streamWriter = null;
        _streamReader = null;

        _mainThreadActions.Enqueue(() =>
        {
            OnDisconnected?.Invoke();
            if (_autoReconnectEnabled && _reconnectCts != null && !_reconnectCts.IsCancellationRequested)
            {
                _ = AttemptReconnectionAsync(_reconnectCts.Token);
            }
        });
    }

    public void DisableAutoReconnect()
    {
        _autoReconnectEnabled = false;
        _reconnectCts?.Cancel();
    }

    private async Task AttemptReconnectionAsync(CancellationToken token)
    {
        float currentDelay = initialReconnectDelay;

        while (!token.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(currentDelay), token);
            if (token.IsCancellationRequested) break;

            await ConnectToAuthServerAsync();

            if (IsConnected) break;

            currentDelay = Mathf.Min(currentDelay * reconnectMultiplier, maxReconnectDelay);
        }

        if (token.IsCancellationRequested)
        {
            GameFlowManager.Instance.GoToLoginScreen("Desconectado do servidor.");
        }
    }

    // NOVO: Loop que envia pings para manter a conexão ativa
    private async Task PingLoopAsync(CancellationToken cancellationToken)
    {
        // Cria um objeto simples para o comando de ping
        var pingRequest = new { Command = "ping" };

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Espera 60 segundos antes de enviar o próximo ping
                await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);

                if (IsConnected)
                {
                    await SendTcpRequest(pingRequest);
                }
            }
            catch (OperationCanceledException)
            {
                // O token foi cancelado, o que é normal ao desconectar. Apenas saia do loop.
                break;
            }
            catch (Exception)
            {
                // Um erro ocorreu ao enviar o ping, provavelmente a conexão já caiu.
                // O loop ListenForTcpMessages vai cuidar da desconexão.
                break;
            }
        }
    }

    private void ProcessServerResponse(string json)
    {
        var baseResponse = JsonConvert.DeserializeObject<BaseResponse>(json);

        switch (baseResponse.Command)
        {
            case "register_response":
                _mainThreadActions.Enqueue(() => OnRegisterResponse?.Invoke(baseResponse));
                break;
            case "login_response":
                var loginResponse = JsonConvert.DeserializeObject<CharacterListResponse>(json);
                _mainThreadActions.Enqueue(() => OnLoginResponse?.Invoke(loginResponse));
                break;
            case "select_character_response":
                var selectResponse = JsonConvert.DeserializeObject<SelectCharacterResponse>(json);
                _mainThreadActions.Enqueue(() => OnSelectCharacterResponse?.Invoke(selectResponse));
                break;
            case "create_character_response":
                var createResponse = JsonConvert.DeserializeObject<CharacterListResponse>(json);
                _mainThreadActions.Enqueue(() => OnCreateCharacterResponse?.Invoke(createResponse));
                break;
            // NOVO: Opcional, caso o servidor envie uma resposta "pong"
            case "pong":
                // Não precisa fazer nada, só de receber o pong já seria uma confirmação
                break;
        }
    }

    public async Task SendTcpRequest(object requestObject)
    {
        if (_streamWriter == null || !IsConnected) return;

        try
        {
            string jsonRequest = JsonConvert.SerializeObject(requestObject);
            await _streamWriter.WriteLineAsync(jsonRequest);
        }
        catch (Exception)
        {
            // O envio falhou, provavelmente desconectado
            HandleDisconnection();
        }
    }

    public void Disconnect()
    {
        _reconnectCts?.Cancel();
        // MODIFICADO: A desconexão manual agora também cancela o ping
        _pingCts?.Cancel();
        HandleDisconnection();
    }

    void OnDestroy()
    {
        _reconnectCts?.Cancel();
        // MODIFICADO: Garante que o ping seja cancelado ao fechar o jogo
        _pingCts?.Cancel();
        _tcpClient?.Close();
    }
}