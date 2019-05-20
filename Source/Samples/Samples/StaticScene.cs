using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 1)]
    public class StaticScene : Sample
    {
        public override void Init()
        {
            Node node = new Node
            {
                Position = new Vector3(),

                Rotation = new Quaternion(),

                Components = new[]
                {
                    new Camera
                    {
                    }
                },

                Children = new[]
                {
                    new Node
                    {
                    }
                }
            };


        }
    }
}
