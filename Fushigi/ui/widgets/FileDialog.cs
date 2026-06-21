using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NativeFileDialogSharp;
using System.IO;

namespace Fushigi.ui.widgets
{
    public class FileDialog
    {
        public string SelectedPath { get; set; } = "";

        public bool ShowDialog(string title = "File Select")
        {
            DialogResult dialogResult = Dialog.FileOpen();
            SelectedPath = dialogResult.Path;
            return dialogResult.IsOk;
        }

        public bool ShowSaveDialog(string title = "Save File")
        {
            DialogResult dialogResult = Dialog.FileSave("bgyml");
            SelectedPath = dialogResult.Path;
            return dialogResult.IsOk;
        }
    }
}