using Barotrauma.Networking;
using System.Collections.Generic;

namespace Barotrauma
{
    class PvPMode : MissionMode
    {
        public PvPMode(GameModePreset preset, IEnumerable<MissionPrefab> missionPrefabs) : base(preset, ValidateMissionPrefabs(missionPrefabs, MissionPrefab.PvPMissionClasses)) { }

        public PvPMode(GameModePreset preset, MissionType missionType, string seed) : base(preset, ValidateMissionType(missionType, MissionPrefab.PvPMissionClasses), seed) { }

        public void AssignTeamIDs(IEnumerable<Client> clients)
        {
            int teamWeight = 0;
            List<Client> randList = new List<Client>(clients);
            for (int i = 0; i < randList.Count; i++)
            {
                if (randList[i].PreferredTeam == CharacterTeamType.FriendlyNPC ||
                    randList[i].PreferredTeam == CharacterTeamType.Team2)
                {
                    randList[i].TeamID = randList[i].PreferredTeam;
                    teamWeight += randList[i].PreferredTeam == CharacterTeamType.FriendlyNPC ? -1 : 1;
                    randList.RemoveAt(i);
                    i--;
                }
            }
            for (int i = 0; i<randList.Count; i++)
                {
                Client a = randList[i];
                int oi = Rand.Range(0, randList.Count - 1);
                Client b = randList[oi];
                randList[i] = b;
                randList[oi] = a;
            }
            int halfPlayers = (randList.Count / 2) + teamWeight;
            for (int i = 0; i < randList.Count; i++)
            {
                if (i < halfPlayers)
                {
                    randList[i].TeamID = CharacterTeamType.FriendlyNPC;
                }
                else
                {
                    randList[i].TeamID = CharacterTeamType.Team2;
                }
            }
        }
    }
}
