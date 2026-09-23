using UnityEngine;

[CreateAssetMenu(fileName = "NetworkConfig", menuName = "Network/Network Configuration")]
public class NetworkConfig : ScriptableObject
{
    [Header("Servidor de Autenticação (TCP)")]
    public string AuthServerIp = "127.0.0.1";
    public int AuthServerPort = 7778;

    [Header("Servidor de Mundo (UDP)")]
    [Tooltip("Este IP é geralmente recebido do servidor de autenticação, mas pode ser usado para debug.")]
    public string DefaultWorldServerIp = "127.0.0.1";
    public int DefaultWorldServerPort = 7777;

    [Header("Versão do Jogo")]
    [Tooltip("Deve corresponder exatamente à versão do servidor.")]
    public string GameVersion = "0.0.1";
}