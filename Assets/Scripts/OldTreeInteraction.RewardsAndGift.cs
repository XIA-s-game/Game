// Handles the old tree reward choices and the mushroom gift pickup.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class OldTreeInteraction
{
    private void ReadRewardChoiceKeys()
    {
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.Alpha1))
        {
            ChooseReward(rewardChoiceA);
        }
        else if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.Alpha2))
        {
            ChooseReward(rewardChoiceB);
        }
        else if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Alpha3))
        {
            ChooseReward(rewardChoiceC);
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.Alpha4))
        {
            ChooseReward(rewardChoiceD);
        }
    }

    private void ChooseReward(string choice)
    {
        UnlockPlayerForEggChallenge();
        branchFlowActive = false;

        if (choice == rewardChoiceA)
        {
            StartDialogue(new[]
            {
                "Old Tree: You want to take the egg?",
                "Old Tree: That is not your choice to make.",
                "Old Tree: The forest has its own rules.",
                "Old Tree: Good intentions can still cause harm.",
                "Old Tree: Watch first, then act.",
                "Old Tree: You are not ready for this lesson.",
                "Old Tree: Leave the nest alone.",
                "Old Tree: Come back when you understand patience."
            }, CloseDialogueAndReset);
        }
        else if (choice == rewardChoiceB)
        {
            StartDialogue(new[]
            {
                "Old Tree: Destroy it?",
                "Old Tree: Magic is not for removing things you dislike.",
                "Old Tree: A mage must understand balance.",
                "Old Tree: Both lives belong to the forest.",
                "Old Tree: Deciding who should live is not wisdom.",
                "Old Tree: That kind of certainty is dangerous.",
                "Old Tree: I will not help you with that.",
                "Old Tree: Step away from the nest.",
                "Old Tree: Think before you judge."
            }, CloseDialogueAndReset);
        }
        else if (choice == rewardChoiceC)
        {
            StartDialogue(new[]
            {
                "Old Tree: Good. You chose restraint.",
                "Old Tree: Many young mages rush to interfere.",
                "Old Tree: The forest does not always need rescue.",
                "Old Tree: It needs understanding.",
                "Old Tree: You kept your hands still.",
                "Old Tree: That deserves a small gift."
            }, StartMushroomGift);
        }
        else
        {
            StartDialogue(new[]
            {
                "Old Tree: Moving it sounds kind, but it still changes the nest.",
                "Old Tree: Help should solve the problem, not create a new one.",
                "Old Tree: If you want to help, build a safe shelter nearby.",
                "Old Tree: Use wisdom, not force.",
                "Old Tree: That is the lesson."
            }, CloseDialogueAndReset);
        }
    }

    private void StartMushroomGift()
    {
        currentAnswer = null;
        state = DialogueState.MushroomGift;
        LockPlayerForEggChallenge();
        PrepareMushroomGift();
    }

    private void PrepareMushroomGift()
    {
        if (mushroomGift == null)
        {
            mushroomGift = FindSceneTransform(mushroomGiftName);
        }

        if (mushroomGift == null)
        {
            PickUpMushroomGift();
            return;
        }

        FindPlayerCamera();
        UpdateMushroomTargetPosition();
        EnableMushroomGlow();

        Vector3 outward = mushroomGift.position - transform.position;
        outward.y = 0f;
        if (outward.sqrMagnitude < 0.01f && player != null)
        {
            outward = player.position - transform.position;
            outward.y = 0f;
        }

        if (outward.sqrMagnitude < 0.01f)
        {
            outward = transform.forward;
        }

        outward.Normalize();
        mushroomTreeExitPosition = mushroomGift.position + outward * mushroomMoveOutDistance;
        mushroomTreeExitPosition.y = mushroomGift.position.y + mushroomMoveOutHeight;
        mushroomGiftStartTime = Time.time;
        mushroomReachedTreeExit = false;
    }

    private void UpdateMushroomGift()
    {
        if (mushroomGift == null)
        {
            mushroomGift = FindSceneTransform(mushroomGiftName);
            if (mushroomGift == null)
            {
                PickUpMushroomGift();
                return;
            }
        }

        UpdateMushroomTargetPosition();

        if (!mushroomReachedTreeExit)
        {
            mushroomGift.position = Vector3.MoveTowards(
                mushroomGift.position,
                mushroomTreeExitPosition,
                mushroomMoveSpeed * Time.deltaTime);

            if (Vector3.Distance(mushroomGift.position, mushroomTreeExitPosition) <= 0.03f)
            {
                mushroomReachedTreeExit = true;
                mushroomGiftStartTime = Time.time;
            }
        }
        else
        {
            mushroomGift.position = Vector3.MoveTowards(
                mushroomGift.position,
                mushroomTargetPosition,
                mushroomMoveSpeed * Time.deltaTime);

            if (Vector3.Distance(mushroomGift.position, mushroomTargetPosition) <= 0.03f)
            {
                float bob = Mathf.Sin((Time.time - mushroomGiftStartTime) * mushroomBobSpeed) * mushroomBobAmount;
                mushroomGift.position = mushroomTargetPosition + Vector3.up * bob;
            }
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            PickUpMushroomGift();
        }
    }

    private void UpdateMushroomTargetPosition()
    {
        Transform targetTransform = player != null ? player : playerCameraTransform;
        Vector3 basePosition = targetTransform != null ? targetTransform.position : transform.position + Vector3.up * 2f;
        Vector3 forward = targetTransform != null ? targetTransform.forward : transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.01f && playerCameraTransform != null)
        {
            forward = playerCameraTransform.forward;
            forward.y = 0f;
        }

        if (forward.sqrMagnitude < 0.01f)
        {
            forward = transform.forward;
            forward.y = 0f;
        }

        forward.Normalize();
        mushroomTargetPosition = basePosition + forward * mushroomFrontDistance + Vector3.up * mushroomFrontHeight;
    }

    private void PickUpMushroomGift()
    {
        DisableMushroomGlow();
        UnlockPlayerForEggChallenge();
        StartDialogue(new[]
        {
            "Old Tree: This is a magic mushroom.",
            "Old Tree: It reminds careful mages to observe before acting.",
            "Old Tree: Keep that lesson with you."
        }, CloseDialogueAndReset);
    }

    private void EnableMushroomGlow()
    {
        DisableMushroomGlow();

        if (mushroomGift == null)
        {
            return;
        }

        Renderer[] renderers = mushroomGift.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            Material material = renderer.material;
            mushroomGlowRenderers.Add(renderer);
            mushroomOriginalColors.Add(material.HasProperty("_Color") ? material.color : Color.white);
            mushroomOriginalEmissionColors.Add(material.HasProperty("_EmissionColor") ? material.GetColor("_EmissionColor") : Color.black);

            if (material.HasProperty("_Color"))
            {
                material.color = mushroomGlowColor;
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", mushroomGlowColor * mushroomGlowIntensity);
            }
        }

        GameObject lightObject = new GameObject("Mu_Gift_Light");
        lightObject.transform.SetParent(mushroomGift, false);
        lightObject.transform.localPosition = Vector3.up * 1.2f;
        mushroomGiftLight = lightObject.AddComponent<Light>();
        mushroomGiftLight.type = LightType.Point;
        mushroomGiftLight.color = mushroomGlowColor;
        mushroomGiftLight.intensity = mushroomGlowIntensity;
        mushroomGiftLight.range = 6f;
    }

    private void DisableMushroomGlow()
    {
        for (int i = 0; i < mushroomGlowRenderers.Count; i++)
        {
            Renderer renderer = mushroomGlowRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material material = renderer.material;
            if (material.HasProperty("_Color") && i < mushroomOriginalColors.Count)
            {
                material.color = mushroomOriginalColors[i];
            }

            if (material.HasProperty("_EmissionColor") && i < mushroomOriginalEmissionColors.Count)
            {
                material.SetColor("_EmissionColor", mushroomOriginalEmissionColors[i]);
            }
        }

        mushroomGlowRenderers.Clear();
        mushroomOriginalColors.Clear();
        mushroomOriginalEmissionColors.Clear();

        if (mushroomGiftLight != null)
        {
            Destroy(mushroomGiftLight.gameObject);
            mushroomGiftLight = null;
        }
    }
}
