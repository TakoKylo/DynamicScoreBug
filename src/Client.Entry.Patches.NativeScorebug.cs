using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UIElements;

public sealed partial class Scoreboard_ClientMod
{
    private void PatchNativeScorebugSuppression()
    {
        try
        {
            var uiGameStateType = typeof(UIGameState);
            var showMethod = AccessTools.Method(uiGameStateType, "Show", Type.EmptyTypes);
            if (showMethod != null)
            {
                _harmony.Patch(showMethod, prefix: new HarmonyMethod(typeof(Scoreboard_ClientMod), nameof(UIGameState_Show_Prefix)));
                Debug.Log("[Scoreboard] Patched UIGameState.Show for native scorebug suppression");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[Scoreboard] Native scorebug suppression patch failed: " + e);
        }
    }

    [HarmonyPrefix]
    private static bool UIGameState_Show_Prefix(UIGameState __instance)
    {
        try
        {
            if (!ShouldSuppressNativeScorebug())
            {
                return true;
            }

            var uiManager = MonoBehaviourSingleton<UIManager>.Instance;
            var root = uiManager?.RootVisualElement;
            if (root != null)
            {
                var gameStateContainer = root.Q<VisualElement>("GameStateContainer");
                if (gameStateContainer != null)
                {
                    gameStateContainer.style.display = UnityEngine.UIElements.DisplayStyle.None;
                    gameStateContainer.style.visibility = UnityEngine.UIElements.Visibility.Hidden;
                    gameStateContainer.style.opacity = 0f;
                }

                var uiScoreboard = root.Q<VisualElement>("UIScoreboard");
                if (uiScoreboard != null)
                {
                    uiScoreboard.style.display = UnityEngine.UIElements.DisplayStyle.None;
                    uiScoreboard.style.visibility = UnityEngine.UIElements.Visibility.Hidden;
                    uiScoreboard.style.opacity = 0f;
                }
            }

            return false;
        }
        catch
        {
            return true;
        }
    }

    private static bool ShouldSuppressNativeScorebug()
    {
        if (!IsModEnabled)
        {
            return false;
        }

        var scoreboard = _staticHost?.GetComponent<CustomScoreboard.UI.CustomBroadcastScoreboard>();
        if (scoreboard == null)
        {
            return false;
        }

        var config = scoreboard.GetConfig();
        return config != null && config.enableCustomScoreboard;
    }
}
