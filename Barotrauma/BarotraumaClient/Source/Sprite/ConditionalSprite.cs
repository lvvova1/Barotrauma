﻿using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;

namespace Barotrauma
{
    partial class ConditionalSprite
    {
        public void Remove()
        {
            Sprite?.Remove();
            Sprite = null;
            DeformableSprite?.Remove();
            DeformableSprite = null;
        }
    }
}