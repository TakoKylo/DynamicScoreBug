using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;

namespace CustomScoreboard.UI
{
    public partial class CustomBroadcastScoreboard
    {
        // ============================================
        // WIN ANIMATION METHODS
        // ============================================
        
        private void ShowWinAnimation(PlayerTeam winningTeam, bool isShutout, string customSuffix = null, bool temporary = false)
        {
            if (winOverlay == null || config == null || !config.enableAnimations) return;
            
            // Kill any existing animations
            DOTween.Kill(winOverlay);
            DOTween.Kill(winOverlayLabel);
            
            // Set the overlay color based on winning team from config colors (fully opaque)
            Color winColor = GetTeamColor(winningTeam);
            winColor.a = 1f; // Ensure fully opaque
            
            winOverlay.style.backgroundColor = winColor;
            
            // Add team logo behind text with translucent team color tint
            VisualElement logoBackground = winOverlay.Q<VisualElement>("animationLogoBackground");
            if (logoBackground == null && config.showAnimationLogo)
            {
                logoBackground = new VisualElement();
                logoBackground.name = "animationLogoBackground";
                logoBackground.style.position = Position.Absolute;
                logoBackground.style.width = config.animationLogoWidth;
                logoBackground.style.height = config.animationLogoHeight;
                logoBackground.style.alignItems = Align.Center;
                logoBackground.style.justifyContent = Justify.Center;
                
                Texture2D teamLogo = GetTeamLogoTexture(winningTeam);
                if (teamLogo != null)
                {
                    SetTeamLogoBackground(logoBackground, teamLogo, winColor);
                    Color tintColor = winColor;
                    tintColor.a = config.animationLogoOpacity;
                    logoBackground.style.unityBackgroundImageTintColor = tintColor;
                }
                
                winOverlay.Insert(0, logoBackground); // Add behind text
            }
            else if (logoBackground != null && config.showAnimationLogo)
            {
                // Update existing logo
                Texture2D teamLogo = GetTeamLogoTexture(winningTeam);
                if (teamLogo != null)
                {
                    SetTeamLogoBackground(logoBackground, teamLogo, winColor);
                }
                // Update size
                logoBackground.style.width = config.animationLogoWidth;
                logoBackground.style.height = config.animationLogoHeight;
                Color tintColor = winColor;
                tintColor.a = config.animationLogoOpacity;
                logoBackground.style.unityBackgroundImageTintColor = tintColor;
                logoBackground.style.display = DisplayStyle.Flex;
            }
            else if (logoBackground != null && !config.showAnimationLogo)
            {
                logoBackground.style.display = DisplayStyle.None;
            }
            
            // Get team name from config and trim spaces for centered display
            string teamName = GetTeamName(winningTeam).Trim();
            
            // Set text color from config
            Color textColor = GetTeamTextColor(winningTeam);
            winOverlayLabel.style.color = textColor;
            
            // Set text based on custom suffix or shutout/normal win
            if (customSuffix != null)
            {
                winOverlayLabel.text = $"{teamName}{customSuffix}";
            }
            else
            {
                winOverlayLabel.text = isShutout ? $"{teamName} SHUTOUT WIN!" : $"{teamName} WINS!";
            }
            
            // Start with width 0 for wipe effect (overlay stays in place).
            // Reset translate in case a previous animation was killed mid-flight.
            winOverlay.style.width = 0;
            winOverlay.style.translate = new StyleTranslate(new Translate(
                new Length(0, LengthUnit.Pixel), new Length(0, LengthUnit.Pixel)));
            winOverlay.style.display = DisplayStyle.Flex;
            
            // Reset text position and scale
            winOverlayLabel.style.translate = new StyleTranslate(new Translate(Length.Percent(0), Length.Percent(0)));
            winOverlayLabel.style.scale = new StyleScale(new Scale(Vector2.one));
            
            // Animate league logo shrink and disappear
            if (leagueLogo != null)
            {
                DOTween.Sequence()
                    .SetTarget(leagueLogo)
                    .Append(DOTween.To(() => 1f, s => {
                        leagueLogo.style.scale = new StyleScale(new Scale(new Vector2(s, s)));
                        leagueLogo.style.opacity = s;
                    }, 0f, 0.3f).SetEase(Ease.InBack))
                    .OnComplete(() => leagueLogo.style.display = DisplayStyle.None);
            }
            
            float targetWidth = ScorebugAnchor.GoalOverlayWidth;
            float holdDuration = temporary ? 3.0f : 4.5f; // 2x or 3x the goal hold time
            float rockAngle = 3f; // Rotation angle in degrees
            
            // All wins are now temporary with rocking animations
            var mainSequence = DOTween.Sequence().SetTarget(winOverlay);
            
            // Wipe in from left to right
            mainSequence.Append(DOTween.To(() => 0f, w => {
                winOverlay.style.width = w;
            }, targetWidth, 0.6f).SetEase(Ease.OutCubic));
            
            // Pulse scale AND rotation together
            mainSequence.Append(DOTween.To(() => 1f, s => {
                winOverlayLabel.style.scale = new StyleScale(new Scale(new Vector2(s, s)));
            }, 1.3f, 0.4f).SetEase(Ease.OutBack));
            
            // Add rotation during pulse (runs at same time as scale down)
            mainSequence.Join(DOTween.To(() => 0f, angle => {
                winOverlayLabel.style.rotate = new StyleRotate(new Rotate(angle));
            }, rockAngle * 0.7f, 0.4f).SetEase(Ease.OutBack));
            
            mainSequence.Append(DOTween.To(() => 1.3f, s => {
                winOverlayLabel.style.scale = new StyleScale(new Scale(new Vector2(s, s)));
            }, 1.0f, 0.3f));
            
            mainSequence.Join(DOTween.To(() => rockAngle * 0.7f, angle => {
                winOverlayLabel.style.rotate = new StyleRotate(new Rotate(angle));
            }, 0f, 0.3f));
            
            // Rocking rotation animation during hold (back and forth)
            mainSequence.Append(DOTween.To(() => 0f, angle => {
                winOverlayLabel.style.rotate = new StyleRotate(new Rotate(angle));
            }, rockAngle, holdDuration * 0.25f).SetEase(Ease.InOutSine))
            .Append(DOTween.To(() => rockAngle, angle => {
                winOverlayLabel.style.rotate = new StyleRotate(new Rotate(angle));
            }, -rockAngle, holdDuration * 0.5f).SetEase(Ease.InOutSine))
            .Append(DOTween.To(() => -rockAngle, angle => {
                winOverlayLabel.style.rotate = new StyleRotate(new Rotate(angle));
            }, 0f, holdDuration * 0.25f).SetEase(Ease.InOutSine));
            
            // Wipe out left-to-right. Overlay is anchored to scoreboardContainer with
            // left = GoalOverlayLeft; we shift translate-X by (targetWidth - w) so the right
            // edge stays fixed while the left edge moves rightward.
            mainSequence.Append(DOTween.To(() => targetWidth, w => {
                winOverlay.style.width = w;
                winOverlay.style.translate = new StyleTranslate(new Translate(
                    new Length(targetWidth - w, LengthUnit.Pixel),
                    new Length(0, LengthUnit.Pixel)));
            }, 0f, 0.6f).SetEase(Ease.InCubic))
            .OnComplete(() => {
                    winOverlay.style.display = DisplayStyle.None;
                    winOverlay.style.width = 0; // ready for next wipe-in
                    winOverlay.style.translate = new StyleTranslate(new Translate(
                        new Length(0, LengthUnit.Pixel),
                        new Length(0, LengthUnit.Pixel)));
                    winOverlayLabel.style.scale = new StyleScale(new Scale(Vector2.one));
                    winOverlayLabel.style.rotate = new StyleRotate(new Rotate(0f));
                    
                    // Animate league logo grow and reappear
                    if (leagueLogo != null)
                    {
                        leagueLogo.style.display = DisplayStyle.Flex;
                        DOTween.Sequence()
                            .Append(DOTween.To(() => 0f, s => {
                                leagueLogo.style.scale = new StyleScale(new Scale(new Vector2(s, s)));
                                leagueLogo.style.opacity = s;
                            }, 1f, 0.3f).SetEase(Ease.OutBack));
                    }
                });
            
            DebugLog($"[CustomScoreboard] Showing win animation for {teamName}, shutout: {isShutout}, temporary: {temporary}");
        }
        
