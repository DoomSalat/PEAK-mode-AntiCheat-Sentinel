using System;
using System.Collections.Generic;
using Portningsbolaget.Platforms;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Reporting.Ui
{
    public sealed class AvatarCache
    {
        private const string UserIdProperty = "UserID";
        private const string PlatformProperty = "PlatformFamily";

        private readonly Dictionary<string, Sprite> _avatars = new Dictionary<string, Sprite>();
        private readonly HashSet<string> _requested = new HashSet<string>();

        public int Version { get; private set; }

        public Sprite Of(PhotonPlayer player)
        {
            if (!(player.CustomProperties[UserIdProperty] is string id))
                return null;

            if (_avatars.TryGetValue(id, out Sprite avatar))
                return avatar;

            if (_requested.Add(id) && IsSamePlatform(player) && ulong.TryParse(id, out ulong userId))
                PlatformManager.Platform.GetAvatarAsync(userId, texture => Store(id, texture));

            return null;
        }

        private bool IsSamePlatform(PhotonPlayer player)
        {
            PlatformUtility.PlatformFamily local = PlatformManager.Platform.PlatformFamily;
            return !(player.CustomProperties[PlatformProperty] is string family) || !Enum.TryParse(family, out PlatformUtility.PlatformFamily remote) || remote == local;
        }

        private void Store(string id, Texture2D texture)
        {
            if (texture == null)
                return;

            _avatars[id] = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            Version++;
        }
    }
}
