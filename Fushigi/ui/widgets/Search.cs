using Fushigi.course;
using Fushigi.gl.Bfres;
using Fushigi.param;
using Fushigi.ui;
using Fushigi.ui.widgets;
using Fushigi.util;
using ImGuiNET;
using System.Numerics;

public class Search
{
    private static List<string> filteredActors = new();
    private static List<string> filteredLayers = new();

    private static string mActorSearchAll = "";
    private static string prevSearch;

    private static string mLayerSearch = "";
    private static string prevLayerSearch;
    private static int prevCallFrom = 0;

    private static bool regenerateActorsList;
    public static bool regenerateLayersList;

    public static string mSelectedActor;
    public static string mSelectedLayer;

    private static List<string> translatedActors = new();
    internal static void layerSearch(string[] layers, int callFrom, CourseScene cs)
    {
        var mLayersVisibility = CourseScene.mLayersVisibility;
        bool isSearch = !string.IsNullOrWhiteSpace(mLayerSearch);
        const int MaxLayerCount = 10;
        int layerCount = 0;

        if (prevCallFrom != callFrom)
            regenerateLayersList = true;
        if (prevLayerSearch != mLayerSearch || regenerateLayersList)
        {
            regenerateLayersList = false;
            filteredLayers.Clear();
            prevLayerSearch = mLayerSearch;

            foreach (var layer in layers)
            {
                bool HasText = layer.IndexOf(mLayerSearch, StringComparison.OrdinalIgnoreCase) >= 0;

                if (isSearch && !HasText)
                    continue;

                filteredLayers.Add(layer);
            }
            prevCallFrom = callFrom;
        }
        if (filteredLayers.Count == 0)
        {
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.Text("No layers found");
        }

        for (var i = 0; i < filteredLayers.Count; i++)
        {
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);

            var layer = filteredLayers[i];

            if (callFrom == 0)
            {
                layerCount = mLayersVisibility.Keys
                            .Count(x => x.StartsWith(layer) && FushigiConstants.NumberRegex.IsMatch(x.AsSpan(layer.Length..)));
                if (layer == "PlayArea" || layer == "DecoArea")
                    layer += $" ({layerCount}/{MaxLayerCount})";
            }

            ImGui.Selectable(layer);

            if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
            {
                mSelectedLayer = layer;
                regenerateLayersList = true;
            }

        }

