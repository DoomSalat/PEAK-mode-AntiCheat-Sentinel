using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Ui
{
    public sealed class VoiceLevels
    {
        private const string PercentFormat = "{0}%";
        private const float PercentScale = 200f;

        public float LevelOf(PhotonPlayer player)
        {
            return AudioLevels.GetPlayerLevel(player.UserId);
        }

        public string PercentOf(float level)
        {
            return string.Format(PercentFormat, Mathf.RoundToInt(level * PercentScale));
        }

        // SAVE NOTE: the level lives in vanilla AudioLevels for the session only; LOAD: file by Steam ID → SetPlayerLevel on join; SAVE: here
        public void Set(PhotonPlayer player, float level)
        {
            if (player == null)
                return;

            AudioLevels.SetPlayerLevel(player.UserId, level);
        }
    }
}
