using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using SharpGame;

namespace SharpGame.Samples
{
    public class SampleDescAttribute : System.Attribute
    {
        public string name;
        public string desc;
        public int sortOrder;
    }

    public class Sample : Object
    {
        public string Name { get; set; }

        protected Scene scene;
        protected Camera camera;
        protected vec2 mousePos = vec2.Zero;

        protected bool firstMode = true;
        protected float rotSpeed = 0.5f;
        protected float wheelSpeed = 1000.0f;
        protected float moveSpeed = 100.0f;
     
        public FileSystem FileSystem => FileSystem.Instance;
        public Resources Resources => Resources.Instance;
        public Graphics Graphics => Graphics.Instance;
        public FrameGraph FrameGraph => FrameGraph.Instance;
        public RenderView MainView => Application.Instance.MainView;

        public Sample()
        {
        }

        public virtual void Init()
        {
        }

        public virtual void Update()
        {
            if(camera == null)
            {
                return;
            }

            var input = Input.Instance;
            if(input.snapshot == null)
            {
                return;
            }

            if (mousePos == vec2.Zero)
                mousePos = input.MousePosition;

            vec2 delta_rotation = new(0);
            vec3 delta_translation = new(0.0f, 0.0f, 0.0f);
            if (firstMode)
            {
                vec2 delta = input.MousePosition - mousePos;
                if (input.IsMouseDown(MouseButton.Right))
                {
                    delta_rotation = delta * (Time.Delta * rotSpeed * new vec2(camera.AspectRatio, 1.0f));

                    if (input.IsKeyPressed(Key.W))
                    {
                        delta_translation.Z += 1.0f;
                    }

                    if (input.IsKeyPressed(Key.S))
                    {
                        delta_translation.Z -= 1.0f;
                    }

                    if (input.IsKeyPressed(Key.A))
                    {
                        delta_translation.X -= 1.0f;
                    }

                    if (input.IsKeyPressed(Key.D))
                    {
                        delta_translation.X += 1.0f;
                    }
                }


                if (input.IsMouseDown(MouseButton.Middle))
                {
                    delta_translation.X = -delta.X * camera.AspectRatio;
                    delta_translation.Y = delta.Y;
                }

                delta_translation *= (Time.Delta * moveSpeed);
                delta_translation.z += input.WheelDelta * wheelSpeed * Time.Delta;

                if (delta_rotation != glm.vec2(0.0f, 0.0f) || delta_translation != glm.vec3(0.0f, 0.0f, 0.0f))
                {
                    quat qx = glm.angleAxis(delta_rotation.y, glm.vec3(1.0f, 0.0f, 0.0f));
                    quat qy = glm.angleAxis(delta_rotation.x, glm.vec3(0.0f, 1.0f, 0.0f));

                    quat orientation = glm.normalize(qy * camera.Node.Rotation * qx);

                    camera.Node.Translate(delta_translation * glm.conjugate(orientation), TransformSpace.PARENT);
                    camera.Node.Rotation = orientation;
                }

            }
            else
            {
                if (input.IsMouseDown(MouseButton.Left))
                {
                    vec2 delta = (input.MousePosition - mousePos) * (Time.Delta * rotSpeed * 2 * new vec2(camera.AspectRatio, 1.0f));

                    camera.Node.RotateAround(vec3.Zero, glm.quatYawPitchRoll(delta.x, delta.y, 0), TransformSpace.WORLD);

                }

                camera.Node.Translate(new vec3(0, 0, input.WheelDelta * wheelSpeed), TransformSpace.LOCAL);
            }


            mousePos = input.MousePosition;
            
        }

