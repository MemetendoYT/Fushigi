using Fushigi.course;
using Fushigi.env;
using Fushigi.gl;
using Fushigi.ui.modal;
using FuzzySharp.Edits;
using ImGuiNET;
using OpenAbility.ImGui.Nodes;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Fushigi.ui.widgets
{
    public class DrawNodeEditor
    {
        public List<string> Nodes = new List<string>();
        public static string editMode = "";
        public NodeEditor AinbEditor = new NodeEditor("AINB Editor");
        public AINB ainb = new AINB();
        public DrawNodeEditor()
        {
            // Do this once at startup
            var floatType = new PinType("Float", 255, 200, 0);

            foreach (var AinbNode in ainb.Nodes)
            {
                int i = 1;
                var newNode = new Node();
                AinbEditor.AddNode(newNode, AinbNode.NodeIndex);

                var name = AinbNode.Name;
                if(name == "")
                    name = AinbNode.NodeType;

                newNode.AddInput(floatType);
                newNode.ID = AinbNode.NodeIndex;
                Console.WriteLine(AinbNode.NodeIndex);
                if (AinbNode.InputParameters != null)
                {
                    foreach (var inputParams in AinbNode.InputParameters.Float)
                    {
                        newNode.AddInput(floatType);
                    }

                }

                if (AinbNode.OutputParameters != null)
                {
                    foreach(var outputParams in AinbNode.OutputParameters.Float)
                    {
                        newNode.AddOutput(floatType);
                    }
                }
                    newNode.name = name;
                newNode.Position = new Vector2(200 * i, 100);
                i++;
            }
            //var nodeA = new Node();
            
            //nodeA.Position = new Vector2(100, 100);
            //nodeA.AddInput(floatType);
            //nodeA.AddOutput(floatType);

            //var nodeB = new Node();
            //nodeB.name = "Bobognus2";
            //nodeB.Position = new Vector2(400, 100);
            //nodeB.AddInput(floatType);
            //nodeB.AddOutput(floatType);

            //AinbEditor.AddNode(nodeA);
            //AinbEditor.AddNode(nodeB);

            //NodePin outPin = nodeA.GetPins()[1]; // output
            //NodePin inPin = nodeB.GetPins()[0]; // input

            //outPin.Connect(inPin);
            ////AinbEditor.AddNode(node);

        }

        public void Draw()
        {
            DrawBalls();
            var size = ImGui.GetContentRegionAvail();
            NodeEditorDraw(size);
        }

        public void NodeEditorDraw(Vector2 size)
        {
            ImGui.Begin("viewport");
            var io = ImGui.GetIO();
            var drawList = ImGui.GetWindowDrawList();
            var nodePos = new Vector2(100, 100);

            AinbEditor.Draw();
            //foreach (var Node in Nodes)
            //{       
            //    ImGui.Text("hi");
            //    ImGui.NewLine();
            //    nodePos = new Vector2(nodePos.X + 300, nodePos.Y);

            //    // Dummy node size
            //    Vector2 nodeSize = new Vector2(150, 80);

            //    // Compute rect corners
            //    Vector2 nodeRectMin = nodePos;
            //    Vector2 nodeRectMax = nodePos + nodeSize;

            //    // Dummy color (RGBA)
            //    uint colour = ImGui.GetColorU32(new Vector4(0.2f, 0.6f, 1.0f, 1.0f));

            //    // Draw the filled rectangle
            //    drawList.AddRectFilled(nodeRectMin, nodeRectMax, colour, 4.0f);
            //    Vector2 textPos = new Vector2(
            //    nodeRectMin.X + 10,
            //    nodeRectMin.Y + 10
            //    );

            //    // Draw text inside the node
            //    drawList.AddText(textPos, ImGui.GetColorU32(new Vector4(1, 1, 1, 1)), Node);
            //    // Optional: outline
            //    drawList.AddRect(nodeRectMin, nodeRectMax, ImGui.GetColorU32(new Vector4(1, 1, 1, 1)), 4.0f, 0, 2.0f);
            //}


            //if (size.X * size.Y == 0)
            //    return;

            var mTopLeft = ImGui.GetCursorScreenPos();

            //ImGui.InvisibleButton("NodeEditor", size,
            //    ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight | ImGuiButtonFlags.MouseButtonMiddle);

            //var IsViewportHovered = IsViewportHovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
            //var IsViewportActive = ImGui.IsItemActive();

            // ProcessModifiers();

            var mSize = size;

            //var HandleCameraControls(deltaSeconds);

            //if (Camera.Width != mSize.X || Camera.Height != mSize.Y)
            //{
            //    Camera.Width = mSize.X;
            //    Camera.Height = mSize.Y;
            //}

            //if (!Camera.UpdateMatrices())
            //    return;

            //DrawGridLines(false, 20f, 10);
            //DrawGridLines(true, 20f, 10);

            ImGui.End();

        }

        private void DrawBalls()
        {
            ImGui.Begin("TestBalls");
            ImGui.Text("fisdjfgijdsfgij");
            ImGui.End();
        }
    }
}