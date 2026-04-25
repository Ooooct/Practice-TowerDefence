using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PurchaseAddAttackSpeed : MonoBehaviour
{
    Button m_button;
    int cost = 250;
    void Start()
    {
        m_button ??= GetComponent<Button>();
        m_button?.onClick.AddListener(OnPurchaseAddAttackSpeed);
    }
    public void OnPurchaseAddAttackSpeed()
    {
        if (CostManager.Instance.CanAfford(cost) == false) return;

        CostManager.Instance.TryCost(cost);

        var towers = UnitManager.Instance.GetUnits<TowerMain>();
        Buff buffAsset = AssetManager.Instance.GetAsset<Buff>(CategoriesEnum.Buff, "AddShootSpeed");
        foreach (var tower in towers)
        {
            if (tower.gameObject.name != "Default")
                tower.BuffManager.AddBuff(buffAsset); // 提升攻速
        }
    }
}
