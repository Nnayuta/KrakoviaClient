// Cliente/Scripts/UI/UI_ColorPalette.cs (NOVO ARQUIVO)
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Collections;

public class UI_ColorPalette : MonoBehaviour
{
    private static UI_ColorPalette _instance;
    public static UI_ColorPalette Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<UI_ColorPalette>(FindObjectsInactive.Include);
            }

            return _instance;
        }

    }

    [Header("Referências")]
    [SerializeField] private GameObject colorButtonPrefab;
    public Transform buttonContainer;

    // Evento que será disparado quando uma cor for selecionada.
    public event Action<Color> OnColorSelected;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        gameObject.SetActive(false); // Garante que comece escondido.
    }

    public void Show(List<Color> colorsToShow)
    {
        // Limpa botões antigos
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        if (colorsToShow == null || colorsToShow.Count == 0) return;

        // Ativa o painel PRIMEIRO
        gameObject.SetActive(true);

        // Cria os novos botões
        foreach (var color in colorsToShow)
        {
            GameObject buttonGO = Instantiate(colorButtonPrefab);
            buttonGO.transform.SetParent(buttonContainer, false);
            buttonGO.transform.localScale = Vector3.one;

            buttonGO.GetComponent<Image>().color = color;

            Color colorToSelect = color;
            buttonGO.GetComponent<Button>().onClick.AddListener(() => SelectColor(colorToSelect));
        }

        // A chamada para a Coroutine e o método da Coroutine foram REMOVIDOS daqui.
    }

    /// <summary>
    /// Chamado quando um botão de cor é clicado.
    /// </summary>
    private void SelectColor(Color selectedColor)
    {
        // Dispara o evento, notificando quem estiver ouvindo.
        OnColorSelected?.Invoke(selectedColor);
        // Esconde a paleta após a seleção.
        Hide();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}