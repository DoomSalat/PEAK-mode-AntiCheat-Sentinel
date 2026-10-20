using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Network
{
    public sealed class SpawnClock : MonoBehaviour
    {
        private readonly Dictionary<Character, float> _firstSeen = new Dictionary<Character, float>();
        private string _room;
        private float _joinedAt;

        private void Update()
        {
            string room = PhotonNetwork.CurrentRoom == null ? null : PhotonNetwork.CurrentRoom.Name;

            if (room != _room)
            {
                _room = room;
                _joinedAt = Time.time;
                _firstSeen.Clear();
            }

            foreach (Character character in Character.AllCharacters)
            {
                if (character != null && !_firstSeen.ContainsKey(character))
                    _firstSeen[character] = Time.time;
            }
        }

        public bool JoinedRoomWithin(float seconds)
        {
            return Time.time - _joinedAt < seconds;
        }

        public bool SpawnedWithin(Character character, float seconds)
        {
            return character != null && (!_firstSeen.TryGetValue(character, out float seen) || Time.time - seen < seconds);
        }

        public bool SpawnedWithin(PhotonPlayer player, float seconds)
        {
            Character character = PlayerHandler.GetPlayerCharacter(player);
            return character == null || SpawnedWithin(character, seconds);
        }
    }
}