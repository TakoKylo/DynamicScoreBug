using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CustomScoreboard.UI
{
    /// <summary>
    /// Partial class containing configuration and hotswap methods.
    /// Handles UI refresh, config application, logo hotswapping, and position updates.
    /// </summary>
    public partial class CustomBroadcastScoreboard
    {
        public void RefreshScoreboardUI()
        {
            try
            {
                // Remove old UI elements
                if (scoreboardContainer != null && scoreboardContainer.parent != null)
                {
                    scoreboardContainer.RemoveFromHierarchy();
                }
                if (leagueLogo != null && leagueLogo.parent != null)
                {
                    leagueLogo.RemoveFromHierarchy();
                }
                
                // Reset initialization
                isInitialized = false;
                // Shot tracking removed for simplicity
                lastBlueScore = 0;
                lastRedScore = 0;
                
                // Recreate the scoreboard
                CreateCustomScoreboard();

                // Handle showing/hiding based on config
                if (config != null && config.enableCustomScoreboard)
                {
                    HideOriginalScoreboard();
                    _originalScoreboardHidden = true;
                    // Show if not in main menu
                    if (!isInMainMenu)
                    {
                        TurnOn();
                    }
                    else
                    {
                        TurnOff();
                    }
                }
                else
                {
                    // Show original scoreboard
                    ShowOriginalScoreboard();
                    TurnOff();
                }
                
                isInitialized = true;
                
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error in RefreshScoreboardUI: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Lightweight config application - updates colors/logos without recreating entire UI.
        /// Use this when only visual settings change (colors, logos, minimap colors).
        /// </summary>
        public void ApplyConfigChanges(ScoreboardConfig newConfig)
        {
            try
            {
                // Update internal config reference directly
                config = newConfig;
                
                // Sync colors to ToasterReskinLoader
                Integration.ToasterReskinLoaderColorSync.SyncTeamColors(newConfig.blueTeamColorHex, newConfig.redTeamColorHex);
                
                // Sync team names to ToasterReskinLoader
                Integration.ToasterReskinLoaderColorSync.SyncTeamNames(newConfig.blueTeamName, newConfig.redTeamName);
                
                // Sync minimap colors to ToasterReskinLoader (falls back to team colors if empty)
                Integration.ToasterReskinLoaderColorSync.SyncMinimapColors(
                    newConfig.blueMinimapPlayerColorHex,
                    newConfig.redMinimapPlayerColorHex,
                    newConfig.blueMinimapNumberColorHex,
                    newConfig.redMinimapNumberColorHex,
                    newConfig.blueTeamColorHex,
                    newConfig.redTeamColorHex,
                    newConfig.blueTeamTextColorHex,
                    newConfig.redTeamTextColorHex
                );
                
                // Hotswap logos without recreating UI
                HotswapLogos(newConfig);

                // Handle enable/disable scoreboard
                if (config.enableCustomScoreboard)
                {
                    if (!_originalScoreboardHidden)
                    {
                        HideOriginalScoreboard();
                        _originalScoreboardHidden = true;
                    }
                    if (!isInMainMenu)
                    {
                        TurnOn();
                    }
                }
                else
                {
                    ShowOriginalScoreboard();
                    TurnOff();
                }
                
                DebugLog("[CustomScoreboard] Applied config changes (lightweight)");
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error in ApplyConfigChanges: {ex.Message}");
            }
        }
        
        public ScoreboardConfig GetConfig()
        {
            if (config == null)
            {
                config = LoadScoreboardConfig();
            }
            return config;
        }

        public void HotswapLogos(ScoreboardConfig liveConfig = null)
        {
            try
            {
                var configToUse = liveConfig ?? config;
                if (configToUse == null)
                {
                    configToUse = LoadScoreboardConfig();
                    config = configToUse;
                }

                // Reload logo textures
                Texture2D leagueLogoTexture = LoadLogoImage(configToUse.leagueLogoFile);
                Texture2D blueLogoTexture = LoadLogoImage(configToUse.blueTeamLogoFile);
                Texture2D redLogoTexture = LoadLogoImage(configToUse.redTeamLogoFile);

                // League logo: anchored to scoreboardContainer; recompute left from the
                // league logo section's center so it stays aligned when widths change.
                if (leagueLogo != null)
                {
                    leagueLogo.style.width = configToUse.leagueLogoWidth;
                    leagueLogo.style.height = configToUse.leagueLogoHeight;
                    float sectionCenterX = 280f + (configToUse.leagueLogoSectionWidth / 2f);
                    leagueLogo.style.left = sectionCenterX - (configToUse.leagueLogoWidth / 2f);
                    leagueLogo.style.top = (41f - configToUse.leagueLogoHeight) / 2f;

                    if (leagueLogoTexture != null)
                    {
                        leagueLogo.style.backgroundImage = new StyleBackground(leagueLogoTexture);
                        leagueLogo.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                        leagueLogo.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
                        leagueLogo.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
                        leagueLogo.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
                    }
                }

                // Update blue team logo
                if (blueTeamLogo != null)
                {
                    blueTeamLogo.style.width = configToUse.blueLogoWidth;
                    blueTeamLogo.style.height = configToUse.blueLogoHeight;
                    blueTeamLogo.style.left = configToUse.blueLogoOffsetX;
                    blueTeamLogo.style.top = configToUse.blueLogoOffsetY;
                    
                    if (blueLogoTexture != null)
                    {
                        blueTeamLogo.style.backgroundImage = new StyleBackground(blueLogoTexture);
                        blueTeamLogo.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                        blueTeamLogo.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
                        blueTeamLogo.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
                        blueTeamLogo.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
                    }
                }

                // Update red team logo
                if (redTeamLogo != null)
                {
                    redTeamLogo.style.width = configToUse.redLogoWidth;
                    redTeamLogo.style.height = configToUse.redLogoHeight;
                    redTeamLogo.style.left = configToUse.redLogoOffsetX;
                    redTeamLogo.style.top = configToUse.redLogoOffsetY;
                    
                    if (redLogoTexture != null)
                    {
                        redTeamLogo.style.backgroundImage = new StyleBackground(redLogoTexture);
                        redTeamLogo.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                        redTeamLogo.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
                        redTeamLogo.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
                        redTeamLogo.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
                    }
                }

                DebugLog("[CustomScoreboard] Logos hotswapped successfully");
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error in HotswapLogos: {ex.Message}");
            }
        }

        public void UpdatePositionAndScale(ScoreboardConfig liveConfig = null)
        {
            try
            {
                var configToUse = liveConfig ?? config;
                if (configToUse == null)
                {
                    configToUse = LoadScoreboardConfig();
                    config = configToUse;
                }

                // Update scoreboard container position and scale
                if (scoreboardContainer != null)
                {
                    scoreboardContainer.style.top = configToUse.scoreboardY;
                    scoreboardContainer.style.translate = new StyleTranslate(new Translate(new Length(configToUse.scoreboardX, LengthUnit.Pixel), new Length(0, LengthUnit.Pixel)));
                    scoreboardContainer.style.scale = new StyleScale(new Scale(new Vector2(configToUse.scoreboardScale, configToUse.scoreboardScale)));
                }

                // League logo is a child of scoreboardContainer; its position is anchored
                // to the league logo section and follows scoreboardContainer's scale
                // automatically. Left depends on leagueLogoSectionWidth so recompute it.
                if (leagueLogo != null)
                {
                    float sectionCenterX = 280f + (configToUse.leagueLogoSectionWidth / 2f);
                    leagueLogo.style.left = sectionCenterX - (configToUse.leagueLogoWidth / 2f);
                    leagueLogo.style.top = (41f - configToUse.leagueLogoHeight) / 2f;
                }
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error in UpdatePositionAndScale: {ex.Message}");
            }
        }

        /// <summary>
        /// Lightweight opacity update - reapplies opacity to all scoreboard colors without recreating UI.
        /// </summary>
        public void UpdateOpacity(ScoreboardConfig liveConfig = null)
        {
            try
            {
                var configToUse = liveConfig ?? config;
                if (configToUse == null)
                    return;

                Color blueTeamColor = GetBlueTeamColor();
                Color redTeamColor = GetRedTeamColor();

                // Update blue section with new opacity
                if (blueSection != null)
                {
                    Color blueColorWithOpacity = blueTeamColor;
                    blueColorWithOpacity.a = configToUse.scoreboardOpacity;
                    blueSection.style.backgroundColor = blueColorWithOpacity;

                    Color blueBorderColor = ParseHexColor(configToUse.blueBorderColorHex, new Color(0.12f, 0.23f, 0.54f, 1f));
                    blueBorderColor.a = configToUse.scoreboardOpacity;
                    blueSection.style.borderTopColor = blueBorderColor;
                    blueSection.style.borderBottomColor = blueBorderColor;
                    blueSection.style.borderLeftColor = blueBorderColor;
                    blueSection.style.borderRightColor = blueBorderColor;
                }

                // Update red section with new opacity
                if (redSection != null)
                {
                    Color redColorWithOpacity = redTeamColor;
                    redColorWithOpacity.a = configToUse.scoreboardOpacity;
                    redSection.style.backgroundColor = redColorWithOpacity;

                    Color redBorderColor = ParseHexColor(configToUse.redBorderColorHex, new Color(0.60f, 0.11f, 0.11f, 1f));
                    redBorderColor.a = configToUse.scoreboardOpacity;
                    redSection.style.borderTopColor = redBorderColor;
                    redSection.style.borderBottomColor = redBorderColor;
                    redSection.style.borderLeftColor = redBorderColor;
                    redSection.style.borderRightColor = redBorderColor;
                }

                DebugLog("[CustomScoreboard] Updated opacity");
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error in UpdateOpacity: {ex.Message}");
            }
        }
    }
}
