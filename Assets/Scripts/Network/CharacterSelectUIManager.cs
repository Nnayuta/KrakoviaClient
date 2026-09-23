using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using Newtonsoft.Json;

public class CharacterSelectUIManager : MonoBehaviour
{
    public static CharacterSelectUIManager Instance { get; private set; }

    #region Referências do Inspector
    [Header("UI - Painéis")]
    [SerializeField] private GameObject characterSelectPanel;
    [SerializeField] private GameObject createCharacterPanel;

    [Header("Configurações Câmera")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float duracao = 2f;
    [SerializeField] private AnimationCurve curva = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private Transform StartCamera;
    [SerializeField] private Transform createCharacterCamera;

    [Header("UI - Lista de Personagens")]
    [SerializeField] private Transform characterListContainer;
    [SerializeField] private GameObject characterButtonPrefab;
    [SerializeField] private Button HandleSelectChar;
    [SerializeField] private List<Transform> charPositions;

    [Header("UI - Criação de Personagem")]
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button showCreatePanelButton;
    [SerializeField] private Button confirmCreateButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private Transform classButtonContainer;
    [SerializeField] private GameObject classButtonPrefab;
    [SerializeField] private GameObject playerModel;
    [SerializeField] private Transform creatorPosition;
    [SerializeField] private TextMeshProUGUI classtText;

    [Header("UI - Botões de Customização")]
    [SerializeField] private Button nextHairButton;
    [SerializeField] private Button prevHairButton;
    [SerializeField] private Button nextFaceButton;
    [SerializeField] private Button prevFaceButton;
    [SerializeField] private Button nextBeardButton;
    [SerializeField] private Button prevBeardButton;
    [SerializeField] private Button femaleButton;
    [SerializeField] private Button maleButton;
    [SerializeField] private Button editSkinColorButton;
    [SerializeField] private Button editHairColorButton;
    [SerializeField] private Button editLipsColorButton;
    [SerializeField] private Button editEyeColorButton;

    [Header("Paletas de Cores")]
    [SerializeField] private List<Color> skinColors = new List<Color>();
    [SerializeField] private List<Color> hairColors = new List<Color>();
    [SerializeField] private List<Color> eyeColors = new List<Color>();
    [SerializeField] private List<Color> lipsColors = new List<Color>();
    #endregion

    #region Estado Interno
    public CharacterAppearance CurrentAppearanceForCreation { get; private set; } = new CharacterAppearance();
    public bool IsCreatingCharacter { get; private set; } = false;

    private TCPNetworkClient _tcpClient;
    private PlayerClass _selectedClass;
    private Coroutine _cameraTransition;
    private GameObject _creationPlayerInstance;
    private CharacterCustomizer _creationCharacterCustomizer;
    private readonly List<CharacterCustomizer> _characterInstances = new List<CharacterCustomizer>();
    #endregion

    #region Ciclo de Vida do Unity
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _tcpClient = GameFlowManager.Instance.TcpClient;
        if (!_tcpClient.IsConnected)
        {
            GameFlowManager.Instance.GoToLoginScreen("Conexão perdida.");
            return;
        }
        SubscribeToEvents();
        SetupButtonListeners();
        PopulateCharacterList(GameFlowManager.Instance.LastCharacterList);
        PopulateClassSelection();
        SetCreatePanelActive(false);
    }

    private void OnDestroy()
    {
        if (_tcpClient != null)
        {
            _tcpClient.OnSelectCharacterResponse -= HandleSelectCharacterResponse;
            _tcpClient.OnCreateCharacterResponse -= HandleCreateCharacterResponse;
        }
    }
    #endregion

    #region Configuração da UI
    private void SubscribeToEvents()
    {
        _tcpClient.OnSelectCharacterResponse += HandleSelectCharacterResponse;
        _tcpClient.OnCreateCharacterResponse += HandleCreateCharacterResponse;
    }

    private void SetupButtonListeners()
    {
        showCreatePanelButton.onClick.AddListener(() => SetCreatePanelActive(true));
        cancelButton.onClick.AddListener(() => SetCreatePanelActive(false));
        confirmCreateButton.onClick.AddListener(OnConfirmCreateClicked);
        femaleButton.onClick.AddListener(() => OnSetGender(true));
        maleButton.onClick.AddListener(() => OnSetGender(false));
        nextHairButton.onClick.AddListener(OnNextHair);
        prevHairButton.onClick.AddListener(OnPreviousHair);
        nextFaceButton.onClick.AddListener(OnNextFace);
        prevFaceButton.onClick.AddListener(OnPreviousFace);
        nextBeardButton.onClick.AddListener(OnNextBeard);
        prevBeardButton.onClick.AddListener(OnPreviousBeard);
        editSkinColorButton.onClick.AddListener(OnClick_EditSkinColor);
        editHairColorButton.onClick.AddListener(OnClick_EditHairColor);
        editEyeColorButton.onClick.AddListener(OnClick_EditEyeColor);
        editLipsColorButton.onClick.AddListener(OnClick_EditLipsColor);
    }

