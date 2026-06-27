using Fushigi.course;
using Fushigi.ui.modal;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Fushigi.ui.widgets
{
    internal class WorldMapSettings
    {

        public static void Draw(ref bool continueDisplay, IPopupModalHost modalHost, WorldMapInfo worldMapInfo)
        {
            ImGui.SetNextWindowSize(new Vector2(500 * MainWindow.dpiScale, 500 * MainWindow.dpiScale), ImGuiCond.Once);

            // Window
            if (ImGui.Begin("Course Settings", ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoCollapse))
            {
                // Close Button
                if (ImGui.Button("Close"))
                {
                    continueDisplay = false;
                }


                if (ImGui.BeginTabBar("Fart", ImGuiTabBarFlags.None))
                {
                    if (ImGui.BeginTabItem("WorldMap Settings"))
                    {
                        DrawWorldMapAppearanceSettings(worldMapInfo);
                        ImGui.EndTabItem();
                    }


                    ImGui.EndTabBar();
                }

                ImGui.End();
            }

        }


        private static void DrawWorldMapAppearanceSettings(WorldMapInfo worldMapInfo)
        {
            foreach (var gate in worldMapInfo.Gates)
            {

                if (ImGui.TreeNodeEx($"Gate_{gate.GateNo}", ImGuiTreeNodeFlags.SpanFullWidth, $"Gate {gate.GateNo}"))
                {
       
                    if (ImGui.BeginTable($"GateTable_{gate.GateNo}", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                    {
        
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.Text("Gate No");

                        ImGui.TableSetColumnIndex(1);
                        ImGui.Text($"{gate.GateNo}");

                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.Text("Initial Message:");

                        ImGui.TableSetColumnIndex(1);
                        var msg = gate.BalloonMsgLabel;
                        ImGui.InputText($"##Msg_{gate.GateNo}", ref msg, 256);
                        gate.BalloonMsgLabel = msg;

                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.Text("Price:");

                        ImGui.TableSetColumnIndex(1);
                        int price = gate.Price;
                        ImGui.DragInt($"##Price_{gate.GateNo}", ref price);
                        gate.Price = price;

  
                        ImGui.EndTable();
                    }

                    ImGui.TreePop();
                }
            }

          }
        }
    }
