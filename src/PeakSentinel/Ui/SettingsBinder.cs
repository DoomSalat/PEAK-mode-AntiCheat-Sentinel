using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using Photon.Pun;
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

        private readonly List<(Toggle Toggle, ConfigEntry<bool> Entry)> _bindings = new List<(Toggle Toggle, ConfigEntry<bool> Entry)>();
        private readonly List<(Toggle Toggle, CanvasGroup Group, Func<bool> Enabled)> _dependents = new List<(Toggle Toggle, CanvasGroup Group, Func<bool> Enabled)>();
        private readonly List<GameObject> _hostNotes = new List<GameObject>();
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

        public void Depend(Toggle toggle, Func<bool> enabled)
        {
            _dependents.Add((toggle, toggle.GetComponent<CanvasGroup>(), enabled));
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