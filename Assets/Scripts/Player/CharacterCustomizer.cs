// Cliente/Scripts/Player/CharacterCustomizer.cs
// Cliente/Scripts/Player/CharacterCustomizer.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Animator))]
public class CharacterCustomizer : MonoBehaviour
{
    #region Estruturas de Dados
    [System.Serializable]
    public class BodyPartMeshes
    {
        public SkinnedMeshRenderer Head;
        public SkinnedMeshRenderer Torso;
        public SkinnedMeshRenderer Hand;
        public SkinnedMeshRenderer Legs;
        public SkinnedMeshRenderer Feet;
    }

    [System.Serializable]
    public class GenderSpecificModel
    {
        public GameObject ModelRoot;
        public Avatar Avatar;
        public AnimatorOverrideController OverrideController;
        public BodyPartMeshes BodyParts;

        [Header("Esqueleto")]
        [Tooltip("Arraste o osso principal (geralmente 'Hips' ou 'Armature') do esqueleto para cá.")]
        public Transform RootBone;

        [Header("Pontos de Ancoragem")]
        public Transform Hand_R;
        public Transform Hand_L;
        public Transform Back_Large;
        public Transform Hip_R;

        [Header("Customização Base")]
        public List<GameObject> HairStyles;
        public List<GameObject> FaceStyles;
        public List<GameObject> BeardStyles;
    }
    #endregion

    #region Propriedades do Inspector
    [Header("Configuração dos Modelos")]
    [SerializeField] private GenderSpecificModel femaleData;
    [SerializeField] private GenderSpecificModel maleData;

    [Header("Configuração Modo BATATA")]
    [SerializeField] private GameObject batataModelPrefab;
    #endregion

    #region Estado Interno
    public bool IsFemale { get; private set; } = true;
    public GenderSpecificModel CurrentGenderData => IsFemale ? femaleData : maleData;

    private CharacterAppearance _appearance = new CharacterAppearance();
    private readonly Dictionary<EquipmentSlot, GameObject> _equippedVisuals = new Dictionary<EquipmentSlot, GameObject>();
    private readonly Dictionary<EquipmentSlot, bool> _slotVisibility = new Dictionary<EquipmentSlot, bool>();

    private GameObject _currentHairInstance, _currentFaceInstance, _currentBeardInstance;
    private Animator _animator;
    // private MaterialPropertyBlock _propBlock;
    private bool _isUnsheathed = false;

    private Renderer _activeHeadRenderer;

    [Header("Configuração de Layers")]
    [Tooltip("Nome da layer configurada para não ser renderizada pela câmera principal.")]
    [SerializeField] private string invisibleLayerName = "InvisibleCharacterParts";
    private int _defaultLayer;
    private int _invisibleLayer;
    private GameObject _currentBatataInstance;
    private bool _isPotatoMode = false;
    #endregion

    #region Ciclo de Vida e Inicialização

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        // _propBlock = new MaterialPropertyBlock();

        // NOVO: Inicializa as layers
        _defaultLayer = gameObject.layer; // Pega a layer padrão do personagem
        _invisibleLayer = LayerMask.NameToLayer(invisibleLayerName);
        if (_invisibleLayer == -1)
        {
            Debug.LogError($"A Layer '{invisibleLayerName}' não foi encontrada! Por favor, configure-a em Edit > Project Settings > Tags and Layers.", this);
        }

        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
        {
            if (slot >= EquipmentSlot.Head && slot <= EquipmentSlot.Feet)
            {
                _slotVisibility[slot] = true;
            }
        }

        // VERSÃO RECOMENDADA
        if (femaleData != null)
        {
            if (femaleData.ModelRoot != null)
            {
                femaleData.ModelRoot.SetActive(false);
            }
        }

