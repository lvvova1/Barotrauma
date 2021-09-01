﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace Barotrauma
{
    abstract partial class Mission
    {
        public readonly MissionPrefab Prefab;
        protected bool completed, failed;

        protected Level level;

        protected int state;
        public virtual int State
        {
            get { return state; }
            protected set
            {
                if (state != value)
                {
                    state = value;
                    TryTriggerEvents(state);
#if SERVER
                    GameMain.Server?.UpdateMissionState(this, state);
#endif
                    ShowMessage(State);
                }
            }
        }

        protected bool IsClient => GameMain.NetworkMember != null && GameMain.NetworkMember.IsClient;

        public readonly List<string> Headers;
        public readonly List<string> Messages;
        
        public string Name
        {
            get { return Prefab.Name; }
        }

        private readonly string successMessage;
        public virtual string SuccessMessage
        {
            get { return successMessage; }
            //private set { successMessage = value; }
        }

        private readonly string failureMessage;
        public virtual string FailureMessage
        {
            get { return failureMessage; }
            //private set { failureMessage = value; }
        }

        protected string description;
        public virtual string Description
        {
            get { return description; }
            //private set { description = value; }
        }

        protected string descriptionWithoutReward;

        public virtual bool AllowUndocking
        {
            get { return true; }
        }

        public virtual int Reward
        {
            get 
            {
                return Prefab.Reward;
            }
        }

        public Dictionary<string, float> ReputationRewards
        {
            get { return Prefab.ReputationRewards; }
        }

        public bool Completed
        {
            get { return completed; }
            set { completed = value; }
        }

        public bool Failed
        {
            get { return failed; }
        }

        public virtual bool AllowRespawn
        {
            get { return true; }
        }

        public virtual int TeamCount
        {
            get { return 1; }
        }

        public virtual SubmarineInfo EnemySubmarineInfo
        {
            get { return null; }
        }

        public virtual IEnumerable<Vector2> SonarPositions
        {
            get { return Enumerable.Empty<Vector2>(); }
        }
        
        public virtual string SonarLabel
        {
            get { return Prefab.SonarLabel; }
        }
        public string SonarIconIdentifier
        {
            get { return Prefab.SonarIconIdentifier; }
        }

        public readonly Location[] Locations;

        public int? Difficulty
        {
            get { return Prefab.Difficulty; }
        }

        private class DelayedTriggerEvent
        {
            public readonly MissionPrefab.TriggerEvent TriggerEvent;
            public float Delay;

            public DelayedTriggerEvent(MissionPrefab.TriggerEvent triggerEvent, float delay)
            {
                TriggerEvent = triggerEvent;
                Delay = delay;
            }
        }

        private List<DelayedTriggerEvent> delayedTriggerEvents = new List<DelayedTriggerEvent>();
           
        public Mission(MissionPrefab prefab, Location[] locations, Submarine sub)
        {
            System.Diagnostics.Debug.Assert(locations.Length == 2);

            Prefab = prefab;

            description = prefab.Description;
            successMessage = prefab.SuccessMessage;
            failureMessage = prefab.FailureMessage;
            Headers = new List<string>(prefab.Headers);
            Messages = new List<string>(prefab.Messages);

            Locations = locations;

            for (int n = 0; n < 2; n++)
            {
                string locationName = $"‖color:gui.orange‖{locations[n].Name}‖end‖";
                if (description != null) { description = description.Replace("[location" + (n + 1) + "]", locationName); }
                if (successMessage != null) { successMessage = successMessage.Replace("[location" + (n + 1) + "]", locationName); }
                if (failureMessage != null) { failureMessage = failureMessage.Replace("[location" + (n + 1) + "]", locationName); }
                for (int m = 0; m < Messages.Count; m++)
                {
                    Messages[m] = Messages[m].Replace("[location" + (n + 1) + "]", locationName);
                }
            }
            string rewardText = $"‖color:gui.orange‖{string.Format(CultureInfo.InvariantCulture, "{0:N0}", GetReward(sub))}‖end‖";
            if (description != null) 
            {
                descriptionWithoutReward = description;
                description = description.Replace("[reward]", rewardText); 
            }
            if (successMessage != null) { successMessage = successMessage.Replace("[reward]", rewardText); }
            if (failureMessage != null) { failureMessage = failureMessage.Replace("[reward]", rewardText); }
            for (int m = 0; m < Messages.Count; m++)
            {
                Messages[m] = Messages[m].Replace("[reward]", rewardText);
            }
        }

        public virtual void SetLevel(LevelData level) { }

        public static Mission LoadRandom(Location[] locations, string seed, bool requireCorrectLocationType, MissionType missionType, bool isSinglePlayer = false)
        {
            return LoadRandom(locations, new MTRandom(ToolBox.StringToInt(seed)), requireCorrectLocationType, missionType, isSinglePlayer);
        }

        public static Mission LoadRandom(Location[] locations, MTRandom rand, bool requireCorrectLocationType, MissionType missionType, bool isSinglePlayer = false)
        {
            List<MissionPrefab> allowedMissions = new List<MissionPrefab>();
            if (missionType == MissionType.None)
            {
                return null;
            }
            else
            {
                allowedMissions.AddRange(MissionPrefab.List.Where(m => ((int)(missionType & m.Type)) != 0));
            }

            allowedMissions.RemoveAll(m => isSinglePlayer ? m.MultiplayerOnly : m.SingleplayerOnly);            
            if (requireCorrectLocationType)
            {
                allowedMissions.RemoveAll(m => !m.IsAllowed(locations[0], locations[1]));
            }

            if (allowedMissions.Count == 0)
            {
                return null;
            }
            
            int probabilitySum = allowedMissions.Sum(m => m.Commonness);
            int randomNumber = rand.NextInt32() % probabilitySum;
            foreach (MissionPrefab missionPrefab in allowedMissions)
            {
                if (randomNumber <= missionPrefab.Commonness)
                {
                    return missionPrefab.Instantiate(locations, Submarine.MainSub);
                }
                randomNumber -= missionPrefab.Commonness;
            }

            return null;
        }

        public virtual int GetReward(Submarine sub)
        {
            return Prefab.Reward;
        }

        public void Start(Level level)
        {
            state = 0;
#if CLIENT
            shownMessages.Clear();
#endif
            delayedTriggerEvents.Clear();
            foreach (string categoryToShow in Prefab.UnhideEntitySubCategories)
            {
                foreach (MapEntity entityToShow in MapEntity.mapEntityList.Where(me => me.prefab?.HasSubCategory(categoryToShow) ?? false))
                {
                    entityToShow.HiddenInGame = false;
                }
            }
            this.level = level;
            TryTriggerEvents(0);
            StartMissionSpecific(level);
        }

        protected virtual void StartMissionSpecific(Level level) { }

        public void Update(float deltaTime)
        {
            for (int i = delayedTriggerEvents.Count - 1; i>=0;i--)
            {
                delayedTriggerEvents[i].Delay -= deltaTime;
                if (delayedTriggerEvents[i].Delay <= 0.0f)
                {
                    TriggerEvent(delayedTriggerEvents[i].TriggerEvent);
                    delayedTriggerEvents.RemoveAt(i);
                }
            }
            UpdateMissionSpecific(deltaTime);
        }

        protected virtual void UpdateMissionSpecific(float deltaTime) { }

        protected void ShowMessage(int missionState)
        {
            ShowMessageProjSpecific(missionState);
        }

        partial void ShowMessageProjSpecific(int missionState);


        private void TryTriggerEvents(int state)
        {
            foreach (var triggerEvent in Prefab.TriggerEvents)
            {
                if (triggerEvent.State == state)
                {
                    TryTriggerEvent(triggerEvent);
                }
            }
        }

        /// <summary>
        /// Triggers the event or adds it to the delayedTriggerEvents it if it has a delay
        /// </summary>
        private void TryTriggerEvent(MissionPrefab.TriggerEvent trigger)
        {
            if (trigger.CampaignOnly && GameMain.GameSession?.Campaign == null) { return; }
            if (trigger.Delay > 0)
            {
                if (!delayedTriggerEvents.Any(t => t.TriggerEvent == trigger))
                {
                    delayedTriggerEvents.Add(new DelayedTriggerEvent(trigger, trigger.Delay));
                }
            }
            else
            {
                TriggerEvent(trigger);
            }
        }

        /// <summary>
        /// Triggers the event immediately, ignoring any delays
        /// </summary>
        private void TriggerEvent(MissionPrefab.TriggerEvent trigger)
        {
            if (trigger.CampaignOnly && GameMain.GameSession?.Campaign == null) { return; }
            var eventPrefab = EventSet.GetAllEventPrefabs().Find(p => p.Identifier.Equals(trigger.EventIdentifier, StringComparison.OrdinalIgnoreCase));
            if (eventPrefab == null)
            {
                DebugConsole.ThrowError($"Mission \"{Name}\" failed to trigger an event (couldn't find an event with the identifier \"{trigger.EventIdentifier}\").");
                return;
            }
            if (GameMain.GameSession?.EventManager != null)
            {
                var newEvent = eventPrefab.CreateInstance();
                GameMain.GameSession.EventManager.ActiveEvents.Add(newEvent);
                newEvent.Init(true);
            }
        }

        /// <summary>
        /// End the mission and give a reward if it was completed successfully
        /// </summary>
        public virtual void End()
        {
            completed = true;
            if (Prefab.LocationTypeChangeOnCompleted != null)
            {
                ChangeLocationType(Prefab.LocationTypeChangeOnCompleted);
            }
            GiveReward();
        }

        public void GiveReward()
        {
            if (!(GameMain.GameSession.GameMode is CampaignMode campaign)) { return; }
            campaign.Money += GetReward(Submarine.MainSub);

            foreach (KeyValuePair<string, float> reputationReward in ReputationRewards)
            {
                if (reputationReward.Key.Equals("location", StringComparison.OrdinalIgnoreCase))
                {
                    Locations[0].Reputation.Value += reputationReward.Value;
                    Locations[1].Reputation.Value += reputationReward.Value;
                }
                else
                {
                    Faction faction = campaign.Factions.Find(faction1 => faction1.Prefab.Identifier.Equals(reputationReward.Key, StringComparison.OrdinalIgnoreCase));
                    if (faction != null) { faction.Reputation.Value += reputationReward.Value; }
                }
            }

            if (Prefab.DataRewards != null)
            {
                foreach (var (identifier, value, operation) in Prefab.DataRewards)
                {
                    SetDataAction.PerformOperation(campaign.CampaignMetadata, identifier, value, operation);
                }
            }
        }

        protected void ChangeLocationType(LocationTypeChange change)
        {
            if (change == null) { throw new ArgumentException(); }
            if (GameMain.GameSession.GameMode is CampaignMode && !IsClient)
            {
                int srcIndex = -1;
                for (int i = 0; i < Locations.Length; i++)
                {
                    if (Locations[i].Type.Identifier.Equals(change.CurrentType, StringComparison.OrdinalIgnoreCase))
                    {
                        srcIndex = i;
                        break;
                    }
                }
                if (srcIndex == -1) { return; }
                var location = Locations[srcIndex];

                if (change.RequiredDurationRange.X > 0)
                {
                    location.PendingLocationTypeChange = (change, Rand.Range(change.RequiredDurationRange.X, change.RequiredDurationRange.Y), Prefab);
                }
                else
                {
                    location.ChangeType(LocationType.List.Find(lt => lt.Identifier.Equals(change.ChangeToType, StringComparison.OrdinalIgnoreCase)));
                    location.LocationTypeChangeCooldown = change.CooldownAfterChange;
                }
            }
        }

        public virtual void AdjustLevelData(LevelData levelData) { }

        // putting these here since both escort and pirate missions need them. could be tucked away into another class that they can inherit from (or use composition)
        protected HumanPrefab GetHumanPrefabFromElement(XElement element)
        {
            if (element.Attribute("name") != null)
            {
                DebugConsole.ThrowError("Error in mission \"" + Name + "\" - use character identifiers instead of names to configure the characters.");

                return null;
            }

            string characterIdentifier = element.GetAttributeString("identifier", "");
            string characterFrom = element.GetAttributeString("from", "");
            HumanPrefab humanPrefab = NPCSet.Get(characterFrom, characterIdentifier);
            if (humanPrefab == null)
            {
                DebugConsole.ThrowError("Couldn't spawn character for mission: character prefab \"" + characterIdentifier + "\" not found");
                return null;
            }

            return humanPrefab;
        }

        protected Character CreateHuman(HumanPrefab humanPrefab, List<Character> characters, Dictionary<Character, List<Item>> characterItems, Submarine submarine, CharacterTeamType teamType, ISpatialEntity positionToStayIn = null, Rand.RandSync humanPrefabRandSync = Rand.RandSync.Server, bool giveTags = true)
        {
            var characterInfo = humanPrefab.GetCharacterInfo(Rand.RandSync.Server) ?? new CharacterInfo(CharacterPrefab.HumanSpeciesName, npcIdentifier: humanPrefab.Identifier, jobPrefab: humanPrefab.GetJobPrefab(humanPrefabRandSync), randSync: humanPrefabRandSync);
            characterInfo.TeamID = teamType;

            if (positionToStayIn == null) 
            {
                positionToStayIn = 
                    WayPoint.GetRandom(SpawnType.Human, characterInfo.Job?.Prefab, submarine) ??
                    WayPoint.GetRandom(SpawnType.Human, null, submarine);
            }

            Character spawnedCharacter = Character.Create(characterInfo.SpeciesName, positionToStayIn.WorldPosition, ToolBox.RandomSeed(8), characterInfo, createNetworkEvent: false);
            spawnedCharacter.Prefab = humanPrefab;
            humanPrefab.InitializeCharacter(spawnedCharacter, positionToStayIn);
            humanPrefab.GiveItems(spawnedCharacter, submarine, Rand.RandSync.Server, createNetworkEvents: false);

            characters.Add(spawnedCharacter);
            characterItems.Add(spawnedCharacter, spawnedCharacter.Inventory.FindAllItems(recursive: true));

            return spawnedCharacter;
        }
    }
}
