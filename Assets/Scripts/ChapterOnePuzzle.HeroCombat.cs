// Handles the helper hero combat scene in chapter one.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class ChapterOnePuzzle
{
    private void UpdateHeroCombat()
    {
        if (!heroCombatActive || hero == null)
        {
            return;
        }

        KeepHeroAtCombatHeight();

        if (heroTargetEnemy == null || !heroTargetEnemy.activeInHierarchy)
        {
            heroTargetEnemy = FindNextHeroTarget();
            if (heroTargetEnemy == null)
            {
                FinishHeroCombat();
                return;
            }

            PlayHeroAnimation(heroWalkController, heroWalkStateName);
        }

        if (heroAttacking)
        {
            FaceHeroToward(heroTargetEnemy.transform.position);
            if (Time.time >= heroAttackHitsAt)
            {
                DefeatEnemy(heroTargetEnemy);
                heroTargetEnemy = FindNextHeroTarget();
                heroAttacking = false;

                if (heroTargetEnemy == null)
                {
                    KeepHeroAtCombatHeight();
                    FinishHeroCombat();
                    return;
                }

                PlayHeroAnimation(heroWalkController, heroWalkStateName);
            }

            return;
        }

        Vector3 targetPosition = heroTargetEnemy.transform.position;
        Vector3 toTarget = targetPosition - hero.position;
        toTarget.y = 0f;

        if (toTarget.magnitude <= heroAttackDistance)
        {
            heroAttacking = true;
            heroAttackHitsAt = Time.time + heroAttackHitDelay;
            FaceHeroToward(targetPosition);
            PlayHeroAnimation(heroAttackController, heroAttackStateName);
            return;
        }

        FaceHeroToward(targetPosition);
        Vector3 moveTarget = new Vector3(targetPosition.x, heroCombatY, targetPosition.z);
        hero.position = Vector3.MoveTowards(hero.position, moveTarget, heroMoveSpeed * Time.deltaTime);
        KeepHeroAtCombatHeight();
    }

    private void KeepHeroAtCombatHeight()
    {
        Vector3 position = hero.position;
        if (Mathf.Abs(position.y - heroCombatY) <= 0.001f)
        {
            return;
        }

        position.y = heroCombatY;
        hero.position = position;
    }

    private GameObject FindNextHeroTarget()
    {
        GameObject bestEnemy = null;
        float bestSqrDistance = float.PositiveInfinity;
        Vector3 heroPosition = hero != null ? hero.position : Vector3.zero;

        for (int i = 0; i < delayedEnemies.Count; i++)
        {
            GameObject enemy = delayedEnemies[i];
            if (enemy == null || defeatedEnemies.Contains(enemy) || !enemy.activeInHierarchy)
            {
                continue;
            }

            float sqrDistance = (enemy.transform.position - heroPosition).sqrMagnitude;
            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                bestEnemy = enemy;
            }
        }

        return bestEnemy;
    }

    private void FaceHeroToward(Vector3 targetPosition)
    {
        Vector3 toTarget = targetPosition - hero.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        hero.rotation = Quaternion.RotateTowards(hero.rotation, targetRotation, heroTurnSpeed * Time.deltaTime);
    }

    private void DefeatEnemy(GameObject enemy)
    {
        if (enemy == null)
        {
            return;
        }

        defeatedEnemies.Add(enemy);
        RouteWaypointWalker walker = enemy.GetComponent<RouteWaypointWalker>();
        if (walker != null)
        {
            walker.enabled = false;
        }

        StopAudioSourcesInHierarchy(enemy.transform);
        enemy.SetActive(false);
    }

    private static void StopAudioSourcesInHierarchy(Transform root)
    {
        if (root == null)
        {
            return;
        }

        AudioSource[] audioSources = root.GetComponentsInChildren<AudioSource>(true);
        for (int i = 0; i < audioSources.Length; i++)
        {
            if (audioSources[i] != null)
            {
                audioSources[i].Stop();
            }
        }
    }

    private void PlayHeroAnimation(RuntimeAnimatorController controller, string stateName)
    {
        if (heroAnimator == null)
        {
            return;
        }

        if (controller != null)
        {
            heroAnimator.runtimeAnimatorController = controller;
        }

        if (string.IsNullOrEmpty(stateName))
        {
            return;
        }

        int fullStateHash = Animator.StringToHash("Base Layer." + stateName);
        int shortStateHash = Animator.StringToHash(stateName);
        if (heroAnimator.HasState(0, fullStateHash))
        {
            heroAnimator.CrossFade(fullStateHash, 0.08f);
        }
        else if (heroAnimator.HasState(0, shortStateHash))
        {
            heroAnimator.CrossFade(shortStateHash, 0.08f);
        }
    }
}