        bool showHierarchy = false;
        bool showInspector = false;
        bool openCamera = true;
        Node selectedNode = null;
        public virtual void OnGUI()
        {
            if(camera)
            {
                if (ImGui.Begin("HUD"))
                {
                    if(ImGui.CollapsingHeader("Camera", ref openCamera))
                    {
                        ImGui.PushItemWidth(120);
                        ImGui.TextUnformatted("pos : " + camera.Node.Position.ToString("0.00"));
                        ImGui.TextUnformatted("rot : " + camera.Node.Rotation.EulerAngles.ToString("0.00"));
                        ImGui.SliderFloat("Rotate Speed: ", ref rotSpeed, 1, 100);
                        ImGui.SliderFloat("Move Speed: ", ref moveSpeed, 1, 1000);
                        ImGui.Checkbox("Show Hierarchy", ref showHierarchy); ImGui.SameLine();
                        ImGui.Checkbox("Show Inspector", ref showInspector);
                        ImGui.PopItemWidth();
                    }
                }

                ImGui.End();
             
            }

            Environment env = scene?.GetComponent<Environment>();
            if(env)
            {
                if (ImGui.Begin("HUD"))
                {
                    if (ImGui.CollapsingHeader("Lighting", ref openCamera))
                    {
                        env.AmbientColor = ImGUI.Color4("Ambient Color", env.AmbientColor);
                        env.SunlightColor = ImGUI.Color4("Sunlight Color", env.SunlightColor);

                        Vector3 dir = (Vector3)env.SunlightDir;
                        ImGui.SliderFloat3("Light dir", ref dir, -1.0f, 1.0f);
                        env.SunlightDir = (vec3)dir;
                    }
                }

                ImGui.End();
            }

            if (showHierarchy)
            {
                ShowHierarchy();
            }

            if (showInspector)
            {
                ShowInspector();
            }

            if(selectedModel != null)
            {
                ShowModel();
            }

        }

        uint dockspaceID;
        void ShowHierarchy()
        {
            if (ImGui.Begin("Hierarchy"))
            {
                Draw(scene, 0);

                //dockspaceID = ImGui.GetID("Hierarchy");
                //ImGui.DockSpace(dockspaceID, new Vector2(0, 0), ImGuiDockNodeFlags.PassthruCentralNode | ImGuiDockNodeFlags.NoResize);
                //ImGui.DockSpaceOverViewport();
            }

            ImGui.End();
            
        }

        void ShowInspector()
        {
            //ImGui.SetNextWindowDockID(dockspaceID, ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Inspector"))
            {
                Inspector(selectedNode);
            }

            ImGui.End();
        }

        Model selectedModel;
        void ShowModel()
        {
            if(selectedModel == null)
            {
                return;
            }

            if (ImGui.Begin("Model"))
            {
                foreach (var g in selectedModel.Geometries)
                {
                    if (g.Length == 0)
                    {
                        continue;
                    }

                    ImGui.Text(g[0].Name);
                }


            }

            ImGui.End();
        }

        public void Draw(Node node, int depth)
        {
            if(node == null)
                return;

            string name = node.Name ?? "";
            bool enable = node.Enabled;

            var flag = selectedNode == node ? ImGuiTreeNodeFlags.Selected : 0;
            bool collapse = ImGuiNET.ImGui.TreeNodeEx(name, ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.FramePadding | flag);

            //ImGuiNET.ImGui.SameLine(100);
            //ImGuiNET.ImGui.Checkbox("", ref enable); //ImGuiNET.ImGui.SameLine(500);
           

            if (ImGui.IsItemClicked())
            {
                selectedNode = node;
            }

            if (depth < 3 && !collapse)
            {
                ImGuiNET.ImGui.TreePush();
            }

            if (collapse)
            {
                foreach (var n in node.Children)
                {
                    Draw(n, depth + 1);
                }

                ImGuiNET.ImGui.TreePop();
            }


        }

        public void Inspector(Node node)
        {
            if (node == null)
                return;

            bool enable = node.Enabled;
            string name = node.Name ?? "";
            ImGuiNET.ImGui.Checkbox(name, ref enable); //ImGuiNET.ImGui.SameLine(500);

            foreach(var c in node.ComponentList)
            {
                bool open = true;
                if (ImGui.CollapsingHeader(c.GetType().Name, ref open))
                {
                    Inspector(c);
                }
            }
        }

        public void Inspector(Object obj)
        {
            if(obj is Node node)
            {
                Inspector(node);
            }
            else
            {
                var props = obj.GetType().GetProperties();
                foreach(var p in props)
                {
                    if(p.GetMethod == null)
                    {
                        continue;
                    }

                    var v = p.GetValue(obj);
                    switch (v)
                    {
                        case bool bVal:
                            ImGui.Value(p.Name, bVal);
                            break;
                        case int iVal:
                            ImGui.Value(p.Name, iVal);
                            break;
                        case uint iVal:
                            ImGui.Value(p.Name, iVal);
                            break;
                        case float fVal:
                            ImGui.Value(p.Name, fVal);
                            break;
                        case string strVal:
                            ImGui.Text(strVal);
                            break;
                        case Model m:
                            if (ImGui.Button(m.FileName??"..."))
                            {
                                selectedModel = m;
                            }
                            break;
                    }

                }

            }
        }

        protected override void Destroy(bool disposing)
        {
            MainView.Attach(null, null, null);
            camera = null;

            scene?.Dispose();
            scene = null;

            base.Destroy(disposing);
        }
    }


}
