﻿using FarseerPhysics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Subsurface
{
    class MonsterQuest : Quest
    {
        //string monsterName;

        string monsterFile;

        Character monster;

        public override string RadarLabel
        {
            get
            {
                return monster.SpeciesName;
            }
        }

        public override Vector2 RadarPosition
        {
            get
            {
                return monster.Position;
            }
        }

        public MonsterQuest(XElement element)
            : base(element)
        {
            //monsterName = ToolBox.GetAttributeString(element, "monstername", "");
            monsterFile = ToolBox.GetAttributeString(element, "monsterfile", "");
        }

        public override void Start(Level level)
        {
            Vector2 position = level.PositionsOfInterest[Rand.Int(level.PositionsOfInterest.Count)];

            monster = new Character(monsterFile, ConvertUnits.ToSimUnits(position+level.Position));
        }

        public override void End()
        {
            if (!monster.IsDead) return;
            
            GiveReward();

            completed = true;
        }
    }
}
