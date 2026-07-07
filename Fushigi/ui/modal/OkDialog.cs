using Fushigi.util;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Fushigi.ui.modal
{
    public abstract class OkDialog<TDialog> : IPopupModal<OkDialog<TDialog>.Void>
        where TDialog : OkDialog<TDialog>, new()
    {
        protected struct Void { }
        protected abstract string Title { get; }
        public static async Task ShowDialog(IPopupModalHost modalHost)
        {
            var dialog = new TDialog();
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(600 * MainWindow.dpiScale, 350 * MainWindow.dpiScale), ImGuiCond.Always);
            await modalHost.ShowPopUp(
                dialog,
                dialog.Title,
                ImGuiWindowFlags.NoResize);
        }
        protected abstract void DrawBody(Promise<Void> promise);
        void IPopupModal<Void>.DrawModalContent(Promise<Void> promise)
        {
            DrawBody(promise);
        }
    }
}