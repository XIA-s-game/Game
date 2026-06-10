using System.Collections.Generic;
using UnityEngine;

public partial class OldTreeInteraction
{
    private void CollectFenceTargets()
    {
        fenceCollectibles.Clear();
        if (fenceCollectibleTargets == null)
        {
            return;
        }

        for (int i = 0; i < fenceCollectibleTargets.Length; i++)
        {
            Transform candidate = fenceCollectibleTargets[i];
            if (candidate != null && candidate != fenceBuildTarget && !fenceCollectibles.Contains(candidate))
            {
                fenceCollectibles.Add(candidate);
            }
        }
    }

    private void UpdateSideQuestCollection()
    {
        if (player == null)
        {
            ClearFenceHighlight();
            return;
        }

        if (collectedFenceCount >= requiredFenceCount)
        {
            UpdateFenceBuildTarget();
            return;
        }

        Transform closest = FindClosestFenceCollectible();
        if (closest != nearbyFence)
        {
            ClearFenceHighlight();
            nearbyFence = closest;
            HighlightFence(nearbyFence);
        }

        if (nearbyFence != null && Input.GetKeyDown(interactKey))
        {
            PickupNearbyFence();
        }
    }

    private void UpdateFenceBuildTarget()
    {
        if (fenceBuilt)
        {
            return;
        }

        if (fenceBuildTarget == null)
        {
            ClearFenceHighlight();
            nearbyFenceBuildTarget = false;
            return;
        }

        if (!fenceBuildTargetShown)
        {
            fenceBuildTarget.gameObject.SetActive(true);
            fenceBuildTargetShown = true;
            ApplyFenceBuildGhost();
        }

        bool isNearBuildTarget = IsWithinDistance(fenceBuildTarget.position, fenceBuildDistance);
        if (isNearBuildTarget != nearbyFenceBuildTarget)
        {
            ClearFenceHighlight();
            if (isNearBuildTarget)
            {
                HighlightFence(fenceBuildTarget);
            }
        }

        nearbyFenceBuildTarget = isNearBuildTarget;

        if (nearbyFenceBuildTarget && Input.GetKeyDown(interactKey))
        {
            fenceBuilt = true;
            GlobalBackpackUI.RemoveAll(fenceInventoryName);
            ClearFenceHighlight();
            RestoreFenceBuildSolid();
            nearbyFenceBuildTarget = false;
        }
    }

    private bool IsSideQuestInProgress()
    {
        return sideQuestActive && (!fenceBuilt || collectedSaplingCount < requiredSaplingCount || plantedSaplingCount < saplingPlantTargets.Count);
    }

    private void PrepareSaplingPlantTargets()
    {
        saplingPlantTargets.Clear();
        if (saplingPlantTargetRefs == null)
        {
            return;
        }

        for (int i = 0; i < saplingPlantTargetRefs.Length; i++)
        {
            Transform target = saplingPlantTargetRefs[i];
            if (target == null)
            {
                continue;
            }

            target.gameObject.SetActive(false);
            saplingPlantTargets.Add(target);
        }
    }

    private void ShowSaplingPlantTargets()
    {
        if (saplingPlantTargets.Count == 0)
        {
            PrepareSaplingPlantTargets();
        }

        if (saplingPlantTargetsShown)
        {
            return;
        }

        for (int i = 0; i < saplingPlantTargets.Count; i++)
        {
            Transform target = saplingPlantTargets[i];
            if (target == null)
            {
                continue;
            }

            target.gameObject.SetActive(true);
            ApplySaplingGhost(target);
        }

        saplingPlantTargetsShown = true;
    }

    private void UpdateSaplingPlanting()
    {
        if (collectedSaplingCount < requiredSaplingCount)
        {
            return;
        }

        ShowSaplingPlantTargets();

        Transform closest = FindClosestSaplingPlantTarget();
        if (closest != nearbySaplingPlantTarget)
        {
            ClearFenceHighlight();
            nearbySaplingPlantTarget = closest;
            if (nearbySaplingPlantTarget != null)
            {
                HighlightFence(nearbySaplingPlantTarget);
            }
        }

        if (nearbySaplingPlantTarget != null && Input.GetKeyDown(interactKey))
        {
            Transform planted = nearbySaplingPlantTarget;
            ClearFenceHighlight();
            RestoreSaplingSolid(planted);
            nearbySaplingPlantTarget = null;
            plantedSaplingCount++;
            GlobalBackpackUI.SetItemCount(saplingInventoryName, Mathf.Max(0, collectedSaplingCount - plantedSaplingCount));
        }
    }

