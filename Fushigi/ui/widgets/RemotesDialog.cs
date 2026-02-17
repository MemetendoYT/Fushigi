using System.Numerics;
using Fushigi.ui.modal;
using Fushigi.util;
using ImGuiNET;

namespace Fushigi.ui.widgets;

public class RemotesDialog : IPopupModal<RemotesDialog.RemotesResult>
{
    public struct RemotesResult
    {
        public List<RemoteEntry> Remotes;
    }
    public struct RemoteEntry
    {
        public string name;
        public string url;
    }

    private int selectedRow = -1;
    private List<RemoteEntry>? _initial;

    public async Task<Dictionary<string,string>> Show(IPopupModalHost host, Dictionary<string,string> initialRemotes)
    {
        List<RemoteEntry> remotes = new List<RemoteEntry>();
        foreach (KeyValuePair<string, string> entry in initialRemotes)
        {
            RemoteEntry e = new RemoteEntry();
            e.name = entry.Key;
            e.url = entry.Value;
            remotes.Add(e);
        }
        _initial = remotes;
        
        (bool wasClosed, RemotesResult result) result =
            await host.ShowPopUp(this, "Git Remotes", ImGuiWindowFlags.None, new Vector2(300,450));
        if (result.wasClosed)
        {
            return initialRemotes;
        }
        Dictionary<string, string> newRemotes = new Dictionary<string, string>();
        if (result.result.Remotes.Count != 0)
        {
            foreach (RemoteEntry e in result.result.Remotes)
            {
                newRemotes[e.name] = e.url;
            }
        }
        else
        {
            newRemotes = initialRemotes;
        }

        return newRemotes;
    }
    public void DrawModalContent(Promise<RemotesResult> promise)
    {
        List<RemoteEntry> data = _initial ?? new List<RemoteEntry>();
        if (promise.TryGetResult(out RemotesResult result))
        {
            data = result.Remotes;
        }
        if (ImGui.BeginTable("toolbar",1,ImGuiTableFlags.Borders))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            if (ImGui.Button("Add##remotesToolbar"))
            {
                data.Add(new RemoteEntry {name = "", url = ""});
            }
            ImGui.SameLine();
            
            bool removeEnabled = selectedRow >= 0 && selectedRow < data.Count;
            if (!removeEnabled) { ImGui.BeginDisabled(); }
            if (ImGui.Button("Remove##remotesToolbar") && removeEnabled)
            {
                data.RemoveAt(selectedRow);
                selectedRow = -1;
            }
            if (!removeEnabled) { ImGui.EndDisabled(); }
            
            ImGui.SameLine();
            
            bool upEnabled = selectedRow > 0;
            if (!upEnabled) { ImGui.BeginDisabled(); }
            if (ImGui.Button("Up##remotesToolbar"))
            {
                (data[selectedRow], data[selectedRow-1]) = (data[selectedRow-1], data[selectedRow]);
            }
            if (!upEnabled) { ImGui.EndDisabled(); }
            
            ImGui.SameLine();
            ImGui.Button("Down##remotesToolbar");
            ImGui.EndTable();
        }
        bool selectedAny = false;
        if (ImGui.BeginTable("data",2,ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg, new Vector2(-1,ImGui.GetContentRegionAvail().Y-64)))
        {
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Url");
            ImGui.TableHeadersRow();

            for (int i = 0; i < data.Count; i++)
            {
                bool selected = selectedRow == i;
                
                ImGui.TableNextRow();
                
                if (selected)
                {
                    Vector4 vector4;
                    Vector4 def = new(0.16f, 0.29f, 0.48f, 0.54f);
                    unsafe
                    {
                        Vector4* col = ImGui.GetStyleColorVec4(ImGuiCol.FrameBg);
                        vector4 = (col != null) ? *col : def;
                    }
                    uint u32Col = ImGui.ColorConvertFloat4ToU32(vector4);
                    
                    Logger.Logger.LogDebug("Remotes Dialog", $"Color is: ${u32Col}");

                    ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0,u32Col);
                }
                
                ImGui.TableSetColumnIndex(0);
                RemoteEntry entry = data[i];
                string oldName = entry.name;
                ImGui.PushStyleColor(ImGuiCol.FrameBg,Vector4.Zero);
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGui.InputText($"##remotesDataTableName${i}", ref entry.name, 128))
                {
                    foreach (RemoteEntry other in data)
                    {
                        if (other.name == entry.name)
                        {
                            entry.name = oldName;
                            break;
                        }
                    }
                }
                if (ImGui.IsItemActive())
                {
                    selectedRow = i;
                    selectedAny = true;
                }
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                ImGui.InputText($"##remotesDataTableUrl${i}",ref entry.url,128);
                if (ImGui.IsItemActive())
                {
                    selectedRow = i;
                    selectedAny = true;
                }
                data[i] = entry;
                ImGui.PopStyleColor();
            }

            if (!selectedAny)
            {
                selectedRow = -1;
            }
            ImGui.EndTable();
        }

        if (ImGui.Button("Ok##remotesToolbar"))
        {
            promise.SetResult(new RemotesResult {Remotes = data});
        }
    }
}