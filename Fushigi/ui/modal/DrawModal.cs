//class SaveFailureAlert : OkDialog<SaveFailureAlert>
//{
//    protected override string Title => "Saving failed";
//    protected override void DrawBody(Promise<Void> promise)
//    {
//        ImGui.Text("The course files may be open in an external app, or Super Mario Bros. Wonder may currently be running in an emulator. \n" +
//            "Close the emulator or external app and try again.");

//        if (ImGui.Button("OK"))
//            promise.SetResult(new Void());
//    }
//}