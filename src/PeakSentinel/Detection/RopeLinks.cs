using System.Collections;
using Photon.Pun;

namespace PeakSentinel.Detection
{
    public sealed class RopeLinks
    {
        private const string LinkPrefix = "rkb.link.";
        private const char PairSeparator = '_';
        private const char PayloadSeparator = '|';
        private const string Tied = "1";
        private const int TiedIndex = 1;

        public bool IsTied(int actor)
        {
            if (!PhotonNetwork.InRoom)
                return false;

            foreach (DictionaryEntry entry in PhotonNetwork.CurrentRoom.CustomProperties)
            {
                if (entry.Key is string key && key.StartsWith(LinkPrefix) && entry.Value is string payload && Involves(key, actor) && IsTiedPayload(payload))
                    return true;
            }

            return false;
        }

        private bool Involves(string key, int actor)
        {
            string[] pair = key.Substring(LinkPrefix.Length).Split(PairSeparator);

            if (pair.Length != 2)
                return false;

            return (int.TryParse(pair[0], out int first) && first == actor) || (int.TryParse(pair[1], out int second) && second == actor);
        }

        private bool IsTiedPayload(string payload)
        {
            string[] parts = payload.Split(PayloadSeparator);
            return parts.Length > TiedIndex && parts[TiedIndex] == Tied;
        }
    }
}
