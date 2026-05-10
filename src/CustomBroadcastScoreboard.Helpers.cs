using System;
using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;

namespace CustomScoreboard.UI
{
    public partial class CustomBroadcastScoreboard
    {
        // ========== HELPER METHODS ==========
        
        /// <summary>Helper to get blue team color with config fallback</summary>
        private Color GetBlueTeamColor()
        {
            return ParseHexColor(config.blueTeamColorHex, new Color(0.15f, 0.35f, 0.75f, 1f));
        }
        
        /// <summary>Helper to get red team color with config fallback</summary>
        private Color GetRedTeamColor()
        {
            return ParseHexColor(config.redTeamColorHex, new Color(0.85f, 0.15f, 0.15f, 1f));
        }
        
        /// <summary>Helper to get blue team text color with config fallback</summary>
        private Color GetBlueTeamTextColor()
        {
            return ParseHexColor(config.blueTeamTextColorHex, Color.white);
        }
        
        /// <summary>Helper to get red team text color with config fallback</summary>
        private Color GetRedTeamTextColor()
        {
            return ParseHexColor(config.redTeamTextColorHex, Color.white);
        }
        
        /// <summary>Helper to get team color based on PlayerTeam</summary>
        private Color GetTeamColor(PlayerTeam team)
        {
            return team == PlayerTeam.Blue ? GetBlueTeamColor() : GetRedTeamColor();
        }
        
        /// <summary>Helper to get team text color based on PlayerTeam</summary>
        private Color GetTeamTextColor(PlayerTeam team)
        {
            return team == PlayerTeam.Blue ? GetBlueTeamTextColor() : GetRedTeamTextColor();
        }
        
        /// <summary>Helper to get team name from config</summary>
        private string GetTeamName(PlayerTeam team)
        {
            return team == PlayerTeam.Blue ? config.blueTeamName : config.redTeamName;
        }
        
        /// <summary>Helper to get team logo texture</summary>
        private Texture2D GetTeamLogoTexture(PlayerTeam team)
        {
            return team == PlayerTeam.Blue ? blueTeamLogoTexture : redTeamLogoTexture;
        }
        
        /// <summary>Helper to safely get UIManager instance</summary>
        private UIManager GetUIManager()
        {
            return MonoBehaviourSingleton<UIManager>.Instance;
        }
        
        /// <summary>Helper to safely get PlayerManager instance</summary>
        private PlayerManager GetPlayerManager()
        {
            return MonoBehaviourSingleton<PlayerManager>.Instance;
        }
        
        /// <summary>Helper to safely get RootVisualElement from UIManager</summary>
        private VisualElement GetRootVisualElement()
        {
            var uiManager = GetUIManager();
            return uiManager?.RootVisualElement;
        }
        
        /// <summary>Helper to get a private static field via reflection</summary>
        private System.Reflection.FieldInfo GetPrivateStaticField(System.Type type, string fieldName)
        {
            return type?.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        }
        
        /// <summary>Helper to get a private instance field via reflection</summary>
        private System.Reflection.FieldInfo GetPrivateInstanceField(System.Type type, string fieldName)
        {
            return type?.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        }
        
        /// <summary>Helper to set horizontal position with centering translate</summary>
        private void SetCenteredHorizontalPosition(VisualElement element, float xOffset)
        {
            element.style.left = new StyleLength(new Length(50, LengthUnit.Percent));
            element.style.translate = new StyleTranslate(new Translate(
                new Length(xOffset, LengthUnit.Pixel), 
                new Length(0, LengthUnit.Pixel)
            ));
        }
        
        /// <summary>Helper to create a slide animation with DOTween</summary>
        private Tween CreateSlideAnimation(VisualElement element, float startY, float endY, float duration)
        {
            return DOTween.To(() => startY, 
                y => element.style.top = y, endY, duration);
        }
        
        /// <summary>Helper to set team logo background with proper scaling</summary>
        private void SetTeamLogoBackground(VisualElement element, Texture2D logo, Color tintColor)
        {
            if (logo != null)
            {
                element.style.backgroundImage = new StyleBackground(logo);
                #pragma warning disable CS0618
                element.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                #pragma warning restore CS0618
                element.style.unityBackgroundImageTintColor = tintColor;
            }
        }
    }
}
