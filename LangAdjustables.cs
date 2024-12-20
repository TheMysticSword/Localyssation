using System.Collections.Generic;
using UnityEngine;

namespace Localyssation
{
    // "adjustables" are in-game objects that must be automatically adjusted with
    // language-specific variants (strings, textures, etc.) whenever language is changed in-game
    internal static class LangAdjustables
    {
        public static List<ILangAdjustable> nonMonoBehaviourAdjustables = new List<ILangAdjustable>();

        public static void Init()
        {
            Localyssation.instance.onLanguageChanged += (newLanguage) =>
            {
                // copy the list to avoid the loop breaking if an entry self-destructs mid-loop
                var safeAdjustables = new List<ILangAdjustable>(nonMonoBehaviourAdjustables);
                foreach (var replaceable in safeAdjustables)
                {
                    replaceable.AdjustToLanguage(newLanguage);
                }
            };
        }

        // handy function to slot into the newTextFunc param when you need a simple key->string replacement
        public static System.Func<int, string> GetStringFunc(string key, string defaultValue = Localyssation.GET_STRING_DEFAULT_VALUE_ARG_UNSPECIFIED)
        {
            return (fontSize) => Localyssation.GetString(key, fontSize, defaultValue);
        }

        public static void RegisterText(UnityEngine.UI.Text text, System.Func<int, string> newTextFunc)
        {
            if (text.GetComponent<LangAdjustableUIText>()) return;

            var replaceable = text.gameObject.AddComponent<LangAdjustableUIText>();
            replaceable.newTextFunc = newTextFunc;
        }

        public static void RegisterDropdown(UnityEngine.UI.Dropdown dropdown, List<System.Func<int, string>> newTextFuncs)
        {
            if (dropdown.GetComponent<LangAdjustableUIDropdown>()) return;

            var replaceable = dropdown.gameObject.AddComponent<LangAdjustableUIDropdown>();
            replaceable.newTextFuncs = newTextFuncs;
        }

        public interface ILangAdjustable
        {
            void AdjustToLanguage(Language newLanguage);
        }

        public class LangAdjustableUIText : MonoBehaviour, ILangAdjustable
        {
            public UnityEngine.UI.Text text;
            public System.Func<int, string> newTextFunc;

            public bool textAutoShrinkable = true;
            public bool textAutoShrunk = false;
            public bool orig_resizeTextForBestFit = false;
            public int orig_resizeTextMaxSize;
            public int orig_resizeTextMinSize;

            public void Awake()
            {
                text = GetComponent<UnityEngine.UI.Text>();
                Localyssation.instance.onLanguageChanged += onLanguageChanged;
            }

            public void Start()
            {
                AdjustToLanguage(Localyssation.currentLanguage);
            }

            private void onLanguageChanged(Language newLanguage)
            {
                AdjustToLanguage(newLanguage);
            }

            public void AdjustToLanguage(Language newLanguage)
            {
                if (newTextFunc != null)
                {
                    text.text = newTextFunc(text.fontSize);
                }

                if (newLanguage.info.autoShrinkOverflowingText != textAutoShrunk)
                {
                    if (newLanguage.info.autoShrinkOverflowingText)
                    {
                        if (textAutoShrinkable)
                        {
                            orig_resizeTextForBestFit = text.resizeTextForBestFit;
                            orig_resizeTextMaxSize = text.resizeTextMaxSize;
                            orig_resizeTextMinSize = text.resizeTextMinSize;

                            text.resizeTextMaxSize = text.fontSize;
                            text.resizeTextMinSize = System.Math.Min(2, text.resizeTextMinSize);
                            text.resizeTextForBestFit = true;

                            textAutoShrunk = true;
                        }
                    }
                    else
                    {
                        text.resizeTextForBestFit = orig_resizeTextForBestFit;
                        text.resizeTextMaxSize = orig_resizeTextMaxSize;
                        text.resizeTextMinSize = orig_resizeTextMinSize;

                        textAutoShrunk = false;
                    }
                }
            }

            public void OnDestroy()
            {
                Localyssation.instance.onLanguageChanged -= onLanguageChanged;
            }
        }

        public class LangAdjustableUIDropdown : MonoBehaviour, ILangAdjustable
        {
            public UnityEngine.UI.Dropdown dropdown;
            public List<System.Func<int, string>> newTextFuncs;

            public void Awake()
            {
                dropdown = GetComponent<UnityEngine.UI.Dropdown>();
                Localyssation.instance.onLanguageChanged += onLanguageChanged;

                // auto-shrink text according to options
                if (dropdown.itemText)
                {
                    dropdown.itemText.gameObject.AddComponent<LangAdjustableUIText>();
                }
                if (dropdown.captionText)
                {
                    dropdown.captionText.gameObject.AddComponent<LangAdjustableUIText>();
                }
            }

            public void Start()
            {
                AdjustToLanguage(Localyssation.currentLanguage);
            }

            private void onLanguageChanged(Language newLanguage)
            {
                AdjustToLanguage(newLanguage);
            }

            public void AdjustToLanguage(Language newLanguage)
            {
                if (newTextFuncs.Count == dropdown.options.Count)
                {
                    for (var i = 0; i < dropdown.options.Count; i++)
                    {
                        var option = dropdown.options[i];
                        option.text = newTextFuncs[i](-1);
                    }
                    dropdown.RefreshShownValue();
                }
            }

            public void OnDestroy()
            {
                Localyssation.instance.onLanguageChanged -= onLanguageChanged;
            }
        }
    }
}