    private void SetCreatePanelActive(bool isActive)
    {
        createCharacterPanel.SetActive(isActive);
        characterSelectPanel.SetActive(!isActive);
        IsCreatingCharacter = isActive;

        if (isActive)
        {
            ChangeCamera(createCharacterCamera);
            _creationPlayerInstance = Instantiate(playerModel, creatorPosition);
            _creationCharacterCustomizer = _creationPlayerInstance.GetComponent<CharacterCustomizer>();
            CurrentAppearanceForCreation = new CharacterAppearance();
            _creationCharacterCustomizer?.ApplyAppearance(CurrentAppearanceForCreation);
            OnClassSelected(GameDatabase.Instance.GetAllClasses()[0]);
        }
        else
        {
            ChangeCamera(StartCamera);
            if (_creationPlayerInstance != null) Destroy(_creationPlayerInstance);
            _creationCharacterCustomizer = null;
        }
    }

    // >> CORREÇÃO <<: Método adicionado de volta
    private void PopulateClassSelection()
    {
        if (GameDatabase.Instance == null) return;
        foreach (Transform child in classButtonContainer) Destroy(child.gameObject);
        List<PlayerClass> availableClasses = GameDatabase.Instance.GetAllClasses();
        foreach (PlayerClass pc in availableClasses)
        {
            GameObject buttonGO = Instantiate(classButtonPrefab, classButtonContainer);
            buttonGO.GetComponent<UI_ClassButton>().Setup(pc, OnClassSelected);
        }
    }
    #endregion

    #region Lógica da Seleção de Personagem
    private void PopulateCharacterList(CharacterListResponse data)
    {
        foreach (Transform child in characterListContainer) Destroy(child.gameObject);
        foreach (var instance in _characterInstances) if (instance != null) Destroy(instance.gameObject);
        _characterInstances.Clear();
        HandleSelectChar.gameObject.SetActive(false);

        if (data?.Characters == null || data.Characters.Count == 0) return;

        for (int i = 0; i < data.Characters.Count; i++)
        {
            var charSummary = data.Characters[i];
            GameObject buttonGO = Instantiate(characterButtonPrefab, characterListContainer);
            buttonGO.GetComponentInChildren<TextMeshProUGUI>().text = $"{charSummary.Name} - Lvl {charSummary.Level}";
            string charId = charSummary.Id;
            int index = i;
            buttonGO.GetComponent<Button>().onClick.AddListener(() => OnCharacterSelect(charId, index));

            if (i < charPositions.Count && charPositions[i] != null)
            {
                GameObject newCharInstance = Instantiate(playerModel, charPositions[i]);
                var customizer = newCharInstance.GetComponent<CharacterCustomizer>();
                if (customizer != null)
                {
                    customizer.ApplyAppearance(charSummary.Appearance);
                    _characterInstances.Add(customizer);
                    newCharInstance.GetComponent<EquipmentManager>()?.ApplyVisualsForPreview(charSummary.EquippedItems);
                }
            }
        }
        if (data.Characters.Count > 0) OnCharacterSelect(data.Characters[0].Id, 0);
    }

    public void OnCharacterSelect(string characterId, int index)
    {
        HandleSelectChar.gameObject.SetActive(true);
        var confirmBtn = HandleSelectChar.GetComponent<Button>();
        confirmBtn.onClick.RemoveAllListeners();
        confirmBtn.onClick.AddListener(() => OnCharacterSelected(characterId));
        for (int i = 0; i < _characterInstances.Count; i++)
        {
            if (_characterInstances[i] != null) _characterInstances[i].GetComponent<Animator>().SetBool("CharSelected", i == index);
        }
    }
    #endregion

    #region Callbacks de Customização

    private void OnClassSelected(PlayerClass playerClass)
    {
        _selectedClass = playerClass;
        // Debug.Log($"Classe selecionada: {playerClass.className}");

        // Validação: Garante que temos uma referência ao customizer antes de continuar.
        if (_creationCharacterCustomizer == null)
        {
            // Debug.LogError("Tentativa de atualizar o visual da classe, mas _creationCharacterCustomizer é nulo!");
            return;
        }

        classtText.text = playerClass.classDescription;

        // 1. Cria um dicionário vazio para armazenar os dados de equipamento.
        var startingEquipmentData = new Dictionary<EquipmentSlot, string>();

        // 2. Itera sobre a lista de 'startingEquipment' do ScriptableObject da classe.
        if (playerClass.startingEquipment != null)
        {
            foreach (var item in playerClass.startingEquipment)
            {
                // Tenta converter o 'Item' genérico para um 'EquipmentItem'.
                if (item is EquipmentItem equipmentItem)
                {
                    // Se for um item equipável, adiciona seu slot e ID ao dicionário.
                    startingEquipmentData[equipmentItem.equipmentSlot] = equipmentItem.itemID;
                }

                if (item is WeaponItem weaponItem)
                {
                    startingEquipmentData[weaponItem.equipmentSlot] = weaponItem.itemID;
                }
            }
        }

        // 3. Chama o método UpdateEquipmentVisuals, passando o dicionário que acabamos de criar.
        _creationCharacterCustomizer.UpdateEquipmentVisualsForPlayer(startingEquipmentData);
        _creationCharacterCustomizer.GetComponent<Animator>().SetTrigger("LevelUP");
    }

