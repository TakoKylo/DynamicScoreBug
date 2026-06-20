using System;
using UnityEngine;
using UnityEngine.UIElements;
using UITK = UnityEngine.UIElements;

namespace CustomScoreboard.UI
{
    public partial class ScoreboardUIManager
    {
        // Note: previously took a `color` arg, but AddButtonFlash unconditionally re-sets the
        // background to BtnBrightGray on hover/click/geometry — so any caller-supplied color
        // was overwritten on the very next layout pass. Param dropped to stop misleading callers.
        private void styleButton(Button button)
        {
            ForceUIFont(button);
            button.style.backgroundColor = new StyleColor(ButtonBg);
            button.style.color = Color.white;
            button.style.fontSize = 14;
            button.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Normal;
            button.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);
            button.style.paddingTop = 8;
            button.style.paddingBottom = 8;
            button.style.paddingLeft = 18;
            button.style.paddingRight = 18;
            button.style.height = 50;
            button.style.minWidth = 120;
            button.style.marginLeft = 4;
            button.style.marginRight = 4;
            button.style.whiteSpace = WhiteSpace.NoWrap;
            AddButtonFlash(button);
        }

        private void styleBottomButton(Button button)
        {
            // CompAdjust footer buttons: height 50, 18px horizontal padding, RowBg
            // fill, no explicit font size (they inherit the panel default). The 8px
            // gap below the row comes from the panel's bottom padding.
            ForceUIFont(button);
            button.style.backgroundColor = new StyleColor(BtnBrightGray);
            button.style.color = Color.white;
            button.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);
            button.style.whiteSpace = WhiteSpace.NoWrap;
            button.style.paddingLeft = 18;
            button.style.paddingRight = 18;
            button.style.height = 50;
            AddButtonFlash(button);
        }

        // CompAdjust button hover: white fill with black text on hover; revert to
        // the ButtonBg fill with white text on leave. No click-flash or geometry
        // reset - the caller's initial background shows until the first hover.
        private static void AddButtonFlash(Button b)
        {
            b.RegisterCallback<PointerEnterEvent>(_ =>
            {
                b.style.backgroundColor = Color.white;
                b.style.color = Color.black;
            });
            b.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                b.style.backgroundColor = new StyleColor(ButtonBg);
                b.style.color = Color.white;
            });
        }

        // CompAdjust toggle look: recolor the checkbox frame to a dark fill with a
        // medium-gray border so it reads clearly against the dark rows. The default
        // Unity USS draws a light box that disappears against this panel. Applied on
        // AttachToPanel because the inner ".unity-toggle__input" element only exists
        // once the toggle is parented.
        private static void StyleConfigCheckbox(UITK.Toggle toggle)
        {
            if (toggle == null) return;
            toggle.RegisterCallback<UITK.AttachToPanelEvent>(_ =>
            {
                var input = toggle.Q(className: "unity-toggle__input");
                if (input == null) return;
                input.style.backgroundColor   = new UITK.StyleColor(new Color(0.15f, 0.15f, 0.15f));
                input.style.borderTopColor    = new UITK.StyleColor(new Color(0.4f, 0.4f, 0.4f));
                input.style.borderBottomColor = new UITK.StyleColor(new Color(0.4f, 0.4f, 0.4f));
                input.style.borderLeftColor   = new UITK.StyleColor(new Color(0.4f, 0.4f, 0.4f));
                input.style.borderRightColor  = new UITK.StyleColor(new Color(0.4f, 0.4f, 0.4f));
            });
        }

        private void AddSection(VisualElement container, string title, int fontSize = 24)
        {
            // CompAdjust section header: yellow-ish text, no padding, tagged
            // "cfg-header" so the SEARCH filter can hide it while filtering.
            var section = new Label(title);
            section.AddToClassList("cfg-header");
            section.style.fontSize = fontSize;
            section.style.marginTop = 16;
            section.style.marginBottom = 8;
            section.style.color = new Color(0.9f, 0.9f, 0.5f);
            ForceUIFont(section);
            container.Add(section);
        }
    }
}