        private void StartWinTextLoop()
        {
            if (winOverlayLabel == null) return;
            
            // Kill any existing text loop
            DOTween.Kill(winOverlayLabel);
            
            // Create a looping sequence with movement and pulse
            Sequence loopSequence = DOTween.Sequence()
                .SetTarget(winOverlayLabel);
            
            // Movement: left to right and back (8 seconds total cycle)
            // Scale pulse: grow and shrink (happens during movement)
            
            // Move right while pulsing
            loopSequence.Append(DOTween.To(() => 0f, x => {
                winOverlayLabel.style.translate = new StyleTranslate(new Translate(
                    new Length(x, LengthUnit.Pixel), 
                    new Length(0, LengthUnit.Pixel)));
            }, 15f, 2f).SetEase(Ease.InOutSine));
            
            // Move left while pulsing
            loopSequence.Append(DOTween.To(() => 15f, x => {
                winOverlayLabel.style.translate = new StyleTranslate(new Translate(
                    new Length(x, LengthUnit.Pixel), 
                    new Length(0, LengthUnit.Pixel)));
            }, -15f, 4f).SetEase(Ease.InOutSine));
            
            // Move back to center while pulsing
            loopSequence.Append(DOTween.To(() => -15f, x => {
                winOverlayLabel.style.translate = new StyleTranslate(new Translate(
                    new Length(x, LengthUnit.Pixel), 
                    new Length(0, LengthUnit.Pixel)));
            }, 0f, 2f).SetEase(Ease.InOutSine));
            
            // Loop the sequence
            loopSequence.SetLoops(-1, LoopType.Restart);
            
            // Separate scale pulse animation that loops independently
            DOTween.Sequence()
                .SetTarget(winOverlayLabel)
                .Append(DOTween.To(() => 1f, s => {
                    winOverlayLabel.style.scale = new StyleScale(new Scale(new Vector2(s, s)));
                }, 1.15f, 1.5f).SetEase(Ease.InOutSine))
                .Append(DOTween.To(() => 1.15f, s => {
                    winOverlayLabel.style.scale = new StyleScale(new Scale(new Vector2(s, s)));
                }, 1f, 1.5f).SetEase(Ease.InOutSine))
                .SetLoops(-1, LoopType.Restart);
        }
        
