﻿using Barotrauma.Networking;
using Microsoft.Xna.Framework;

namespace Barotrauma.Items.Components
{
    partial class ItemLabel : ItemComponent, IDrawableComponent, IServerSerializable
    {
        public Vector2 DrawSize
        {
            //use the extents of the item as the draw size
            get { return Vector2.Zero; }
        }

        partial void OnStateChanged();

        private string prevColorSignal;

        public override void ReceiveSignal(Signal signal, Connection connection)
        {
            switch (connection.Name)
            {
                case "set_text":
                    if (Text == signal.value) { return; }
                    Text = signal.value;
                    OnStateChanged();
                    break;
                case "set_text_color":
                    if (signal.value != prevColorSignal)
                    {
                        TextColor = XMLExtensions.ParseColor(signal.value, false);
                        prevColorSignal = signal.value;
                    }
                    break;
            }
        }
    }
}