﻿using Barotrauma.Networking;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Barotrauma.Items.Components
{
    partial class ItemLabel : ItemComponent, IDrawableComponent
    {
        private CoroutineHandle sendStateCoroutine;
        private string lastSentText;
        private float sendStateTimer;

        [Serialize("", true, description: "The text to display on the label.", alwaysUseInstanceValues: true), Editable(100)]
        public string Text
        {
            get;
            set;
        }

        [Editable, Serialize("0,0,0,255", true, description: "The color of the text displayed on the label.", alwaysUseInstanceValues: true)]
        public Color TextColor
        {
            get;
            set;
        }
        
        [Editable, Serialize(1.0f, true, description: "The scale of the text displayed on the label.", alwaysUseInstanceValues: true)]
        public float TextScale
        {
            get;
            set;
        }

        [Serialize("0,0,0,0", true, description: "The amount of padding around the text in pixels (left,top,right,bottom).")]
        public Vector4 Padding
        {
            get;
            set;
        }

        public override void Move(Vector2 amount)
        {
            //do nothing
        }

        public ItemLabel(Item item, XElement element)
            : base(item, element)
        {
        }

        partial void OnStateChanged()
        {
            sendStateTimer = 0.1f;
            if (sendStateCoroutine == null)
            {
                sendStateCoroutine = CoroutineManager.StartCoroutine(SendStateAfterDelay());
            }
        }

        private IEnumerable<CoroutineStatus> SendStateAfterDelay()
        {
            while (sendStateTimer > 0.0f)
            {
                sendStateTimer -= CoroutineManager.DeltaTime;
                yield return CoroutineStatus.Running;
            }

            if (item.Removed || GameMain.NetworkMember == null)
            {
                yield return CoroutineStatus.Success;
            }

            sendStateCoroutine = null;
            if (lastSentText != Text) { item.CreateServerEvent(this); }
            yield return CoroutineStatus.Success;
        }

        public void ServerWrite(IWriteMessage msg, Client c, object[] extraData = null)
        {
            msg.Write(Text);
            lastSentText = Text;
        }
    }
}