        private void HideWinAnimation()
        {
            if (winOverlay == null) return;
            
            // Kill any ongoing animations (overlay and text loop)
            DOTween.Kill(winOverlay);
            DOTween.Kill(winOverlayLabel);
            
            // Wipe out animation. Anchored coords: shift translate-X by (targetWidth - w)
            // so the right edge stays fixed while the left edge slides rightward.
            float targetWidth = ScorebugAnchor.GoalOverlayWidth;

            DOTween.Sequence()
                .SetTarget(winOverlay)
                .Append(DOTween.To(() => targetWidth, w => {
                    winOverlay.style.width = w;
                    winOverlay.style.translate = new StyleTranslate(new Translate(
                        new Length(targetWidth - w, LengthUnit.Pixel),
                        new Length(0, LengthUnit.Pixel)));
                }, 0f, 0.6f).SetEase(Ease.InCubic))
                .OnComplete(() => {
                    winOverlay.style.display = DisplayStyle.None;
                    winOverlay.style.width = 0;
                    winOverlay.style.translate = new StyleTranslate(new Translate(
                        new Length(0, LengthUnit.Pixel),
                        new Length(0, LengthUnit.Pixel)));
                    winOverlayLabel.style.scale = new StyleScale(new Scale(Vector2.one));
                    winOverlayLabel.style.rotate = new StyleRotate(new Rotate(0f));
                    
                    // Animate league logo grow and reappear
                    if (leagueLogo != null)
                    {
                        leagueLogo.style.display = DisplayStyle.Flex;
                        DOTween.Sequence()
                            .SetTarget(leagueLogo)
                            .Append(DOTween.To(() => 0f, s => {
                                leagueLogo.style.scale = new StyleScale(new Scale(new Vector2(s, s)));
                                leagueLogo.style.opacity = s;
                            }, 1f, 0.3f).SetEase(Ease.OutBack));
                    }
                });
        }

        // ============================================
        // GOAL ANIMATION METHODS
        // ============================================
        
