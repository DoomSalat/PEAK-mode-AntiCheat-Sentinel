using BepInEx.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace PeakSentinel.Ui
{
    public sealed class HotkeyButton
    {
        private const float Width = 330f;
        private const float TextSize = 19f;
        private const string KeyFormat = "Window key: {0}";
        private const string NoKey = "none";
        private const string Waiting = "Press a key... (Esc cancels)";

        private readonly ConfigEntry<Key> _key;
        private readonly TextMeshProUGUI _label;

        public HotkeyButton(UiFactory ui, Transform parent, ConfigEntry<Key> key)
        {
            _key = key;
            (Button _, TextMeshProUGUI label) = ui.Button(parent, string.Empty, Width, TextSize, StartListening);
            _label = label;
            ShowKey();
        }

        public bool Listening { get; private set; }

        public void Capture(Keyboard keyboard)
        {
            foreach (KeyControl control in keyboard.allKeys)
            {
                if (control == null || !control.wasPressedThisFrame)
                    continue;

                if (control.keyCode != Key.Escape)
                    _key.Value = control.keyCode;

                StopListening();
                return;
            }
        }

        public void StopListening()
        {
            Listening = false;
            ShowKey();
        }

        private void StartListening()
        {
            Listening = true;
            _label.text = Waiting;
        }

        private void ShowKey()
        {
            _label.text = string.Format(KeyFormat, _key.Value == Key.None ? NoKey : _key.Value.ToString());
        }
    }
}