    private void OnSetGender(bool isFemale)
    {
        if (_creationCharacterCustomizer == null) return;
        CurrentAppearanceForCreation.IsFemale = isFemale;
        _creationCharacterCustomizer.SetGender(isFemale);
        ValidateAppearanceIndices();
    }

    private void OnNextHair() => CurrentAppearanceForCreation.HairStyleIndex = CycleCustomization(Next, CurrentAppearanceForCreation.HairStyleIndex, _creationCharacterCustomizer.GetCurrentHairStyleCount, _creationCharacterCustomizer.NextHairStyle);
    private void OnPreviousHair() => CurrentAppearanceForCreation.HairStyleIndex = CycleCustomization(Previous, CurrentAppearanceForCreation.HairStyleIndex, _creationCharacterCustomizer.GetCurrentHairStyleCount, _creationCharacterCustomizer.PreviousHairStyle);
    private void OnNextFace() => CurrentAppearanceForCreation.FaceStyleIndex = CycleCustomization(Next, CurrentAppearanceForCreation.FaceStyleIndex, _creationCharacterCustomizer.GetCurrentFaceStyleCount, _creationCharacterCustomizer.NextFaceStyle);
    private void OnPreviousFace() => CurrentAppearanceForCreation.FaceStyleIndex = CycleCustomization(Previous, CurrentAppearanceForCreation.FaceStyleIndex, _creationCharacterCustomizer.GetCurrentFaceStyleCount, _creationCharacterCustomizer.PreviousFaceStyle);
    private void OnNextBeard() => CurrentAppearanceForCreation.BeardStyleIndex = CycleCustomization(Next, CurrentAppearanceForCreation.BeardStyleIndex, _creationCharacterCustomizer.GetCurrentBeardStyleCount, _creationCharacterCustomizer.NextBeardStyle);
    private void OnPreviousBeard() => CurrentAppearanceForCreation.BeardStyleIndex = CycleCustomization(Previous, CurrentAppearanceForCreation.BeardStyleIndex, _creationCharacterCustomizer.GetCurrentBeardStyleCount, _creationCharacterCustomizer.PreviousBeardStyle);

    private int CycleCustomization(System.Func<int, int, int> cycleFunc, int currentIndex, System.Func<int> getCountFunc, System.Action applyAction)
    {
        if (_creationCharacterCustomizer == null) return currentIndex; // Retorna o índice sem alterá-lo se não houver customizer

        int count = getCountFunc();
        if (count == 0) return currentIndex;

        // Calcula o novo índice
        int newIndex = cycleFunc(currentIndex, count);

        // Aplica a ação visual
        applyAction();

        // Retorna o novo índice para ser atribuído
        return newIndex;
    }

    private int Next(int index, int count) => (index + 1) % count;
    private int Previous(int index, int count) => (index - 1 + count) % count;

    private void ValidateAppearanceIndices()
    {
        if (_creationCharacterCustomizer == null) return;
        int hairCount = _creationCharacterCustomizer.GetCurrentHairStyleCount();
        CurrentAppearanceForCreation.HairStyleIndex = hairCount > 0 ? Mathf.Clamp(CurrentAppearanceForCreation.HairStyleIndex, 0, hairCount - 1) : 0;
        int faceCount = _creationCharacterCustomizer.GetCurrentFaceStyleCount();
        CurrentAppearanceForCreation.FaceStyleIndex = faceCount > 0 ? Mathf.Clamp(CurrentAppearanceForCreation.FaceStyleIndex, 0, faceCount - 1) : 0;
        int beardCount = _creationCharacterCustomizer.GetCurrentBeardStyleCount();
        CurrentAppearanceForCreation.BeardStyleIndex = beardCount > 0 ? Mathf.Clamp(CurrentAppearanceForCreation.BeardStyleIndex, 0, beardCount - 1) : 0;
    }
    #endregion

