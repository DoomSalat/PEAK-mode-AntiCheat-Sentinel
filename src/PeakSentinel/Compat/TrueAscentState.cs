using System.Collections.Generic;
using System.Globalization;
using ExitGames.Client.Photon;
using Peak.Afflictions;
using Photon.Pun;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Compat
{
    public sealed class TrueAscentState
    {
        private const string VersionKey = "FAver";
        private const string FinalAscentKey = "FA";
        private const string ApocalypseKey = "APO";
        private const string WeatherKey = "APOW";
        private const string PermanentWeatherKey = "APOP";
        private const string GhostModifierKey = "APOGM";
        private const string SizeKey = "APOSZ";
        private const string BodyKey = "APOSB";
        private const string BallKey = "APOKBP";
        private const string BuriedKey = "APOAV";
        private const string KickKey = "TFAkick";
        private const char GhostModifierSeparator = '|';
        private const double GhostModifierSlack = 5d;
        private const int MiniSize = 1;
        private const int MegaSize = 2;
        private const int BallBody = 3;
        private const string OxygenTank = "FA_OxygenTank";
        private const string Prefix = "True Final Ascent: ";
        private const string SpawnTag = "FA_";
        private const string FinalAscentName = "Final Ascent";
        private const string ApocalypseName = "Apocalypse";
        private const string FloodWeather = "flood";
        private const string WispsWeather = "wisps";
        private const string JetpackVote = "jetpack";
        private const string TailwindVote = "tailwind";
        private const string MegaboostVote = "megaboost";
        private const string ShieldVote = "shield";
        private const string HealshroomVote = "healshroom";

        private static readonly HashSet<string> MovingWeathers = new HashSet<string> // Static: TFA weathers that move players by script (lift, swim, carry, wind)
        {
            "antigrav", "flood", "tsunami", "avalanche", "storm", "rain", "snow", "sand",
        };

        private readonly HashSet<Character> _landed = new HashSet<Character>();

        public bool InLobby => FinalAscentRun || Apocalypse || AnyPlayerHasMod();

        public bool FinalAscentRun => RoomFlag(FinalAscentKey);

        public bool Apocalypse => RoomFlag(ApocalypseKey);

        public bool KicksAllowed => InLobby && !(RoomValue(KickKey) is bool allowed && !allowed);

        public string ModeName
        {
            get
            {
                if (FinalAscentRun && Apocalypse)
                    return FinalAscentName + " + " + ApocalypseName;

                if (FinalAscentRun)
                    return FinalAscentName;

                return Apocalypse ? ApocalypseName : null;
            }
        }

        public string MovementCause(Character character)
        {
            PhotonPlayer owner = character.photonView.Owner;

            if (IsBall(owner))
                return Prefix + "ball mode";

            if (PlayerFlag(owner, BuriedKey))
                return Prefix + "buried by an avalanche";

            if (!Apocalypse)
                return null;

            string weather = Weather();

            if (weather != null && MovingWeathers.Contains(weather))
                return Prefix + weather + " weather";

            if (Size() == MiniSize || Size() == MegaSize)
                return Prefix + "mini or mega size";

            return GhostModifierActive(JetpackVote) ? Prefix + "ghost vote jetpack" : null;
        }

        public string StaminaCause(Character character)
        {
            if (IsBall(character.photonView.Owner))
                return Prefix + "ball mode";

            if (!Apocalypse)
                return null;

            if (GhostModifierActive(TailwindVote))
                return Prefix + "ghost vote tailwind";

            if (Weather() == FloodWeather)
                return Prefix + "flood weather";

            return Size() == MiniSize ? Prefix + "mini size" : null;
        }

        public string StatusCause(Character character, CharacterAfflictions.STATUSTYPE status)
        {
            PhotonPlayer owner = character.photonView.Owner;

            if (IsBall(owner))
                return Prefix + "ball mode";

            if (status == CharacterAfflictions.STATUSTYPE.Cold && PlayerFlag(owner, BuriedKey))
                return Prefix + "buried by an avalanche";

            Item held = character.data.currentItem;

            if (held != null && held.name.StartsWith(OxygenTank))
                return Prefix + "oxygen tank";

            if (!Apocalypse)
                return null;

            if (status == CharacterAfflictions.STATUSTYPE.Hot && Weather() == FloodWeather)
                return Prefix + "flood weather";

            return Size() == MegaSize ? Prefix + "mega size" : null;
        }

        public string EffectCause(Character character, Affliction.AfflictionType effect, bool freshJoin)
        {
            if (effect == Affliction.AfflictionType.InfiniteStamina)
            {
                if (HasMod(character.photonView.Owner) && character.refs.afflictions.HasAfflictionType(Affliction.AfflictionType.FasterBoi, out _))
                    return Prefix + "energy drink combo";

                return Apocalypse && GhostModifierActive(MegaboostVote) ? Prefix + "ghost vote megaboost" : null;
            }

            if (effect != Affliction.AfflictionType.Invincibility || !Apocalypse)
                return null;

            if (GhostModifierActive(ShieldVote))
                return Prefix + "ghost vote shield";

            return freshJoin ? Prefix + "late join grace" : null;
        }

        public string SpawnCause(PhotonView created)
        {
            object[] data = created.InstantiationData;

            if (data != null && data.Length > 0 && data[0] is string tag && tag.StartsWith(SpawnTag))
                return Prefix + "tagged spawn " + tag;

            if (!Apocalypse)
                return null;

            if (Weather() == WispsWeather)
                return Prefix + "wisp gift";

            return GhostModifierActive(HealshroomVote) ? Prefix + "ghost vote healshroom" : null;
        }

        public string LandingCause(Character character)
        {
            if (!FinalAscentRun || !_landed.Add(character))
                return null;

            _landed.RemoveWhere(landed => landed == null);
            return Prefix + "rejoin landing";
        }

        private string Weather()
        {
            string weather = RoomValue(WeatherKey) as string;
            return string.IsNullOrEmpty(weather) ? RoomValue(PermanentWeatherKey) as string : weather;
        }

        private int Size()
        {
            if (RoomValue(BodyKey) is int body && (body == MiniSize || body == MegaSize))
                return body;

            return RoomValue(SizeKey) is int size ? size : 0;
        }

        private bool IsBall(PhotonPlayer player)
        {
            return PlayerFlag(player, BallKey) || (Apocalypse && RoomValue(BodyKey) is int body && body == BallBody);
        }

        private bool GhostModifierActive(string modifier)
        {
            if (!(RoomValue(GhostModifierKey) is string text))
                return false;

            string[] parts = text.Split(GhostModifierSeparator);
            return parts.Length >= 2 && parts[0] == modifier && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double end) && PhotonNetwork.Time <= end + GhostModifierSlack;
        }

        private bool AnyPlayerHasMod()
        {
            if (!PhotonNetwork.InRoom)
                return false;

            foreach (PhotonPlayer player in PhotonNetwork.PlayerList)
            {
                if (HasMod(player))
                    return true;
            }

            return false;
        }

        private bool HasMod(PhotonPlayer player)
        {
            return player != null && player.CustomProperties != null && player.CustomProperties.ContainsKey(VersionKey);
        }

        private bool RoomFlag(string key)
        {
            return RoomValue(key) is bool flag && flag;
        }

        private object RoomValue(string key)
        {
            Hashtable properties = PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.CustomProperties : null;
            return properties != null && properties.TryGetValue(key, out object value) ? value : null;
        }

        private bool PlayerFlag(PhotonPlayer player, string key)
        {
            return player != null && player.CustomProperties != null && player.CustomProperties.TryGetValue(key, out object value) && value is bool flag && flag;
        }
    }
}
