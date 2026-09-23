// // Exemplo para UI_CharacterPanel.cs
// using UnityEngine;
// using TMPro;

// public class UI_CharacterPanel : MonoBehaviour
// {
//   [SerializeField] private TextMeshProUGUI strengthText;
//     [SerializeField] private TextMeshProUGUI agilityText;
//     [SerializeField] private TextMeshProUGUI healthText;
//     // ... outras referências de texto

//     private NetworkCharacter _networkCharacter;

//     private void OnEnable()
//     {
//         var localPlayer = UDPClient.Instance.MyPlayerObject;
//         if (localPlayer != null)
//         {
//             // 1. Pega o componente NetworkCharacter, que é a nova fonte da verdade.
//             _networkCharacter = localPlayer.GetComponent<NetworkCharacter>();
//             if (_networkCharacter != null && _networkCharacter.Stats != null)
//             {
//                 // 2. Se inscreve no evento OnStatChanged DENTRO do componente Stats.
//                 _networkCharacter.Stats.OnStatChanged += OnStatsChanged;
//                 UpdateUI(); // Atualiza a UI uma vez ao abrir o painel.
//             }
//         }
//     }

//     private void OnDisable()
//     {
//         // Garante que a inscrição no evento seja removida para evitar erros.
//         if (_networkCharacter != null && _networkCharacter.Stats != null)
//         {
//             _networkCharacter.Stats.OnStatChanged -= OnStatsChanged;
//         }
//     }

//         private void OnStatsChanged(StatType statType)
//     {
//         UpdateUI();
//     }

//     private void UpdateUI()
//     {
//         if (_networkCharacter == null || _networkCharacter.Stats == null) return;

//         // Lê os valores diretamente das propriedades de conveniência do ClientCharacterStats.
//         strengthText.text = _networkCharacter.Stats.Strength.ToString("F0");
//         agilityText.text = _networkCharacter.Stats.Agility.ToString("F0");

//         // Para vida, mostramos o valor atual (do NetworkCharacter) vs. o máximo (dos Stats).
//         healthText.text = $"{_networkCharacter.CurrentHealth:F0} / {_networkCharacter.Stats.MaxHealth:F0}";
//     }
// }