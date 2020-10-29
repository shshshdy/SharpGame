using ImGuiNET;

namespace SharpGame.Samples
{
    [SampleDesc(sortOrder = 5)]
    public class ImGuiDemo : Sample
    {
        public override void OnGUI()
        {
            ImGui.ShowDemoWindow();
        }
    }

}
