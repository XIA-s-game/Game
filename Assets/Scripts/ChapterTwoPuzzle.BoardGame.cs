// Runs the dice board mini-game and moves the player between board tiles.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class ChapterTwoPuzzle
{
    private void StartBoardGame()
    {
        RefreshBoardReferences();
        boardRound = 1;
        boardPosition = 0;
        lastDiceRoll = 0;
        boardGamePhase = BoardGamePhase.WaitingToRoll;
        state = FlowState.BoardGame;
        ShowSystemPrompt(GetRollPrompt(), 3f);
    }

    private void UpdateBoardGameInput()
    {
        if (boardGamePhase == BoardGamePhase.WaitingToRoll && Input.GetKeyDown(interactKey))
        {
            StopBoardRoutine();
            boardRoutine = StartCoroutine(RollDiceAndMove());
        }
    }

    private IEnumerator RollDiceAndMove()
    {
        // Roll first, then trust the top face so the number matches what the player sees.
        boardGamePhase = BoardGamePhase.Rolling;
        int targetFace = Random.Range(1, 7);
        lastDiceRoll = 0;
        ShowSystemPrompt(rollingDicePrompt, 3f);

        yield return ThrowDice(targetFace);
        lastDiceRoll = GetFacePointingUp();
        ShowSystemPrompt(string.Format(rolledPromptFormat, lastDiceRoll), 3f);
        yield return new WaitForSeconds(0.35f);

        int target = Mathf.Min(boardPosition + lastDiceRoll, 20);
        boardGamePhase = BoardGamePhase.Moving;
        BeginBoardMove();
        yield return MoveAlongBoard(boardPosition, target);
        boardPosition = target;

        int adjusted = GetBoardAdjustment(boardPosition);
        if (adjusted != boardPosition)
        {
            string action = adjusted > boardPosition ? "forward" : "back";
            ShowSystemPrompt(string.Format(boardMovePromptFormat, action, adjusted), 3f);
            yield return new WaitForSeconds(0.6f);
            yield return MoveAlongBoard(boardPosition, adjusted);
            boardPosition = adjusted;
        }

        if (boardPosition >= 20)
        {
            yield return MovePlayerToTransform(endTile);
            EndBoardMove();
            CompleteBoardGame();
            yield break;
        }

        EndBoardMove();
        boardRound++;
        boardGamePhase = BoardGamePhase.WaitingToRoll;
        ShowSystemPrompt(GetRollPrompt(), 3f);
        boardRoutine = null;
    }

    private IEnumerator ThrowDice(int result)
    {
        if (dice == null)
        {
            yield return new WaitForSeconds(0.4f);
            yield break;
        }

        if (!diceOriginalTransformReady)
        {
            CacheDiceOriginalTransform();
        }

        Vector3 start = diceOriginalPosition;
        Quaternion finalRotation = GetDiceResultRotation(result);
        float elapsed = 0f;
        Vector3 drift = player != null ? player.forward * 0.8f : Vector3.forward * 0.8f;
        Vector3 spinAxis = new Vector3(0.73f, 1f, 0.41f).normalized;

        while (elapsed < diceThrowSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / diceThrowSeconds);
            float arc = Mathf.Sin(t * Mathf.PI) * diceThrowHeight;
            dice.position = Vector3.Lerp(start, start + drift, t) + Vector3.up * arc;

            Quaternion spin = Quaternion.AngleAxis(1440f * t, spinAxis) * Quaternion.AngleAxis(980f * t, Vector3.up);
            dice.rotation = t < 0.72f
                ? spin * diceOriginalRotation
                : Quaternion.Slerp(spin * diceOriginalRotation, finalRotation, Mathf.InverseLerp(0.72f, 1f, t));
            yield return null;
        }

        dice.position = start;
        dice.rotation = finalRotation;
    }

    private Quaternion GetDiceResultRotation(int result)
    {
        result = Mathf.Clamp(result, 1, 6);
        Vector3 localNormal = diceFaceLocalNormals[result].sqrMagnitude > 0.001f ? diceFaceLocalNormals[result] : Vector3.up;
        Quaternion faceUp = Quaternion.FromToRotation(localNormal, Vector3.up);
        return Quaternion.AngleAxis(Random.Range(0, 4) * 90f, Vector3.up) * faceUp;
    }

    private int GetFacePointingUp()
    {
        if (dice == null)
        {
            return Mathf.Clamp(lastDiceRoll, 1, 6);
        }

        int bestFace = 1;
        float bestDot = float.NegativeInfinity;
        for (int i = 1; i < diceFaces.Length; i++)
        {
            Vector3 localNormal = diceFaceLocalNormals[i].sqrMagnitude > 0.001f ? diceFaceLocalNormals[i] : Vector3.up;
            float dot = Vector3.Dot(dice.TransformDirection(localNormal), Vector3.up);
            if (dot > bestDot)
            {
                bestDot = dot;
                bestFace = i;
            }
        }

        return bestFace;
    }

    private IEnumerator MoveAlongBoard(int from, int to)
    {
        int index = from;
        while (index < to)
        {
            index++;
            yield return MovePlayerToBoardIndex(index);
        }

        while (index > to)
        {
            index--;
            yield return MovePlayerToBoardIndex(index);
        }
    }

    private IEnumerator MovePlayerToBoardIndex(int index)
    {
        if (index <= 0)
        {
            yield return MovePlayerToTransform(startTile);
            yield break;
        }

        index = Mathf.Clamp(index, 1, 20);
        yield return MovePlayerToTransform(boardTiles[index]);
    }

    private IEnumerator MovePlayerToTransform(Transform targetTransform)
    {
        if (player == null || targetTransform == null)
        {
            yield break;
        }

        Vector3 start = player.position;
        Vector3 target = targetTransform.position + Vector3.up * playerGroundOffset;
        target.y = start.y;
        float distance = Vector3.Distance(start, target);
        float duration = Mathf.Max(0.15f, distance / Mathf.Max(0.1f, boardMoveSpeed));
        float elapsed = 0f;
        Animator animator = player.GetComponentInChildren<Animator>();
        AquariusMax.Fae.demo.DemoCharacter.ForceWalkAnimation = true;
        SetBoardWalkAnimation(animator, true);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 direction = target - player.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion facing = Quaternion.LookRotation(direction.normalized, Vector3.up);
                player.rotation = Quaternion.RotateTowards(player.rotation, facing, 360f * Time.deltaTime);
            }

            player.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        player.position = target;
        AquariusMax.Fae.demo.DemoCharacter.ForceWalkAnimation = false;
        SetBoardWalkAnimation(animator, false);

    }

    private void BeginBoardMove()
    {
        if (player == null || boardMoveController != null)
        {
            return;
        }

        boardMoveController = player.GetComponent<CharacterController>();
        boardMoveControllerWasEnabled = boardMoveController != null && boardMoveController.enabled;
        if (boardMoveController != null)
        {
            boardMoveController.enabled = false;
        }
    }

    private void EndBoardMove()
    {
        if (boardMoveController != null)
        {
            boardMoveController.enabled = boardMoveControllerWasEnabled;
        }

        boardMoveController = null;
        boardMoveControllerWasEnabled = false;
        AquariusMax.Fae.demo.DemoCharacter.ForceWalkAnimation = false;
    }

    private void SetBoardWalkAnimation(Animator animator, bool walking)
    {
        if (animator == null)
        {
            return;
        }

        SetAnimatorBool(animator, "IsMoving", walking);
        SetAnimatorBool(animator, "IsRunning", false);
        SetAnimatorFloat(animator, "Speed", walking ? 0.5f : 0f);

        string stateName = walking ? "Walk" : "Idle";
        int fullPathHash = Animator.StringToHash("Base Layer." + stateName);
        int shortNameHash = Animator.StringToHash(stateName);
        if (animator.HasState(0, fullPathHash))
        {
            animator.CrossFade(fullPathHash, 0.08f, 0);
        }
        else if (animator.HasState(0, shortNameHash))
        {
            animator.CrossFade(shortNameHash, 0.08f, 0);
        }
    }

    private void SetAnimatorBool(Animator animator, string parameterName, bool value)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(parameterName, value);
                return;
            }
        }
    }

    private void SetAnimatorFloat(Animator animator, string parameterName, float value)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Float)
            {
                animator.SetFloat(parameterName, value);
                return;
            }
        }
    }

    private int GetBoardAdjustment(int position)
    {
        switch (position)
        {
            case 3:
                return 1;
            case 9:
                return 8;
            case 12:
                return 13;
            case 15:
                return 13;
            default:
                return position;
        }
    }

    private void CompleteBoardGame()
    {
        hasPass = true;
        boardGamePhase = BoardGamePhase.Won;
        state = FlowState.Exploring;
        AddInventoryItem(mazePassItemName);
        ShowSystemPrompt(boardWonPrompt, 3f);
    }

    private void StopBoardRoutine()
    {
        if (boardRoutine != null)
        {
            StopCoroutine(boardRoutine);
            boardRoutine = null;
        }

        EndBoardMove();
    }

    private string GetRollPrompt()
    {
        return string.Format(boardRollPromptFormat, boardRound);
    }
}