    private void ApplySaplingGhost(Transform target)
    {
        Renderer[] renderers = GetCachedRenderers(target);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material material = renderer.material;
            if (!material.HasProperty("_Color"))
            {
                continue;
            }

            if (!saplingGhostRenderers.Contains(renderer))
            {
                saplingGhostRenderers.Add(renderer);
                saplingGhostOriginalColors.Add(material.color);
            }

            Color ghostColor = material.color;
            ghostColor.a = 0.32f;
            material.color = ghostColor;
            SetMaterialTransparent(material, true);
        }
    }

    private bool IsSaplingGhost(Transform target)
    {
        Renderer[] renderers = GetCachedRenderers(target);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (saplingGhostRenderers.Contains(renderers[i]))
            {
                return true;
            }
        }

        return false;
    }

    private void RestoreSaplingSolid(Transform target)
    {
        Renderer[] renderers = GetCachedRenderers(target);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            int index = saplingGhostRenderers.IndexOf(renderer);
            if (index < 0)
            {
                continue;
            }

            Material material = renderer.material;
            if (material.HasProperty("_Color") && index < saplingGhostOriginalColors.Count)
            {
                material.color = saplingGhostOriginalColors[index];
            }

            SetMaterialTransparent(material, false);
            saplingGhostRenderers.RemoveAt(index);
            saplingGhostOriginalColors.RemoveAt(index);
        }
    }

    private void ApplyFenceBuildGhost()
    {
        fenceBuildRenderers.Clear();
        fenceBuildOriginalColors.Clear();

        if (fenceBuildTarget == null)
        {
            return;
        }

        Renderer[] renderers = GetCachedRenderers(fenceBuildTarget);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material material = renderer.material;
            if (!material.HasProperty("_Color"))
            {
                continue;
            }

            fenceBuildRenderers.Add(renderer);
            fenceBuildOriginalColors.Add(material.color);

            Color ghostColor = material.color;
            ghostColor.a = 0.32f;
            material.color = ghostColor;
            SetMaterialTransparent(material, true);
        }
    }

    private void RestoreFenceBuildSolid()
    {
        for (int i = 0; i < fenceBuildRenderers.Count; i++)
        {
            Renderer renderer = fenceBuildRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material material = renderer.material;
            if (material.HasProperty("_Color") && i < fenceBuildOriginalColors.Count)
            {
                material.color = fenceBuildOriginalColors[i];
            }

            SetMaterialTransparent(material, false);
        }

        fenceBuildRenderers.Clear();
        fenceBuildOriginalColors.Clear();
    }

    private static void SetMaterialTransparent(Material material, bool transparent)
    {
        if (material == null || !material.HasProperty("_Mode"))
        {
            return;
        }

        if (transparent)
        {
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }
        else
        {
            material.SetFloat("_Mode", 0f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = -1;
        }
    }

    private void PickupNearbyFence()
    {
        if (nearbyFence == null)
        {
            return;
        }

        Transform pickedFence = nearbyFence;
        ClearFenceHighlight();
        pickedFence.gameObject.SetActive(false);
        collectedFenceCount = Mathf.Min(collectedFenceCount + 1, requiredFenceCount);
        GlobalBackpackUI.SetItemCount(fenceInventoryName, collectedFenceCount);
        nearbyFence = null;
    }

    private void HighlightFence(Transform target)
    {
        if (target == null)
        {
            return;
        }

        Renderer[] renderers = GetCachedRenderers(target);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            Material material = renderer.material;
            highlightedFenceRenderers.Add(renderer);
            highlightedFenceOriginalColors.Add(material.HasProperty("_Color") ? material.color : Color.white);
            highlightedFenceOriginalEmissionColors.Add(material.HasProperty("_EmissionColor") ? material.GetColor("_EmissionColor") : Color.black);
            highlightedFenceHadEmissionEnabled.Add(material.IsKeywordEnabled("_EMISSION"));

            if (material.HasProperty("_Color"))
            {
                material.color = fenceHighlightColor;
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", fenceHighlightColor * 1.5f);
            }
        }
    }

    private void ClearFenceHighlight()
    {
        for (int i = 0; i < highlightedFenceRenderers.Count; i++)
        {
            Renderer renderer = highlightedFenceRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material material = renderer.material;
            if (material.HasProperty("_Color") && i < highlightedFenceOriginalColors.Count)
            {
                material.color = highlightedFenceOriginalColors[i];
            }

            if (material.HasProperty("_EmissionColor") && i < highlightedFenceOriginalEmissionColors.Count)
            {
                material.SetColor("_EmissionColor", highlightedFenceOriginalEmissionColors[i]);
            }

            bool hadEmission = i < highlightedFenceHadEmissionEnabled.Count && highlightedFenceHadEmissionEnabled[i];
            if (hadEmission)
            {
                material.EnableKeyword("_EMISSION");
            }
            else
            {
                material.DisableKeyword("_EMISSION");
            }
        }

        highlightedFenceRenderers.Clear();
        highlightedFenceOriginalColors.Clear();
        highlightedFenceOriginalEmissionColors.Clear();
        highlightedFenceHadEmissionEnabled.Clear();
        nearbyFence = null;
    }

    private Transform FindClosestFenceCollectible()
    {
        Transform closest = null;
        float closestDistance = fencePickupDistance * fencePickupDistance;

        for (int i = 0; i < fenceCollectibles.Count; i++)
        {
            Transform candidate = fenceCollectibles[i];
            if (candidate == null || !candidate.gameObject.activeInHierarchy)
            {
                continue;
            }

            float distance = Vector3.SqrMagnitude(candidate.position - player.position);
            if (distance <= closestDistance)
            {
                closest = candidate;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private Transform FindClosestSaplingPlantTarget()
    {
        Transform closest = null;
        float closestDistance = saplingPlantDistance * saplingPlantDistance;

        for (int i = 0; i < saplingPlantTargets.Count; i++)
        {
            Transform target = saplingPlantTargets[i];
            if (target == null || !target.gameObject.activeInHierarchy || !IsSaplingGhost(target))
            {
                continue;
            }

            float distance = Vector3.SqrMagnitude(target.position - player.position);
            if (distance <= closestDistance)
            {
                closest = target;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private bool IsWithinDistance(Vector3 targetPosition, float distance)
    {
        return Vector3.SqrMagnitude(targetPosition - player.position) <= distance * distance;
    }

    private Renderer[] GetCachedRenderers(Transform target)
    {
        if (!treeRendererCache.TryGetValue(target, out Renderer[] renderers))
        {
            renderers = target.GetComponentsInChildren<Renderer>();
            treeRendererCache[target] = renderers;
        }

        return renderers;
    }
}
