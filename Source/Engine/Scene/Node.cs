using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class Node : Object
    {
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Scale { get; set; }


        public Node()
        {
        }

        public Node(Vector3 position, Vector3 rotation, Vector3 scale, params Component[] components)
        {

        }
    }
}