        if (maleData != null)
        {
            if (maleData.ModelRoot != null)
            {
                maleData.ModelRoot.SetActive(false);
            }
        }

    }

    private void OnEnable()
    {
        SettingsManager.OnPotatoModeToggled += TogglePotatoMode;
    }

    private void OnDisable()
    {
        SettingsManager.OnPotatoModeToggled -= TogglePotatoMode; // Se desinscreve para evitar erros
        ClearAllVisuals();
    }

    private void TogglePotatoMode(bool isPotato)
    {
        _isPotatoMode = isPotato;

        // Se o modo BATATA foi ativado
        if (_isPotatoMode)
        {
            // Desativa os modelos 3D
            if (femaleData?.ModelRoot != null) femaleData.ModelRoot.SetActive(false);
            if (maleData?.ModelRoot != null) maleData.ModelRoot.SetActive(false);

            // Destrói qualquer instância BATATA antiga para garantir
            if (_currentBatataInstance != null) Destroy(_currentBatataInstance);

            // Instancia o novo modelo BATATA como filho do customizador
            if (batataModelPrefab != null)
            {
                // 1. Instancia o prefab como filho deste objeto (sua posição local será 0,0,0 por padrão)
                _currentBatataInstance = Instantiate(batataModelPrefab, transform);

                // 2. AJUSTE: Define a posição local para (0, 1, 0)
                _currentBatataInstance.transform.localPosition = new Vector3(0f, 1f, 0f);
            }
        }
        // Se o modo BATATA foi desativado
        else
        {
            // Destrói a instância BATATA
            if (_currentBatataInstance != null) Destroy(_currentBatataInstance);

            // Reativa o modelo 3D correto reaplicando a aparência atual
            ApplyAppearance(_appearance);
        }
    }

    public void ApplyAppearance(CharacterAppearance appearance)
    {
        if (appearance == null) return;
        _appearance = appearance;

        // NOVO: Se estiver no modo batata, não faça nada aqui.
        if (_isPotatoMode) return;

        // O resto do seu método ApplyAppearance continua igual...
        IsFemale = _appearance.IsFemale;
        femaleData.ModelRoot.SetActive(IsFemale);
        maleData.ModelRoot.SetActive(!IsFemale);

        _animator.avatar = CurrentGenderData.Avatar;
        if (CurrentGenderData.OverrideController != null)
            _animator.runtimeAnimatorController = CurrentGenderData.OverrideController;

        _activeHeadRenderer = CurrentGenderData.BodyParts.Head;

        ReapplyBaseAppearance();
    }

    #endregion

    #region Visibilidade de Equipamento

    public void ToggleSlotVisibility(EquipmentSlot slot)
    {
        if (slot < EquipmentSlot.Head || slot > EquipmentSlot.Feet) return;
        if (!_equippedVisuals.TryGetValue(slot, out GameObject visualInstance)) return;

        bool isCurrentlyVisible = _slotVisibility.ContainsKey(slot) ? _slotVisibility[slot] : true;
        bool newVisibility = !isCurrentlyVisible;
        _slotVisibility[slot] = newVisibility;

        visualInstance.SetActive(newVisibility);
        SetBodyPartVisibility(slot, !newVisibility);
    }

    #endregion

    #region Lógica de Equipamento (Player vs. NPC)

    public void UpdateEquipmentVisualsForPlayer(Dictionary<EquipmentSlot, string> equipmentData)
    {
        ClearAllVisuals();
        ReapplyBaseAppearance();
        if (equipmentData == null) return;

        foreach (var pair in equipmentData)
        {
            if (string.IsNullOrEmpty(pair.Value) || pair.Value.ToLower() == "null") continue;
            Item itemData = GameDatabase.Instance.GetItem(pair.Value);
            if (itemData != null) ShowVisual(itemData, isSheathed: true);
        }
        _isUnsheathed = false;
    }


    public void UpdateEquipmentVisualsForNpc(List<Item> equipment)
    {
        ClearAllVisuals();
        ReapplyBaseAppearance();
        if (equipment == null) return;

        foreach (var item in equipment)
        {
            if (item != null) ShowVisual(item, isSheathed: false);
        }
        _isUnsheathed = true;
    }

    #endregion

    #region Sistema Visual Central


    private void ShowVisual(Item item, bool isSheathed)
    {
        if (item == null) return;
        EquipmentSlot slot;
        if (item is EquipmentItem ei) slot = ei.equipmentSlot;
        else if (item is WeaponItem wi) slot = wi.equipmentSlot;
        else return;

        if (_equippedVisuals.TryGetValue(slot, out var oldVisual)) Destroy(oldVisual);

        GameObject prefabToShow = GetPrefabForItem(item);
        if (prefabToShow == null) return;

        Transform parentSocket = GetSocketForItem(item, isSheathed);
        GameObject visualInstance = Instantiate(prefabToShow, parentSocket);
        visualInstance.name = $"{item.itemID}_VisualInstance";
        visualInstance.transform.localPosition = Vector3.zero;
        visualInstance.transform.localRotation = Quaternion.identity;
        _equippedVisuals[slot] = visualInstance;

        var newRenderer = visualInstance.GetComponentInChildren<SkinnedMeshRenderer>();
        if (newRenderer != null) ApplyRigging(newRenderer, CurrentGenderData.RootBone);

        if (item is ArmorItem armor)
        {
            ApplyArmorColors(visualInstance, armor);
            bool shouldBeVisible = _slotVisibility.ContainsKey(slot) ? _slotVisibility[slot] : true;
            visualInstance.SetActive(shouldBeVisible);
            SetBodyPartVisibility(slot, !shouldBeVisible);
        }
        else if (item is WeaponItem)
        {
            visualInstance.SetActive(true);
        }

        // NOVO: Após equipar qualquer item, atualize a cor da pele em toda a malha visível.
        UpdateAllSkinRenderers();
    }

    private void ClearAllVisuals()
    {
        foreach (var visual in _equippedVisuals.Values) if (visual != null) Destroy(visual);
        _equippedVisuals.Clear();
        // Habilita todas as partes do corpo base.
        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot))) SetBodyPartVisibility(slot, true);
        // NOVO: Reaplica a cor da pele ao corpo base que acabou de ser reativado.
        UpdateAllSkinRenderers();
    }

    #endregion

    #region Lógica de "Guardar/Sacar" Armas

    public void SheatheAllWeapons()
    {
        if (!_isUnsheathed) return;
        _isUnsheathed = false;

        if (_animator != null)
        {
            int combatLayerIndex = _animator.GetLayerIndex("Combat");
            if (combatLayerIndex != -1)
            {
                _animator.SetLayerWeight(combatLayerIndex, 0f);
            }
        }

        foreach (var pair in _equippedVisuals)
        {
            Item itemData = FindItemForVisual(pair.Key);
            if (itemData is WeaponItem)
            {
                Transform sheathSocket = GetSocketForItem(itemData, isSheathed: true);
                if (sheathSocket != null) pair.Value.transform.SetParent(sheathSocket, false);
            }
        }
    }


    public void UnsheatheAllWeapons()
    {
        if (_isUnsheathed) return;
        _isUnsheathed = true;

        if (_animator != null)
        {
            int combatLayerIndex = _animator.GetLayerIndex("Combat");
            if (combatLayerIndex != -1)
            {
                _animator.SetLayerWeight(combatLayerIndex, 1f);
            }
        }

        foreach (var pair in _equippedVisuals)
        {
            Item itemData = FindItemForVisual(pair.Key);
            if (itemData is WeaponItem)
            {
                Transform handSocket = GetSocketForItem(itemData, isSheathed: false);
                if (handSocket != null) pair.Value.transform.SetParent(handSocket, false);
            }
        }
    }

    private Item FindItemForVisual(EquipmentSlot slot)
    {
        if (_equippedVisuals.TryGetValue(slot, out var visual))
        {
            string itemId = visual.name.Replace("_VisualInstance", "");
            return GameDatabase.Instance.GetItem(itemId);
        }
        return null;
    }

    #endregion

    #region Métodos de Customização e Helpers

    private void ReapplyBaseAppearance()
    {
        if (_currentHairInstance != null) Destroy(_currentHairInstance);
        if (_currentFaceInstance != null) Destroy(_currentFaceInstance);
        if (_currentBeardInstance != null) Destroy(_currentBeardInstance);

        ApplyFaceStyle(_appearance.FaceStyleIndex);
        ApplyHairStyle(_appearance.HairStyleIndex);
        ApplyBeardStyle(_appearance.BeardStyleIndex);
        ApplyAllColorsFromAppearance(_appearance);
    }


    private void ApplyHairStyle(int index)
    {
        if (_currentHairInstance != null) Destroy(_currentHairInstance);
        _appearance.HairStyleIndex = index;
        var list = CurrentGenderData.HairStyles;
        if (list != null && index >= 0 && index < list.Count && list[index] != null)
        {
            _currentHairInstance = InstantiateAndRig(list[index]);
            ReapplyHairColor();
        }
    }

    private void ApplyFaceStyle(int index)
    {
        if (_currentFaceInstance != null) Destroy(_currentFaceInstance);
        _appearance.FaceStyleIndex = index;
        var list = CurrentGenderData.FaceStyles;
        var baseHead = CurrentGenderData.BodyParts.Head;

        // 1. Garante que a cabeça base esteja visível por padrão (na sua layer original).
        // Ambos os componentes precisam estar habilitados para o rig funcionar.
        baseHead.enabled = true;
        SetGameObjectLayerRecursive(baseHead.gameObject, _defaultLayer);
        _activeHeadRenderer = baseHead;

        bool hasCustomFace = list != null && index >= 0 && index < list.Count && list[index] != null;

        if (hasCustomFace)
        {
            _currentFaceInstance = InstantiateAndRig(list[index]);

            // 2. Em vez de desativar, movemos a cabeça base para a layer invisível.
            SetGameObjectLayerRecursive(baseHead.gameObject, _invisibleLayer);

            _activeHeadRenderer = _currentFaceInstance.GetComponentInChildren<Renderer>();
        }

        // 3. Aplica as cores na cabeça que estiver ativa.
        ReapplyFaceColors();
        UpdateAllSkinRenderers(); // Atualiza a cor da pele na nova cabeça ativa
        ReapplyHairColor(); // Atualiza a cor da sobrancelha na nova cabeça ativa
    }

    private void ApplyBeardStyle(int index)
    {
        if (_currentBeardInstance != null) Destroy(_currentBeardInstance);
        if (IsFemale) return;
        _appearance.BeardStyleIndex = index;
        var list = CurrentGenderData.BeardStyles;
        if (list != null && index >= 0 && index < list.Count && list[index] != null)
        {
            _currentBeardInstance = InstantiateAndRig(list[index]);
            ReapplyHairColor();
        }
    }

    private void SetBodyPartVisibility(EquipmentSlot slot, bool isVisible)
    {
        var activeParts = CurrentGenderData.BodyParts;
        SkinnedMeshRenderer partToToggle = null;

        if (slot == EquipmentSlot.Head)
        {
            if (_currentHairInstance != null) _currentHairInstance.SetActive(isVisible);
            if (_currentBeardInstance != null) _currentBeardInstance.SetActive(isVisible);
        }

        switch (slot)
        {
            case EquipmentSlot.Chest: partToToggle = activeParts.Torso; break;
            case EquipmentSlot.Legs: partToToggle = activeParts.Legs; break;
            case EquipmentSlot.Feet: partToToggle = activeParts.Feet; break;
            case EquipmentSlot.Hands: partToToggle = activeParts.Hand; break;
        }

        if (partToToggle != null)
        {
            partToToggle.enabled = isVisible;
        }
    }

    private GameObject GetPrefabForItem(Item item)
    {
        if (item is ArmorItem armor) return IsFemale ? armor.femaleArmorPrefab : armor.maleArmorPrefab;
        if (item is WeaponItem weapon) return weapon.weaponPrefab;
        return null;
    }

    private Transform GetSocketForItem(Item item, bool isSheathed)
    {
        if (item is WeaponItem weapon)
        {
            if (isSheathed)
            {
                return weapon.sheathSlot switch
                {
                    WeaponSheathSlot.Back_Large => CurrentGenderData.Back_Large,
                    WeaponSheathSlot.Hip_R => CurrentGenderData.Hip_R,
                    _ => CurrentGenderData.Back_Large,
                };
            }
            return weapon.equipmentSlot == EquipmentSlot.MainHand ? CurrentGenderData.Hand_R : CurrentGenderData.Hand_L;
        }
        return CurrentGenderData.ModelRoot.transform;
    }


    #endregion

    #region Métodos de Rigging e Cores

    private GameObject InstantiateAndRig(GameObject prefab)
    {
        if (prefab == null || CurrentGenderData.ModelRoot == null) return null;
        GameObject instance = Instantiate(prefab, CurrentGenderData.ModelRoot.transform);
        var renderer = instance.GetComponentInChildren<SkinnedMeshRenderer>();
        if (renderer != null) ApplyRigging(renderer, CurrentGenderData.RootBone);
        return instance;
    }

    private void ApplyRigging(SkinnedMeshRenderer newRenderer, Transform rootBone)
    {
        if (rootBone == null)
        {
            Debug.LogError("Root Bone não configurado!", this);
            return;
        }
        var boneMap = new Dictionary<string, Transform>();
        foreach (var bone in rootBone.GetComponentsInChildren<Transform>()) boneMap[bone.name] = bone;

        var newBones = new Transform[newRenderer.bones.Length];
        for (int i = 0; i < newRenderer.bones.Length; i++)
        {
            if (newRenderer.bones[i] != null && boneMap.TryGetValue(newRenderer.bones[i].name, out Transform matchingBone))
                newBones[i] = matchingBone;
            else
                newBones[i] = rootBone;
        }
        newRenderer.bones = newBones;
        newRenderer.rootBone = rootBone;
    }



    private void ApplyArmorColors(GameObject armorInstance, ArmorItem item)
    {
        try
        {
            if (item.ColorVariations == null || item.ColorVariations.Count == 0) return;
            var colorVar = item.ColorVariations[0];
            if (colorVar == null) return;

            var armorRenderer = armorInstance.GetComponentInChildren<Renderer>();
            if (armorRenderer == null) return;

            // ALTERADO: Criamos um novo MPB aqui para garantir que a operação seja atômica.
            // Isso evita que o _propBlock da classe interfira ou seja sujo por esta operação.
            MaterialPropertyBlock armorPropBlock = new MaterialPropertyBlock();

            armorRenderer.GetPropertyBlock(armorPropBlock); // Pega as propriedades existentes, se houver.

            const string leather1 = "_LEATHER1COLOR", leather2 = "_LEATHER2COLOR", leather3 = "_LEATHER3COLOR";
            const string metal1 = "_PLATE1COLOR", metal2 = "_PLATE2COLOR", metal3 = "_PLATE3COLOR";
            const string cloth1 = "_CLOTH1COLOR", cloth2 = "_CLOTH2COLOR", cloth3 = "_CLOTH3COLOR";

            switch (item.ColorType)
            {
                case ArmorType.Cloth:
                    armorPropBlock.SetColor(cloth1, colorVar.PrimaryColor);
                    armorPropBlock.SetColor(cloth2, colorVar.SecondaryColor);
                    armorPropBlock.SetColor(cloth3, colorVar.TrimColor);
                    break;
                case ArmorType.Leather:
                    armorPropBlock.SetColor(leather1, colorVar.PrimaryColor);
                    armorPropBlock.SetColor(leather2, colorVar.SecondaryColor);
                    armorPropBlock.SetColor(leather3, colorVar.TrimColor);
                    break;
                case ArmorType.Mail:
                case ArmorType.Plate:
                    armorPropBlock.SetColor(metal1, colorVar.PrimaryColor);
                    armorPropBlock.SetColor(metal2, colorVar.SecondaryColor);
                    armorPropBlock.SetColor(metal3, colorVar.TrimColor);
                    break;
            }

            armorRenderer.SetPropertyBlock(armorPropBlock);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Falha ao aplicar cores no item {item.itemID}. Erro: {ex.Message}");
        }
    }
    #endregion

    #region Métodos Públicos para UI

    public void SetGender(bool isFemale)
    {
        if (IsFemale == isFemale) return;
        ClearAllVisuals();
        _appearance.IsFemale = isFemale;
        ApplyAppearance(_appearance);
    }

    public int GetCurrentHairStyleCount() => CurrentGenderData.HairStyles?.Count ?? 0;
    public int GetCurrentFaceStyleCount() => CurrentGenderData.FaceStyles?.Count ?? 0;
    public int GetCurrentBeardStyleCount() => CurrentGenderData.BeardStyles?.Count ?? 0;

    public void NextHairStyle() { int c = GetCurrentHairStyleCount(); if (c > 0) ApplyHairStyle((_appearance.HairStyleIndex + 1) % c); }
    public void PreviousHairStyle() { int c = GetCurrentHairStyleCount(); if (c > 0) ApplyHairStyle((_appearance.HairStyleIndex - 1 + c) % c); }
    public void NextFaceStyle() { int c = GetCurrentFaceStyleCount(); if (c > 0) ApplyFaceStyle((_appearance.FaceStyleIndex + 1) % c); }
    public void PreviousFaceStyle() { int c = GetCurrentFaceStyleCount(); if (c > 0) ApplyFaceStyle((_appearance.FaceStyleIndex - 1 + c) % c); }
    public void NextBeardStyle() { if (IsFemale) return; int c = GetCurrentBeardStyleCount(); if (c > 0) ApplyBeardStyle((_appearance.BeardStyleIndex + 1) % c); }
    public void PreviousBeardStyle() { if (IsFemale) return; int c = GetCurrentBeardStyleCount(); if (c > 0) ApplyBeardStyle((_appearance.BeardStyleIndex - 1 + c) % c); }

    public void SetSkinColor(Color color)
    {
        _appearance.SkinColorHex = "#" + ColorUtility.ToHtmlStringRGB(color);
        UpdateAllSkinRenderers(); // ALTERADO: Usa o novo método centralizado
    }

    public void SetHairColor(Color color)
    {
        _appearance.HairColorHex = "#" + ColorUtility.ToHtmlStringRGB(color);
        ReapplyHairColor();
    }

    public void SetEyeColor(Color color)
    {
        _appearance.EyeColorHex = "#" + ColorUtility.ToHtmlStringRGB(color);
        ReapplyFaceColors();
    }

    public void SetScleraColor(Color color)
    {
        _appearance.ScleraColorHex = "#" + ColorUtility.ToHtmlStringRGB(color);
        ReapplyFaceColors();
    }

    public void SetLipsColor(Color color)
    {
        _appearance.LipsColorHex = "#" + ColorUtility.ToHtmlStringRGB(color);
        ReapplyFaceColors();
    }
    #endregion

    #region Lógica de Aplicação de Cor (Refatorado)

    private void ApplyAllColorsFromAppearance(CharacterAppearance appearance)
    {
        if (appearance == null) return;
        UpdateAllSkinRenderers(); // ALTERADO: Usa o novo método centralizado
        ReapplyHairColor();
        ReapplyFaceColors();
    }

    private void UpdateAllSkinRenderers()
    {
        if (!ColorUtility.TryParseHtmlString(_appearance.SkinColorHex, out Color skin))
            return; // Sai se a cor for inválida.

        var renderersToColor = new List<Renderer>();

        // 1. Adiciona a cabeça ativa (seja a base ou a customizada).
        if (_activeHeadRenderer != null)
        {
            renderersToColor.Add(_activeHeadRenderer);
        }

        // 2. Adiciona as partes do corpo base que estiverem VISÍVEIS.
        var bodyParts = CurrentGenderData.BodyParts;
        if (bodyParts.Torso != null && bodyParts.Torso.enabled) renderersToColor.Add(bodyParts.Torso);
        if (bodyParts.Hand != null && bodyParts.Hand.enabled) renderersToColor.Add(bodyParts.Hand);
        if (bodyParts.Legs != null && bodyParts.Legs.enabled) renderersToColor.Add(bodyParts.Legs);
        if (bodyParts.Feet != null && bodyParts.Feet.enabled) renderersToColor.Add(bodyParts.Feet);

        // 3. Adiciona os renderers de TODAS as armaduras equipadas.
        // Se a armadura não tiver a propriedade _SKINCOLOR no shader, nada acontece. É seguro.
        foreach (var visual in _equippedVisuals.Values)
        {
            var armorRenderer = visual.GetComponentInChildren<Renderer>();
            if (armorRenderer != null)
            {
                renderersToColor.Add(armorRenderer);
            }
        }

        // 4. Aplica a cor a todos os renderers encontrados, de uma vez.
        ApplyColorToRenderers("_SKINCOLOR", skin, renderersToColor.ToArray());
    }

    // NOVO: Método centralizado para reaplicar a cor do cabelo/barba/sobrancelha
    private void ReapplyHairColor()
    {
        if (ColorUtility.TryParseHtmlString(_appearance.HairColorHex, out Color hair))
        {
            ApplyColorToInstance(_currentHairInstance, "_HAIRCOLOR", hair);
            ApplyColorToInstance(_currentBeardInstance, "_HAIRCOLOR", hair);

            if (_activeHeadRenderer != null)
            {
                ApplyColorToRenderers("_HAIRCOLOR", hair, _activeHeadRenderer);
            }
        }
    }

    private void ReapplyFaceColors()
    {
        if (_activeHeadRenderer == null) return;

        if (ColorUtility.TryParseHtmlString(_appearance.EyeColorHex, out Color eyes)) ApplyColorToRenderers("_EYESCOLOR", eyes, _activeHeadRenderer);
        if (ColorUtility.TryParseHtmlString(_appearance.ScleraColorHex, out Color sclera)) ApplyColorToRenderers("_SCLERACOLOR", sclera, _activeHeadRenderer);
        if (ColorUtility.TryParseHtmlString(_appearance.LipsColorHex, out Color lips)) ApplyColorToRenderers("_LIPSCOLOR", lips, _activeHeadRenderer);
        if (ColorUtility.TryParseHtmlString(_appearance.ScarColorHex, out Color scar)) ApplyColorToRenderers("_SCARSCOLOR", scar, _activeHeadRenderer);
    }

    private void ApplyColorToRenderers(string propertyName, Color color, params Renderer[] renderers)
    {
        // NOVO: Criamos um MPB temporário para esta operação.
        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

        foreach (var r in renderers)
        {
            if (r != null)
            {
                // O padrão correto é:
                // 1. Pegar o bloco existente do renderer para não perder outras propriedades.
                r.GetPropertyBlock(propBlock);
                // 2. Definir a cor que queremos alterar.
                propBlock.SetColor(propertyName, color);
                // 3. Aplicar o bloco modificado de volta.
                r.SetPropertyBlock(propBlock);
            }
        }
    }

    private void ApplyColorToInstance(GameObject instance, string propertyName, Color color)
    {
        if (instance != null)
        {
            var renderer = instance.GetComponentInChildren<Renderer>();
            if (renderer != null) ApplyColorToRenderers(propertyName, color, renderer);
        }
    }

    private Renderer[] GetAllBodyRenderers()
    {
        var p = CurrentGenderData.BodyParts;
        return new Renderer[] { p.Head, p.Torso, p.Hand, p.Legs, p.Feet }.Where(r => r != null).ToArray();
    }

    private void SetGameObjectLayerRecursive(GameObject obj, int layer)
    {
        if (obj == null || _invisibleLayer == -1) return;

        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetGameObjectLayerRecursive(child.gameObject, layer);
        }
    }

    #endregion
}