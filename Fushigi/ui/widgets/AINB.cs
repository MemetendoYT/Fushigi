using Fushigi.course;
using Fushigi.env;
using Fushigi.ui.modal;
using ImGuiNET;
using OpenAbility.ImGui.Nodes;
using Silk.NET.OpenGL;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fushigi.ui.widgets
{
    public class AINB
    {
        public List<AinbNode> Nodes { get; set; }
        public string Name { get; set; }

        public AINB()
        {
            string json = File.ReadAllText("res/test.json");

            Root data = JsonSerializer.Deserialize<Root>(json);

            Nodes = data.Nodes;

            Name = Nodes[0].NodeType;
        }
    }

}
public class Root
{
    public List<AinbNode> Nodes { get; set; }
}

public class AinbNode
{
    [JsonPropertyName("Node Type")]
    public string NodeType { get; set; }

    [JsonPropertyName("Node Index")]
    public int NodeIndex { get; set; }
    public string Name { get; set; }
    public string GUID { get; set; }

    public string LinkedNodes { get; set; }

    [JsonPropertyName("Input Parameters")]
    public InputParameters InputParameters { get; set; }

    [JsonPropertyName("Output Parameters")]
    public InputParameters OutputParameters { get; set; }
}


public class InputParameters
{
    [JsonPropertyName("float")]
    public List<InputParameter> Float { get; set; }
}

public class InputParameter
{
    public string Name { get; set; }

    [JsonPropertyName("Node Index")]
    public int NodeIndex { get; set; }

    [JsonPropertyName("Parameter Index")]
    public int ParameterIndex { get; set; }

    public float Value { get; set; }
}

public class OutputParameter
{
    public string Name { get; set; }

}

public class OutputParameters 
{
    public List<OutputParameter> Float { get; set; }

}