        if (mSelectedLayer != null && mSelectedActor != null)
            cs.AddSelectedActorWithLayer(mSelectedActor, mSelectedLayer);
        else if (mSelectedLayer != null)
            cs.AddSelectedLayer(mSelectedLayer);
    }
    internal static void ActorSearch(CourseScene cs)
    {
        var ActorTypes = CourseScene.ActorTypes;
        var mLayersVisibility = CourseScene.mLayersVisibility;
        var ActorTypeEnabled = CourseScene.ActorTypeEnabled;
        ImGuiTableFlags flags = ImGuiTableFlags.BordersInnerV;

        int columnCount = 1;
        if (UserSettings.GetEnableTranslation())
            columnCount = 2;

        if (mSelectedActor == null)
        {
            ImGui.InputText("##ActorSearch", ref mActorSearchAll, 0x100);
            flags |= ImGuiTableFlags.Resizable;

            ImGui.SameLine();
            var paletteFlag = ImGuiComboFlags.NoArrowButton | ImGuiComboFlags.WidthFitPreview | ImGuiComboFlags.HeightLarge;
            if (ImGui.BeginCombo($"##ActorFilter", "  Filter  ", paletteFlag))
            {
                if (ImGui.Button("All", new Vector2(ImGui.GetColumnWidth(), 25 * MainWindow.dpiScale)))
                {
                    foreach (var actor in ActorTypes)
                    {
                        ActorTypeEnabled[actor] = true;
                    }
                    regenerateActorsList = true;
                }

                ImGui.BeginTable("ActorFilter", 2);
                foreach (var actor in ActorTypes)
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    bool filter = ActorTypeEnabled[actor];
                    if (ImGui.Checkbox($"{actor}", ref filter))
                    {
                        ActorTypeEnabled[actor] = filter;
                        regenerateActorsList = true;
                    }

                    ImGui.TableSetColumnIndex(1);
                    if (ImGui.Button($"Only##{actor}"))
                    {
                        foreach (var actorToHide in ActorTypes)
                        {
                            if (actorToHide != actor)
                                ActorTypeEnabled[actorToHide] = false;
                            else
                                ActorTypeEnabled[actorToHide] = true;
                        }
                        regenerateActorsList = true;
                    }
                }
                ImGui.EndTable();
                ImGui.EndCombo();
            }
        }
        else
        {
            ImGui.InputText("##LayerSearch", ref mLayerSearch, 0x100);
            flags &= ~ImGuiTableFlags.Resizable;
            columnCount = 1;
        }

        bool isSearch = !string.IsNullOrWhiteSpace(mActorSearchAll);

        if (mSelectedActor != null && mSelectedLayer != null)
            ImGui.Text($"Placing {mSelectedActor} on {mSelectedLayer}");

        if (prevSearch != mActorSearchAll || regenerateActorsList)
        {
            filteredActors.Clear();
            translatedActors.Clear();
            prevSearch = mActorSearchAll;

            foreach (var actor in ParamDB.GetActors())
            {

                bool isVanilla = BfresRender.VanillaActors.Contains(actor);
                bool isEnabled = ActorTypeEnabled[CourseActor.GetActorTypeFromGyaml(actor).ToString()];

                if (isVanilla && !isEnabled)
                    continue;
                if (!isVanilla && !ActorTypeEnabled["Custom"])
                    continue;

                var actorEnglish = Translate.FetchTranslatedName(actor);
                bool HasText = actor.IndexOf(mActorSearchAll, StringComparison.OrdinalIgnoreCase) >= 0 ||
                               actorEnglish.IndexOf(mActorSearchAll, StringComparison.OrdinalIgnoreCase) >= 0;

                if (isSearch && !HasText)
                    continue;

                filteredActors.Add(actor);
                translatedActors.Add(actorEnglish);
            }
            regenerateActorsList = false;
        }

        ImGui.BeginChild("ActorScroll", ImGui.GetContentRegionAvail());
        string enActor = "";

        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            ImGui.SetWindowFocus(null);

        if (filteredActors.Count == 0)
            ImGui.Text("No Actors found");

        if (ImGui.BeginTable("##ActorsAndLayers", columnCount, flags))
        {

            if (mSelectedActor == null)
            {
                int i = 0;
                foreach (string actor in filteredActors)
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    var actorType = CourseActor.GetActorTypeFromGyaml(actor);
                    uint color = CourseActor.CourseActorColors[CourseActorType.None];
                    CourseActor.CourseActorColors.TryGetValue(actorType, out color);
                    ImGui.PushStyleColor(ImGuiCol.Text, CourseActor.ColorShift(color));
                    if (UserSettings.GetEnableTranslation())
                    {
                        enActor = translatedActors[i];
                        ImGui.Selectable(enActor);
                        ImGui.PopStyleColor();
                    }
                    else
                    {
                        ImGui.Selectable(actor);
                        ImGui.PopStyleColor();
                    }

                    if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
                        mSelectedActor = actor;
                    if (UserSettings.GetEnableTranslation())
                    {
                        ImGui.TableSetColumnIndex(1);
                        ImGui.BeginDisabled();
                        ImGui.Text(actor);
                        ImGui.EndDisabled();
                    }
                    i++;
                }

            }
            else if (mSelectedLayer == null)
            {
                var layers = mLayersVisibility.Keys.ToArray().ToArray();
                layerSearch(layers, 1, cs);
            }

            ImGui.EndTable();
        }
        ImGui.EndChild();
    }

    internal static void DrawLayerSearch(CourseScene cs)
    {
        var mLayersVisibility = CourseScene.mLayersVisibility;
        ImGui.InputText("##LayerSearch", ref mLayerSearch, 0x100);
        ImGui.BeginChild("ActorScroll", ImGui.GetContentRegionAvail());

        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            ImGui.SetWindowFocus(null);


        if (ImGui.BeginTable("##Layers", 1, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
        {
            var layers = FushigiConstants.LayerTypes
              .Except(mLayersVisibility.Keys)
              .ToArray();

            layerSearch(layers, 0, cs);
            ImGui.EndTable();
            ImGui.EndChild();
        }
    }
}
