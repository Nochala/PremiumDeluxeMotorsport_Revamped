using System;
using System.Drawing;
using GTA;
using GTA.Native;
using GTA.UI;
using LemonUI.Elements;
using LemonUI.Tools;

namespace PDMCD4
{
    public class PDMeX : Script
    {
        public PDMeX()
        {
            Tick += PDMeX_Tick;
        }

        private void PDMeX_Tick(object sender, EventArgs e)
        {
            MenuHelper._menuPool?.Process();

            if (Helper.HideHud)
            {
                Function.Call(Hash.HIDE_HUD_AND_RADAR_THIS_FRAME);
                Function.Call(Hash.SHOW_HUD_COMPONENT_THIS_FRAME, 3);
                Function.Call(Hash.SHOW_HUD_COMPONENT_THIS_FRAME, 4);
                Function.Call(Hash.SHOW_HUD_COMPONENT_THIS_FRAME, 5);
                Function.Call(Hash.SHOW_HUD_COMPONENT_THIS_FRAME, 13);
                Helper.wsCamera.Update();
            }

            if (MenuHelper._menuPool != null && MenuHelper._menuPool.AreAnyVisible)
            {
                if (Helper.ShowVehicleName && !string.IsNullOrEmpty(Helper.VehicleName) && Helper.VehPreview != null && Helper.poly.IsInInterior(Helper.VehPreview.Position) && Helper.TaskScriptStatus == 0)
                {
                    PointF vnPos = SafeZone.GetPositionAt(new PointF(0.95f, 0.82f), GTA.UI.Alignment.Right, LemonUI.GFXAlignment.Right);
                    PointF vcPos = SafeZone.GetPositionAt(new PointF(0.95f, 0.87f), GTA.UI.Alignment.Right, LemonUI.GFXAlignment.Right);

                    GTA.UI.Font titleFont = GTA.UI.Font.ChaletComprimeCologne;
                    switch (Game.Language.ToString())
                    {
                        case "Chinese":
                        case "Korean":
                        case "Japanese":
                        case "ChineseSimplified":
                            titleFont = GTA.UI.Font.ChaletLondon;
                            break;
                    }

                    var vn = new ScaledText(vnPos, Helper.VehicleName, 0.85f, titleFont)
                    {
                        Alignment = GTA.UI.Alignment.Right,
                        Color = Color.White,
                        Shadow = true,
                    };
                    vn.Draw();

                    var vc = new ScaledText(vcPos, Helper.VehPreview.GetClassDisplayName(), 0.85f, GTA.UI.Font.HouseScript)
                    {
                        Alignment = GTA.UI.Alignment.Right,
                        Color = Color.DodgerBlue,
                        Shadow = true,
                    };
                    vc.Draw();
                }
            }
        }
    }
}