        private void ShowGoalAnimation(PlayerTeam team, Action onComplete, bool isHatTrick = false)
        {
            try
            {
                if (goalOverlay == null || config == null || !config.enablePopups) 
                {
                    onComplete?.Invoke();
                    return;
                }
            
            // Set animation lock
            isAnimationPlaying = true;
            
            // Set the overlay color based on team from config colors (fully opaque)
            Color goalColor = GetTeamColor(team);
            goalColor.a = 1f; // Ensure fully opaque
            
            goalOverlay.style.backgroundColor = goalColor;
            
            // Add team logo behind text with translucent team color tint
            VisualElement logoBackground = goalOverlay.Q<VisualElement>("animationLogoBackground");
            if (logoBackground == null && config.showAnimationLogo)
            {
                logoBackground = new VisualElement();
                logoBackground.name = "animationLogoBackground";
                logoBackground.style.position = Position.Absolute;
                logoBackground.style.width = config.animationLogoWidth;
                logoBackground.style.height = config.animationLogoHeight;
                logoBackground.style.alignItems = Align.Center;
                logoBackground.style.justifyContent = Justify.Center;
                
                Texture2D teamLogo = GetTeamLogoTexture(team);
                if (teamLogo != null)
                {
                    SetTeamLogoBackground(logoBackground, teamLogo, goalColor);
                }
                
                // Apply team color tint with opacity
                Color tintColor = goalColor;
                tintColor.a = config.animationLogoOpacity;
                logoBackground.style.unityBackgroundImageTintColor = tintColor;
                
                goalOverlay.Insert(0, logoBackground); // Add behind text
            }
            else if (logoBackground != null && config.showAnimationLogo)
            {
                // Update existing logo
                Texture2D teamLogo = GetTeamLogoTexture(team);
                if (teamLogo != null)
                {
                    SetTeamLogoBackground(logoBackground, teamLogo, goalColor);
                }
                // Update size
                logoBackground.style.width = config.animationLogoWidth;
                logoBackground.style.height = config.animationLogoHeight;
                Color tintColor = goalColor;
                tintColor.a = config.animationLogoOpacity;
                logoBackground.style.unityBackgroundImageTintColor = tintColor;
                logoBackground.style.display = DisplayStyle.Flex;
            }
            else if (logoBackground != null && !config.showAnimationLogo)
            {
                logoBackground.style.display = DisplayStyle.None;
            }
            
            // Get team name from config and trim spaces for centered display
            string teamName = GetTeamName(team).Trim();
            goalOverlayLabel.text = isHatTrick ? $"{teamName} HAT TRICK!" : $"{teamName} GOAL!";
            
            // Set text color from config
            Color textColor = GetTeamTextColor(team);
            goalOverlayLabel.style.color = textColor;
            
            // Start with width 0 for wipe effect. Reset translate so a previously-killed
            // animation can't leave the overlay shifted.
            goalOverlay.style.width = 0;
            goalOverlay.style.translate = new StyleTranslate(new Translate(
                new Length(0, LengthUnit.Pixel), new Length(0, LengthUnit.Pixel)));
            goalOverlay.style.display = DisplayStyle.Flex;
            
            // Animate league logo shrink and disappear
            if (leagueLogo != null)
            {
                DOTween.Sequence()
                    .Append(DOTween.To(() => 1f, s => {
                        leagueLogo.style.scale = new StyleScale(new Scale(new Vector2(s, s)));
                        leagueLogo.style.opacity = s;
                    }, 0f, 0.3f).SetEase(Ease.InBack))
                    .OnComplete(() => leagueLogo.style.display = DisplayStyle.None);
            }
            
            // Animate: wipe left-to-right, pulse with rotation, rock text back-and-forth, then wipe away
            float targetWidth = ScorebugAnchor.GoalOverlayWidth;
            float rockAngle = 3f; // Rotation angle in degrees
            // SetTarget so DOTween.Kill(goalOverlay) in OnDestroy can find this sequence;
            // OnKill releases the animation lock if the sequence is killed before OnComplete.
            var mainSequence = DOTween.Sequence()
                .SetTarget(goalOverlay)
                .OnKill(() => isAnimationPlaying = false);
            
            // Wipe in from left to right
            mainSequence.Append(DOTween.To(() => 0f, w => {
                goalOverlay.style.width = w;
            }, targetWidth, 0.6f).SetEase(Ease.OutCubic));
            
            // Pulse scale AND rotation together
            mainSequence.Append(DOTween.To(() => 1f, s => {
                goalOverlayLabel.style.scale = new StyleScale(new Scale(new Vector2(s, s)));
            }, 1.3f, 0.4f).SetEase(Ease.OutBack));
            
            mainSequence.Join(DOTween.To(() => 0f, angle => {
                goalOverlayLabel.style.rotate = new StyleRotate(new Rotate(angle));
            }, rockAngle * 0.7f, 0.4f).SetEase(Ease.OutBack));
            
            mainSequence.Append(DOTween.To(() => 1.3f, s => {
                goalOverlayLabel.style.scale = new StyleScale(new Scale(new Vector2(s, s)));
            }, 1.0f, 0.3f));
            
            mainSequence.Join(DOTween.To(() => rockAngle * 0.7f, angle => {
                goalOverlayLabel.style.rotate = new StyleRotate(new Rotate(angle));
            }, 0f, 0.3f));
            
            // Rocking rotation animation (back and forth)
            mainSequence.Append(DOTween.To(() => 0f, angle => {
                goalOverlayLabel.style.rotate = new StyleRotate(new Rotate(angle));
            }, rockAngle, 0.4f).SetEase(Ease.InOutSine))
                .Append(DOTween.To(() => rockAngle, angle => {
                    goalOverlayLabel.style.rotate = new StyleRotate(new Rotate(angle));
                }, -rockAngle, 0.8f).SetEase(Ease.InOutSine))
                .Append(DOTween.To(() => -rockAngle, angle => {
                    goalOverlayLabel.style.rotate = new StyleRotate(new Rotate(angle));
                }, 0f, 0.4f).SetEase(Ease.InOutSine))
                // Wipe out left-to-right (scoreboard-local; right edge stays fixed via translate)
                .Append(DOTween.To(() => targetWidth, w => {
                    goalOverlay.style.width = w;
                    goalOverlay.style.translate = new StyleTranslate(new Translate(
                        new Length(targetWidth - w, LengthUnit.Pixel),
                        new Length(0, LengthUnit.Pixel)));
                }, 0f, 0.6f).SetEase(Ease.InCubic))
                .OnComplete(() => {
                    goalOverlay.style.display = DisplayStyle.None;
                    goalOverlay.style.width = 0;
                    goalOverlay.style.translate = new StyleTranslate(new Translate(
                        new Length(0, LengthUnit.Pixel),
                        new Length(0, LengthUnit.Pixel)));
                    goalOverlayLabel.style.scale = new StyleScale(new Scale(Vector2.one));
                    goalOverlayLabel.style.rotate = new StyleRotate(new Rotate(0f));
                    
                    // Animate league logo grow and reappear
                    if (leagueLogo != null)
                    {
                        leagueLogo.style.display = DisplayStyle.Flex;
                        DOTween.Sequence()
                            .Append(DOTween.To(() => 0f, s => {
                                leagueLogo.style.scale = new StyleScale(new Scale(new Vector2(s, s)));
                                leagueLogo.style.opacity = s;
                            }, 1f, 0.3f).SetEase(Ease.OutBack));
                    }
                    
                    isAnimationPlaying = false; // Release animation lock
                    onComplete?.Invoke();
                });
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error showing goal animation: {ex.Message}");
                isAnimationPlaying = false;
                onComplete?.Invoke();
            }
        }

        // ============================================
        // OT/SHOOTOUT WIN ANIMATION METHODS
        // ============================================
        
        private void ShowOTWinAnimation(PlayerTeam team, Action onComplete = null)
        {
            try
            {
                // OT wins - shorter hold duration (temporary=true)
                ShowWinAnimation(team, false, " OT WIN", true);
                onComplete?.Invoke();
            }
            catch (Exception ex)
            {
                DebugWarning($"[CustomScoreboard] Error showing OT win animation: {ex.Message}");
                onComplete?.Invoke();
            }
        }

        private void ShowShootoutWinAnimation(PlayerTeam team)
        {
            // SO wins - shorter hold duration (temporary=true)
            ShowWinAnimation(team, false, " SO WIN", true);
        }
    }
}
