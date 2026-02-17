using System.Numerics;
using Fushigi.ui.modal;
using Fushigi.util;
using ImGuiNET;

namespace Fushigi.ui.widgets;

public class CreateCommitDialog : IPopupModal<(string s, bool shouldPush)>
{
    private string _commitMessage = "";
    
    public async Task Show(IPopupModalHost host)
    {
        
        (bool wasClosed, (string commitMsg, bool shouldPush) result) result =
            await host.ShowPopUp(this, "Create Commit", ImGuiWindowFlags.None, new Vector2(300,450));
        if (result.wasClosed)
        {
            return;
        }

        GitRepository repo = GitRepository.GetInstance(UserSettings.GetModRomFSPath());
        repo.StageAll();
        string? sha = repo.Commit(result.result.commitMsg, UserSettings.GetGitUsername(), UserSettings.GetGitEmail());
        
        Logger.Logger.LogDebug("Create Commit", $"Created Commit with Hash {sha}");
        if (result.result.shouldPush)
        {
            repo.Push(username: UserSettings.GetGitUsername(), passwordOrToken: UserSettings.GetGitPasswordOrToken());
        }
    }
    public void DrawModalContent(Promise<(string s, bool shouldPush)> promise)
    {
        ImGui.InputTextMultiline("Commit Message", ref _commitMessage, 2048, new Vector2(-1, ImGui.GetWindowSize().Y - 128));
        if (ImGui.Button("Commit"))
        {
            promise.SetResult((_commitMessage, shouldPush: false));
        }
        ImGui.SameLine();
        if (ImGui.Button("Commit & Push"))
        {
            promise.SetResult((_commitMessage, shouldPush: true));
        }
    }
}