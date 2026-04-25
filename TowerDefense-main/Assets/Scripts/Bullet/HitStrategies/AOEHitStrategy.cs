using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 范围伤害策略：命中时对周围范围内的所有目标造成伤害，处理完后回收子弹
/// </summary>
[CreateAssetMenu(fileName = "AOEHitStrategy", menuName = "TowerDefense/HitStrategies/AOE")]
public class AOEHitStrategy : HitStrategyBase
{
    [Header("AOE 设置")]
    [SerializeField] private float m_aoeRadius = 2f;
    [SerializeField] private LayerMask m_targetLayer = 0;
    [SerializeField] private bool m_enableVisualEffect = true;
    [SerializeField] private Color m_effectColor = new Color(1f, 0.5f, 0f, 0.8f);
    [SerializeField, Range(0f, 1f)] private float m_effectInitialAlpha = 0.8f;
    [SerializeField] private float m_effectFadeDuration = 0.5f;
    [SerializeField] private float m_effectScale = 1f;

    protected override void ProcessHit(Collider triggerCollider, BulletMain bullet)
    {
        // 使用命中的敌人（triggerCollider）作为 AOE 中心点
        Vector3 center = triggerCollider.transform.position;

        // 范围检测
        Collider[] hits = Physics.OverlapSphere(center, m_aoeRadius, m_targetLayer);
        HashSet<GameObject> processedTargets = new HashSet<GameObject>();
        int hitCount = 0;

        foreach (var col in hits)
        {
            if (col == null)
                continue;

            // 标签过滤
            if (CheckTag(col) == false)
                continue;

            GameObject target = col.gameObject;

            // 避免重复处理同一目标
            if (processedTargets.Contains(target))
                continue;

            // 验证目标有效性
            if (!IsValidTarget(target))
                continue;

            processedTargets.Add(target);

            // 发布命中事件（不标记回收）
            PublishHitEvent(target, bullet.AttackData, bullet, false);
            hitCount++;
        }

        // 生成视觉效果
        if (m_enableVisualEffect && hitCount > 0)
        {
            SpawnVisualEffect(center);
        }

        // AOE模式：处理完所有目标后请求回收子弹
        if (hitCount > 0)
        {
            // 使用是否存在第一个目标作为回收事件的目标参数
            GameObject firstTarget = processedTargets.Count > 0 ? GetFirstElement(processedTargets) : null;
            if (firstTarget != null)
            {
                PublishHitEvent(null, bullet.AttackData, bullet, true);
            }
        }
    }

    /// <summary>
    /// 获取HashSet的第一个元素
    /// </summary>
    private GameObject GetFirstElement(HashSet<GameObject> set)
    {
        foreach (var item in set)
        {
            return item;
        }
        return null;
    }

    /// <summary>
    /// 生成视觉效果
    /// </summary>
    private void SpawnVisualEffect(Vector3 position)
    {
        // 从 AssetManager 获取 AOEView 预制体
        GameObject prefab = AssetManager.Instance.GetAsset<GameObject>(CategoriesEnum.ViewPrefab, "AOEView");

        if (prefab == null)
        {
            Debug.LogWarning("[AOEHitStrategy] - 未找到 AOEView 预制体");
            return;
        }

        // 实例化视觉效果
        GameObject effectObj = Instantiate(prefab, position, Quaternion.identity);

        // 设置缩放（根据 AOE 半径和缩放倍数）
        float scale = m_aoeRadius * 2f * m_effectScale; // 直径 = 半径 * 2
        effectObj.transform.localScale = new Vector3(scale, scale, scale);

        // 获取或添加 FadeOut3D 组件
        FadeOut3D fadeOut = effectObj.GetComponent<FadeOut3D>();
        if (fadeOut == null)
        {
            fadeOut = effectObj.AddComponent<FadeOut3D>();
        }

        // 配置渐变效果
        fadeOut.SetColor(m_effectColor);
        fadeOut.SetInitialAlpha(m_effectInitialAlpha);
        fadeOut.SetFadeDuration(m_effectFadeDuration);
        fadeOut.StartFade();
    }
}
