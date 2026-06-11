// Runs the fence and sapling build side quest after the fairy backstory opens.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class OldTreeInteraction
{
    private void CollectFenceTargets()
    {
        fenceCollectibles.Clear();

        Transform[] allTransforms = FindObjectsOfType<Transform>();
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform candidate = allTransforms[i];
            if (candidate == null || !candidate.gameObject.activeInHierarchy)
            {
                continue;
            }

            string normalizedName = NormalizeName(candidate.name);
            if (fenceBuildTarget != null && candidate == fenceBuildTarget)
            {
                continue;
            }

            if (NamesMatch(candidate.name, fenceBuildTargetName))
            {
                continue;
            }

            if (!normalizedName.StartsWith("fence"))
            {
                continue;
            }

            if (candidate.parent != null && NormalizeName(candidate.parent.name).StartsWith("fence"))
            {
                continue;
            }

            fenceCollectibles.Add(candidate);
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
            fenceBuildTarget = FindSceneTransform(fenceBuildTargetName);
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

        bool isNearBuildTarget = Vector3.SqrMagnitude(fenceBuildTarget.position - player.position) <= fenceBuildDistance * fenceBuildDistance;
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
        for (int i = 0; i < saplingPlantTargetNames.Length; i++)
        {
            Transform target = FindSceneTransform(saplingPlantTargetNames[i]);
            if (target == null && saplingPreviewPrefab != null)
            {
                Vector3 spawnPosition = GetSaplingPlantPosition(i);
                GameObject spawned = Instantiate(saplingPreviewPrefab, spawnPosition, Quaternion.identity);
                spawned.name = saplingPlantTargetNames[i];
                target = spawned.transform;
            }

            if (target == null)
            {
                continue;
            }

            target.gameObject.SetActive(false);
            saplingPlantTargets.Add(target);
        }
    }

    private Vector3 GetSaplingPlantPosition(int index)
    {
        Vector3 basePosition = fenceBuildTarget != null ? fenceBuildTarget.position : transform.position;
        Vector3 offset = index < saplingPlantOffsets.Length ? saplingPlantOffsets[index] : Vector3.zero;
        return basePosition + offset;
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
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
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
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
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
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
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

        Renderer[] renderers = fenceBuildTarget.GetComponentsInChildren<Renderer>();
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

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
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
}
