using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeakSentinel.Ui
{
    public sealed class SettingsBinder
    {
        private const float TextSize = 20f;
        private const float DisabledAlpha = 0.45f;
        private const float IndentStep = 38f;
        private const string HostOnly = "Blocking and kick work only while you are the host";
        private const float HintSize = 16f;

        private readonly List<(Toggle Toggle, ConfigEntry<bool> Entry)> _bindings = new List<(Toggle Toggle, ConfigEntry<bool> Entry)>();
        private readonly List<(Toggle Toggle, CanvasGroup Group, Func<bool> Enabled)> _dependents = new List<(Toggle Toggle, CanvasGroup Group, Func<bool> Enabled)>();
        private readonly List<GameObject> _hostNotes = new List<GameObject>();
        private readonly List<(TextMeshProUGUI Label, string Effect, Func<bool, HintScope> Scope)> _hints = new List<(TextMeshProUGUI Label, string Effect, Func<bool, HintScope> Scope)>();
        private readonly UiFactory _ui;

        public SettingsBinder(UiFactory ui)
        {
            _ui = ui;
        }

        public Toggle Bind(Transform parent, string label, ConfigEntry<bool> entry)
        {
            Toggle toggle = _ui.Toggle(parent, label, TextSize, entry.Value, value => entry.Value = value);
            _bindings.Add((toggle, entry));
            return toggle;
        }

        public Toggle BindIndented(Transform parent, int level, string label, ConfigEntry<bool> entry)
        {
            RectTransform row = _ui.Row(parent, 0f);
            _ui.Fixed(_ui.Row(row, 0f), IndentStep * level, 0f);
            Toggle toggle = Bind(row, label, entry);
            _ui.Flexible(toggle, 1f, 0f);
            return toggle;
        }

        public Toggle BindPair(Transform parent, string label, ConfigEntry<bool> main, string secondLabel, ConfigEntry<bool> second, float secondWidth)
        {
            RectTransform row = _ui.Row(parent, 10f);
            _ui.Flexible(Bind(row, label, main), 1f, 0f);
            Toggle secondToggle = Bind(row, secondLabel, second);
            _ui.Fixed(secondToggle, secondWidth, 0f);
            Depend(secondToggle, () => main.Value);
            return secondToggle;
        }

        public void Depend(Toggle toggle, Func<bool> enabled)
        {
            _dependents.Add((toggle, toggle.GetComponent<CanvasGroup>(), enabled));
        }

        public void Hint(Transform parent, int level, string effect, Func<bool, HintScope> scope)
        {
            RectTransform row = _ui.Row(parent, 0f);
            _ui.Fixed(_ui.Row(row, 0f), IndentStep * (level + 1), 0f);
            TextMeshProUGUI label = _ui.Label(row, string.Empty, HintSize, UiPalette.Notice, true);
            _ui.Flexible(label, 1f, 0f);
            _hints.Add((label, effect, scope));
        }

        public void HostNote(Transform parent)
        {
            _hostNotes.Add(_ui.Label(parent, HostOnly, TextSize, UiPalette.Suspicious, false).gameObject);
        }

        public void Refresh()
        {
            foreach ((Toggle toggle, ConfigEntry<bool> entry) in _bindings)
                toggle.SetIsOnWithoutNotify(entry.Value);

            foreach ((Toggle toggle, CanvasGroup group, Func<bool> enabled) in _dependents)
            {
                bool active = enabled();
                toggle.interactable = active;
                group.alpha = active ? 1f : DisabledAlpha;
            }

            foreach ((TextMeshProUGUI label, string effect, Func<bool, HintScope> scope) in _hints)
                label.text = ScopeHint.Text(effect, scope);

            foreach (GameObject note in _hostNotes)
            {
                if (PhotonNetwork.IsMasterClient)
                    note.SetActive(false);
                else
                    note.SetActive(true);
            }
        }
    }
}