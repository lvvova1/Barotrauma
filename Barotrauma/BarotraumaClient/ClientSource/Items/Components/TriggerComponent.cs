﻿using Barotrauma.Networking;

namespace Barotrauma.Items.Components
{
    partial class TriggerComponent : ItemComponent, IServerSerializable
    {
        public void ClientRead(ServerNetObject type, IReadMessage msg, float sendingTime)
        {
            CurrentForceFluctuation = msg.ReadRangedSingle(0.0f, 1.0f, 8);
        }
    }
}
