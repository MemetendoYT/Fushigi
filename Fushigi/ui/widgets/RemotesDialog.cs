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
        foreach (RemoteEntry e in result.result.Remotes)
        {
            newRemotes[e.name] = e.url;
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
        if (ImGui.BeginTable("data",2,ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY))
        {
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Url");
            ImGui.TableHeadersRow();

            for (int i = 0; i < data.Count; i++)
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                RemoteEntry entry = data[i];
                string oldName = entry.name;
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
                ImGui.TableNextColumn();
                ImGui.InputText($"##remotesDataTableUrl${i}",ref entry.url,128);
                data[i] = entry;
            }
            ImGui.EndTable();
        }
    }
}