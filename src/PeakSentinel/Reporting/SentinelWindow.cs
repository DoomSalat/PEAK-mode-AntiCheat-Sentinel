using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using PeakSentinel.Detection;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PeakSentinel.Reporting
{
    public sealed class SentinelWindow : MonoBehaviour
    {
        private const int WindowId = 0x5E471;
        private const int MaxLines = 500;
        private const float TitleHeight = 20f;
        private const string FollowLabel = "Follow newest";
        private const float Width = 760f;
        private const float Height = 460f;
        private const float TopMargin = 40f;
        private const int FontSize = 14;
        private const string TimeFormat = "HH:mm:ss";
        private const string Empty = "no findings yet";
        private const string HostOnly = "Blocking works only while you are the host";
        private const string BlockLabel = "Block cheat item spawns (spawners, forged pickups and inventories)";
        private const string AllowMineLabel = "Allow my own spawns (host)";
        private const string NoticesLabel = "Show notices";
        private const string ClearLabel = "Clear";

        private static readonly Color FlagColor = new Color(1f, 0.45f, 0.4f); // Static: fixed window colors
        private static readonly Color BlockedColor = new Color(1f, 0.75f, 0.2f); // Static: fixed window colors
        private static readonly Color NoticeColor = new Color(0.75f, 0.75f, 0.75f); // Static: fixed window colors

        private readonly List<(string Text, Color Color, bool IsNotice)> _lines = new List<(string Text, Color Color, bool IsNotice)>();
        private ConfigEntry<Key> _toggleKey;
        private ProtectionSettings _protection;
        private string _title;
        private Rect _window;
        private Vector2 _scroll;
        private bool _showNotices = true;
        private bool _followNewest = true;
        private GUIStyle _lineStyle;

        public bool IsOpen { get; private set; }

        public void Initialize(string title, ConfigEntry<Key> toggleKey, ProtectionSettings protection)
        {
            _title = title;
            _toggleKey = toggleKey;
            _protection = protection;
            _window = new Rect((Screen.width - Width) / 2f, TopMargin, Width, Height);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null && _toggleKey.Value != Key.None && keyboard[_toggleKey.Value].wasPressedThisFrame)
                IsOpen = !IsOpen;
        }

        private void OnGUI()
        {
            if (!IsOpen)
                return;

            if (_lineStyle == null)
                _lineStyle = new GUIStyle(GUI.skin.label) { fontSize = FontSize, wordWrap = true };

            _window = GUI.Window(WindowId, _window, DrawWindow, $"{_title} — {_toggleKey.Value}");
        }

        public void Add(string text, bool isFlag, bool isBlocked)
        {
            Color color = isBlocked ? BlockedColor : isFlag ? FlagColor : NoticeColor;
            _lines.Add(($"[{DateTime.Now.ToString(TimeFormat)}] {text}", color, !isFlag));

            if (_lines.Count > MaxLines)
                _lines.RemoveAt(0);
        }

        private void DrawWindow(int id)
        {
            _protection.BlockCheatSpawns.Value = GUILayout.Toggle(_protection.BlockCheatSpawns.Value, BlockLabel);
            _protection.AllowMySpawns.Value = GUILayout.Toggle(_protection.AllowMySpawns.Value, AllowMineLabel);
            GUILayout.BeginHorizontal();
            _showNotices = GUILayout.Toggle(_showNotices, NoticesLabel);
            _followNewest = GUILayout.Toggle(_followNewest, FollowLabel);
            GUILayout.FlexibleSpace();

            if (!PhotonNetwork.IsMasterClient)
                GUILayout.Label(HostOnly);

            if (GUILayout.Button(ClearLabel))
                _lines.Clear();

            GUILayout.EndHorizontal();

            if (_followNewest)
                _scroll.y = float.MaxValue;

            _scroll = GUILayout.BeginScrollView(_scroll, false, true);

            if (_lines.Count == 0)
                GUILayout.Label(Empty);

            foreach ((string text, Color color, bool isNotice) in _lines)
            {
                if (isNotice && !_showNotices)
                    continue;

                _lineStyle.normal.textColor = color;
                GUILayout.Label(text, _lineStyle);
            }

            GUILayout.EndScrollView();
            GUI.DragWindow(new Rect(0f, 0f, Width, TitleHeight));
        }
    }
}
