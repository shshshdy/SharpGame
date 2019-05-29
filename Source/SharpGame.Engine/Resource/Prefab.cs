using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class Prefab : Resource<Prefab>
    {
        public Node Node { get; set; }
    }
}
