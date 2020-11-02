﻿using SharpGame;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;


namespace SharpGame.Samples
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CameraVS
    {
        public mat4 View;
        public mat4 ViewInv;
        public mat4 ViewProj;
        public vec3 CameraPos;
        public float NearClip;
        public vec3 FrustumSize;
        public float FarClip;
    }

    [SampleDesc(sortOrder = -6)]
    public class NewRenderer : Sample
    {
        RenderPipeline renderer = new RenderPipeline();
        List<SourceBatch> batches = new List<SourceBatch>();

        CameraVS cameraVS = new CameraVS();

        ResourceSet resourceSet;

        SharedBuffer ubCameraVS;

        Geometry cube;
        vec3 cameraPos;
        const int COUNT = 100;

        public override void Init()
        {
            var resourceLayout = new ResourceLayout(0)
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Vertex),
                new ResourceLayoutBinding(1, DescriptorType.UniformBufferDynamic, ShaderStage.Vertex),
            };

            var mat = new Material("Shaders/Basic.shader");
            mat.SetTexture("DiffMap", Texture.White);

            cube = GeometricPrimitive.CreateCube(10, 10, 10);

            for(int i = 0; i < COUNT; i++)
            {
                var batch = new SourceBatch
                {
                    geometry = cube,
                    material = mat,
                    numWorldTransforms = 1,
                };

                batches.Add(batch);
            }

            ubCameraVS = new SharedBuffer(BufferUsageFlags.UniformBuffer, (uint) Utilities.SizeOf<CameraVS>());

            resourceSet = new ResourceSet(resourceLayout);

            renderer.AddGraphicsPass(CustomDraw);

            cameraPos = new vec3(0, 5, -50);
            
            MainView.Attach(null, null, renderer);

        }

        public override void Update()
        {
            UpdateInput();
        }

        private void UpdateInput()
        {
            var input = Input.Instance;
            if (input.snapshot == null)
            {
                return;
            }

            if (mousePos == vec2.Zero)
                mousePos = input.MousePosition;

            vec3 offset = default;
            if (input.IsMouseDown(MouseButton.Right))
            {
                vec2 delta = (input.MousePosition - mousePos) * Time.Delta * rotSpeed;
                yaw += delta.X;
                pitch += delta.Y;

                if (input.IsKeyPressed(Key.W))
                {
                    offset.z += 1.0f;
                }

                if (input.IsKeyPressed(Key.S))
                {
                    offset.z -= 1.0f;
                }

                if (input.IsKeyPressed(Key.A))
                {
                    offset.x -= 1.0f;
                }

                if (input.IsKeyPressed(Key.D))
                {
                    offset.x += 1.0f;
                }
            }

            if (input.IsMouseDown(MouseButton.Middle))
            {
                vec2 delta = input.MousePosition - mousePos;
                offset.x = delta.X;
                offset.y = delta.Y;
            }

            cameraPos += offset * (Time.Delta * moveSpeed);
            cameraPos += new vec3(0, 0, input.WheelDelta * wheelSpeed);

            mousePos = input.MousePosition;
        }

        void CustomDraw(GraphicsSubpass pass, RenderContext rc, CommandBuffer cmd)
        {
            var rs = resourceSet;// resourceSet[Graphics.WorkContext];

            var ub = ubCameraVS;

            mat4 rotM = glm.mat4(1.0f);
            
            rotM = glm.yawPitchRoll(yaw, pitch, 0);

            var m = glm.translate(cameraPos)* rotM ;

            cameraVS.View = glm.inverse(m);

            var proj = glm.perspective((float)Math.PI / 4, 16 / 9.0f, 1, 1000);
            proj.M22 = -proj.M22;

            cameraVS.ViewProj = proj * cameraVS.View ;
            ub.SetData(ref cameraVS);
            ub.Flush();

            rs.Bind(0, ub.Buffer);
            rs.Bind(1, FrameGraph.TransformBuffer[Graphics.WorkContext]);
            rs.UpdateSets();

            float gridSize = 15;

            for (int i = 0; i < COUNT; i++)
            {
                mat4 worldTransform = glm.translate(gridSize * (i / 10), 0, gridSize * (i % 10));

                batches[i].offset = (int)FrameGraph.Instance.GetTransform(Utilities.AsPointer(ref worldTransform), 1);
            }

            for (int i = 0; i < COUNT; i++)
            {
                var batch = batches[i];
                pass.DrawBatch(cmd, 1, batch, default, rs, null);
            }
        }
    }
}
