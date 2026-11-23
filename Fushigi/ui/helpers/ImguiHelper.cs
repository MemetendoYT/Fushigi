using Fushigi.course;
using Fushigi.ui.widgets;
using ImGuiNET;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.ui.helpers
{
    public class ImguiHelper
    {
        public static bool DrawTextToggle(string text, bool toggle, Vector2 size)
        {
            var color = toggle ? ImGui.GetStyle().Colors[(int)ImGuiCol.Text]
                               : ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled];

            ImGui.PushStyleColor(ImGuiCol.Text, color);

            bool pressed = ImGui.Button(text, size);

            ImGui.PopStyleColor();

            return pressed;
        }

        public static bool DrawDropdown(string text, ref string param, Dictionary<string, string> dict)
        {
            bool returnVal = false;
            ImGui.Text(text);
            ImGui.TableSetColumnIndex(1);
            string value = param;
            int index = dict.Values.ToList().IndexOf(value);
            if (ImGui.Combo($"##{text}_dropdown", ref index, dict.Keys.ToArray(), dict.Count(), 10))
            {
                param = dict.Values.ToArray()[index];

                returnVal = true;
            }

            ImGui.TableNextColumn();
            return returnVal;
        }
    }
}
