using DG.Tweening;
using PeakSentinel.Protection;
using PeakSentinel.Reporting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Ui
{
    public sealed class AvatarBadge : MonoBehaviour
    {
        private const float MarkSize = 40f;
        private const float FontSize = 38f;
        private const float PulseScale = 1.3f;
        private const float PulseSeconds = 0.45f;
        private const string SuspectMark = "?";
        private const string CheaterMark = "!";
        private const string MutedMark = "X";
        private const float CrossSize = 80f;

        private static readonly Color MutedTint = new Color(0.4f, 0.4f, 0.4f, 1f); // Static: fixed grey for muted avatars
        private static readonly Color CrossColor = new Color(0.12f, 0.1f, 0.08f, 0.9f); // Static: fixed colour of the mute cross

        private RectTransform _avatar;
        private RectTransform _frame;
        private Image _avatarImage;
        private TextMeshProUGUI _mark;
        private TextMeshProUGUI _cross;
        private MuteList _mutes;
        private PhotonPlayer _player;
        private bool _shownMuted;
        private Tween _pulse;
        private FindingLog _findings;
        private int _actor;
        private PlayerStanding _shown = PlayerStanding.Clean;

        public void Initialize(Image avatar, PhotonPlayer player, FindingLog findings, MuteList mutes)
        {
            _player = player;
            _actor = player.ActorNumber;
            _findings = findings;
            _mutes = mutes;

            if (_mark == null)
                Build(avatar);

            Show(_findings.StandingOf(_actor));
            ShowMuted(_mutes.IsMuted(_player));
        }

        private void Update()
        {
            if (_findings == null)
                return;

            float flip = Mathf.Sign(_avatar.localScale.y);

            if (!Mathf.Approximately(_frame.localScale.y, flip))
                _frame.localScale = new Vector3(1f, flip, 1f);

            PlayerStanding standing = _findings.StandingOf(_actor);

            if (standing != _shown)
                Show(standing);

            bool muted = _mutes.IsMuted(_player);

            if (muted != _shownMuted)
                ShowMuted(muted);
        }

        private void OnDestroy()
        {
            _pulse?.Kill();
        }

        private void Build(Image avatar)
        {
            _avatarImage = avatar;
            _avatar = avatar.rectTransform;
            _frame = new GameObject(nameof(AvatarBadge), typeof(RectTransform)).GetComponent<RectTransform>();
            _frame.SetParent(_avatar, false);
            _frame.anchorMin = Vector2.zero;
            _frame.anchorMax = Vector2.one;
            _frame.pivot = new Vector2(0.5f, 0.5f);
            _frame.offsetMin = Vector2.zero;
            _frame.offsetMax = Vector2.zero;

            RectTransform holder = new GameObject(nameof(AvatarBadge), typeof(RectTransform)).GetComponent<RectTransform>();
            holder.SetParent(_frame, false);
            holder.anchorMin = Vector2.one;
            holder.anchorMax = Vector2.one;
            holder.pivot = new Vector2(0.5f, 0.5f);
            holder.sizeDelta = new Vector2(MarkSize, MarkSize);
            holder.anchoredPosition = Vector2.zero;
            _mark = Glyph(holder);
            _cross = Glyph(_frame);
            _cross.fontSize = CrossSize;
            _cross.color = CrossColor;
            _cross.text = MutedMark;
            _cross.transform.SetAsFirstSibling();
        }

        private void ShowMuted(bool muted)
        {
            _shownMuted = muted;
            _avatarImage.color = muted ? MutedTint : Color.white;

            if (muted)
                _cross.gameObject.SetActive(true);
            else
                _cross.gameObject.SetActive(false);
        }

        private TextMeshProUGUI Glyph(RectTransform holder)
        {
            TextMeshProUGUI glyph = new GameObject(nameof(TextMeshProUGUI), typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            RectTransform rect = glyph.rectTransform;
            rect.SetParent(holder, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            glyph.font = GameFonts.Main();
            glyph.fontSize = FontSize;
            glyph.alignment = TextAlignmentOptions.Center;
            glyph.textWrappingMode = TextWrappingModes.NoWrap;
            glyph.raycastTarget = false;
            return glyph;
        }

        private void Show(PlayerStanding standing)
        {
            _shown = standing;
            _pulse?.Kill();
            RectTransform holder = (RectTransform)_mark.rectTransform.parent;
            holder.localScale = Vector3.one;

            switch (standing)
            {
                case PlayerStanding.Cheater:
                    _mark.text = CheaterMark;
                    _mark.color = UiPalette.CheatBright;
                    _pulse = holder.DOScale(PulseScale, PulseSeconds).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
                    break;

                case PlayerStanding.Suspect:
                    _mark.text = SuspectMark;
                    _mark.color = UiPalette.SuspiciousBright;
                    break;

                default:
                    _mark.text = string.Empty;
                    break;
            }
        }
    }
}