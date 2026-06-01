using Fushigi.ui.modal;
using Fushigi.util;
using ImGuiNET;

namespace Fushigi.ui.widgets
{
    class SavePrefabDialog : IPopupModal<SavePrefabDialog.SavePrefabResult>
    {
        public struct SavePrefabResult
        {
            public DialogResult Result;
            public string PrefabName;
        }
        public enum DialogResult
        {
            Yes,
            No
        }

        private string PrefabName = "";
        public static string modalText = "";
        public static async Task<SavePrefabResult> ShowDialog(IPopupModalHost modalHost, string title, string text)
        {
            modalText = text;
            var dialog = new SavePrefabDialog();

            var result = await modalHost.ShowPopUp(
                dialog,
                title,
                ImGuiWindowFlags.AlwaysAutoResize
            );

            if (result.wasClosed)
            {
                return new SavePrefabResult
                {
                    Result = DialogResult.No,
                    PrefabName = ""
                };
            }

            return new SavePrefabResult
            {
                Result = result.result.Result,
                PrefabName = dialog.PrefabName
            };
        }

        public void DrawModalContent(Promise<SavePrefabResult> promise)
        {
            ImGui.Text(modalText);
            ImGui.NewLine();

            ImGui.InputText("##Prefab", ref PrefabName, 0x100);

            float centerXButtons =
                (ImGui.GetWindowWidth() - ImGui.CalcTextSize("Yes Cancel").X) * 0.4f;

            ImGui.SetCursorPosX(centerXButtons);

            if (ImGui.Button("Save"))
            {
                promise.SetResult(new SavePrefabResult
                {
                    Result = DialogResult.Yes,
                    PrefabName = PrefabName
                });
            }

            ImGui.SameLine();

            if (ImGui.Button("Cancel"))
            {
                promise.SetResult(new SavePrefabResult
                {
                    Result = DialogResult.No,
                    PrefabName = PrefabName
                });
            }
        }
    }
}