    #region Callbacks da Paleta de Cores
    private void OnClick_EditSkinColor() => OpenColorPalette(skinColors, ApplySkinColor);
    private void OnClick_EditHairColor() => OpenColorPalette(hairColors, ApplyHairColor);
    private void OnClick_EditEyeColor() => OpenColorPalette(eyeColors, ApplyEyeColor);
    private void OnClick_EditLipsColor() => OpenColorPalette(lipsColors, AppyLipsColor);

    private void OpenColorPalette(List<Color> colorList, System.Action<Color> onSelectAction)
    {
        UI_ColorPalette.Instance.OnColorSelected -= onSelectAction;
        UI_ColorPalette.Instance.OnColorSelected += onSelectAction;
        UI_ColorPalette.Instance.Show(colorList);
        StartCoroutine(RebuildLayoutAfterFrame());
    }

    private void ApplySkinColor(Color newColor)
    {
        _creationCharacterCustomizer?.SetSkinColor(newColor);
        CurrentAppearanceForCreation.SkinColorHex = "#" + ColorUtility.ToHtmlStringRGB(newColor);
        UI_ColorPalette.Instance.OnColorSelected -= ApplySkinColor;
    }
    private void ApplyHairColor(Color newColor)
    {
        _creationCharacterCustomizer?.SetHairColor(newColor);
        CurrentAppearanceForCreation.HairColorHex = "#" + ColorUtility.ToHtmlStringRGB(newColor);
        UI_ColorPalette.Instance.OnColorSelected -= ApplyHairColor;
    }
    private void ApplyEyeColor(Color newColor)
    {
        _creationCharacterCustomizer?.SetEyeColor(newColor);
        CurrentAppearanceForCreation.EyeColorHex = "#" + ColorUtility.ToHtmlStringRGB(newColor);
        UI_ColorPalette.Instance.OnColorSelected -= ApplyEyeColor;
    }

    private void AppyLipsColor(Color newColor)
    {
        _creationCharacterCustomizer?.SetLipsColor(newColor);
        CurrentAppearanceForCreation.LipsColorHex = "#" + ColorUtility.ToHtmlStringRGB(newColor);
        UI_ColorPalette.Instance.OnColorSelected -= AppyLipsColor;
    }
    #endregion

    #region Lógica de Rede
    private async void OnConfirmCreateClicked()
    {
        if (string.IsNullOrWhiteSpace(nameInputField.text)) { feedbackText.text = "O nome é obrigatório."; return; }
        if (_selectedClass == null) { feedbackText.text = "Selecione uma classe."; return; }
        confirmCreateButton.interactable = false;
        feedbackText.text = "Criando...";
        var request = new CreateCharacterRequest { Command = "create_character", Name = nameInputField.text, ClassID = _selectedClass.classID, Appearance = CurrentAppearanceForCreation };
        await _tcpClient.SendTcpRequest(request);
    }

    public async void OnCharacterSelected(string characterId)
    {
        await _tcpClient.SendTcpRequest(new SelectCharacterRequest { Command = "select_character", CharacterId = characterId });
    }

    private void HandleCreateCharacterResponse(CharacterListResponse response)
    {
        feedbackText.text = response.Message;
        confirmCreateButton.interactable = true;
        if (response.Success)
        {
            PopulateCharacterList(response);
            SetCreatePanelActive(false);
        }
    }

    private void HandleSelectCharacterResponse(SelectCharacterResponse response)
    {
        if (response.Success) GameFlowManager.Instance.OnCharacterSelectSuccess(response);
        // else Debug.LogError($"Falha na seleção de personagem: {response.Message}");
    }
    #endregion

    #region Coroutines e Câmera
    // >> CORREÇÃO <<: Métodos adicionados de volta
    public void ChangeCamera(Transform destino)
    {
        if (destino == null) return;
        if (_cameraTransition != null) StopCoroutine(_cameraTransition);
        _cameraTransition = StartCoroutine(Transicao(destino));
    }

    private IEnumerator Transicao(Transform destino)
    {
        if (mainCamera == null) yield break;
        Transform cameraTransform = mainCamera.transform;
        cameraTransform.GetPositionAndRotation(out Vector3 posInicial, out Quaternion rotInicial);
        float tempo = 0f;

        while (tempo < duracao)
        {
            float t = Mathf.Clamp01((tempo += Time.deltaTime) / duracao);
            cameraTransform.SetPositionAndRotation(Vector3.Lerp(posInicial, destino.position, curva.Evaluate(t)), Quaternion.Slerp(rotInicial, destino.rotation, curva.Evaluate(t)));
            yield return null;
        }
        cameraTransform.SetPositionAndRotation(destino.position, destino.rotation);
        _cameraTransition = null;
    }

    private IEnumerator RebuildLayoutAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        var paletteContainer = UI_ColorPalette.Instance.buttonContainer;
        if (paletteContainer != null) LayoutRebuilder.MarkLayoutForRebuild(paletteContainer as RectTransform);
    }
    #endregion
}