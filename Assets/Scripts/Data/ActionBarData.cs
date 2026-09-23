// Cliente/Scripts/Data/ActionBarData.cs
using System;
using System.Collections.Generic;

// O enum já deve ser idêntico
public enum ActionBarContentType { None, Item, Ability }

[Serializable]
public class ActionBarSlotData
{
    // Adicione { get; set; } para consistência e para a serialização funcionar corretamente
    public ActionBarContentType ContentType { get; set; } = ActionBarContentType.None;
    public string ContentID { get; set; } = string.Empty;
    public string FallbackItemID { get; set; } = string.Empty;
}

[Serializable]
public class ActionBarData
{
    public List<ActionBarSlotData> Slots { get; set; }

    // Construtor principal para o cliente
    public ActionBarData(int size)
    {
        Slots = new List<ActionBarSlotData>();
        for (int i = 0; i < size; i++)
        {
            Slots.Add(new ActionBarSlotData());
        }
    }

    public ActionBarData()
    {
        Slots = new List<ActionBarSlotData>();
    }
}