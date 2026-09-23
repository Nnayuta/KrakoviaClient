// Cliente/Scripts/UI/ChatManager_Client.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ChatManager_Client : MonoBehaviour
{
    public static ChatManager_Client Instance { get; private set; }

    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private TextMeshProUGUI chatLogText;
    [SerializeField] private ScrollRect chatScrollRect;

    [Header("Input")]
    [Tooltip("Arraste aqui o seu asset InputReader")]
    [SerializeField] private InputReader inputReader; // Referência para o InputReader

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        chatInputField.onSubmit.AddListener(OnSubmitChatMessage);

        // --- ADIÇÕES IMPORTANTES ---
        // Adiciona listeners para quando o campo de input é selecionado e deselecionado
        chatInputField.onSelect.AddListener(OnSelectChat);
        chatInputField.onDeselect.AddListener(OnDeselectChat);
    }


    // NOVO MÉTODO: Chamado quando o jogador clica no campo de chat
    private void OnSelectChat(string text)
    {
        inputReader.SetInputState(GameInputState.UI);
    }

    // NOVO MÉTODO: Chamado quando o jogador clica fora ou pressiona Enter
    private void OnDeselectChat(string text)
    {
        inputReader.SetInputState(GameInputState.Gameplay);
    }

    private void OnSubmitChatMessage(string message)
    {
        // O seu código original já desfoca o campo de input,
        // o que vai disparar automaticamente o evento 'onDeselect'.
        // Então não precisamos mudar nada aqui.
        if (string.IsNullOrWhiteSpace(message))
        {
            chatInputField.DeactivateInputField(true); // Desfoca o campo
            return;
        }

        UDPClient.Instance.SendNetworkMessage($"SEND_CHAT_MSG|{message}");

        chatInputField.text = "";
        chatInputField.DeactivateInputField(true); // Desfoca o campo
    }

    /// <summary>
    /// Chamado pelo UDPClient quando uma mensagem de chat é recebida.
    /// </summary>
    public void AddMessageToLog(string channel, string sender, string message)
    {
        string formattedMessage = "";
        switch (channel)
        {
            case "SAY":
                formattedMessage = $"<b>{sender}:</b> {message}";
                break;
            case "WHISPER_RECV":
                formattedMessage = $"<color=#FF69B4><b>[De: {sender}]</b> {message}</color>"; // Rosa
                break;
            case "WHISPER_SENT":
                formattedMessage = $"<color=#FF69B4><b>[Para: {sender}]</b> {message}</color>"; // Rosa
                break;
            case "SYSTEM":
                formattedMessage = $"<color=yellow><b>[{sender}]</b> {message}</color>"; // Amarelo
                break;
            default:
                formattedMessage = message;
                break;
        }

        chatLogText.text += formattedMessage + "\n";

        StartCoroutine(ForceScrollDown());
    }

    // Corrotina para forçar a rolagem para o final após a UI ser atualizada
    private IEnumerator ForceScrollDown()
    {
        // Espera um frame para que o Content Size Fitter atualize a altura do conteúdo
        yield return new WaitForEndOfFrame();
        chatScrollRect.verticalNormalizedPosition = 0f;
    }
}