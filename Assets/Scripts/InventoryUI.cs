using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public struct OreUI
{
    public Image oreIcon;
    public TextMeshProUGUI oreAmountText;
}

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private GameObject OriGirdUIPrefab;
    [SerializeField] private RectTransform OreUIParent;
    [SerializeField] private int GridWidth = 100;
    [SerializeField] private int GridSpacing = 10;
    List<OreUI> OreUIList = new List<OreUI>();

    public void CreateOreUI(InventoryManger inventoryManger)
    {
        OreUIParent.sizeDelta = new Vector2(OreUIParent.sizeDelta.x, (GridWidth + GridSpacing) * inventoryManger.OreList.Length);
        OreUIList = new List<OreUI>();
        for(int i = 0; i < inventoryManger.OreList.Length; i++ ) 
        {
            GameObject oreUI = Instantiate(OriGirdUIPrefab, OreUIParent);
            oreUI.transform.SetParent(OreUIParent);
            OreUI oreUIComp = new OreUI
            {
                oreIcon = oreUI.transform.GetChild(1).GetComponent<Image>(),
                oreAmountText = oreUI.transform.GetChild(2).GetComponent<TextMeshProUGUI>()
            };
            oreUI.transform.GetChild(1).GetComponent<Image>().sprite = inventoryManger.orePools[i].icon;
            OreUIList.Add(oreUIComp);
        }
    }
    public void UpdateOreUI(OreType oreType, int amount)
    {
        OreUIList[(int)oreType].oreAmountText.text = "x"+amount.ToString();
    }
}
