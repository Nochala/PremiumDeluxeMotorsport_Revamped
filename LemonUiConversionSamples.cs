using System;
using System.Drawing;
using GTA;
using GTA.UI;
using LemonUI;
using LemonUI.Elements;
using LemonUI.Menus;
using LemonUI.Scaleform;
using LemonUI.Tools;

using GtaFont = GTA.UI.Font;

namespace PDMCD4
{
    internal static class LemonUiConversionSamples
    {
        private static string Gxt(string key)
        {
            return Game.GetLocalizedString(key);
        }

        public static void SampleInstructionalButtons(NativeMenu menu)
        {
            menu.Buttons.Add(new InstructionalButton(Gxt("CMM_MOD_S6"), Control.ParachuteBrakeLeft));
            menu.Buttons.Add(new InstructionalButton(Gxt("CMOD_MOD_ROF"), Control.VehicleRoof));
            menu.Buttons.Add(new InstructionalButton(Gxt("CTRL_0"), Control.VehiclePushbikeSprint));
            menu.Buttons.Add(new InstructionalButton(Gxt("HUD_INPUT91"), Control.NextCamera));
        }

        public static ObjectPool CreatePoolWithMenus(out NativeMenu mainMenu, out NativeMenu customizeMenu)
        {
            var pool = new ObjectPool();

            mainMenu = new NativeMenu("", Gxt("CMOD_MOD_T"));
            customizeMenu = new NativeMenu("", Gxt("PERSO"));

            pool.Add(mainMenu);
            pool.Add(customizeMenu);

            var customizeItem = new NativeSubmenuItem(mainMenu, customizeMenu, "Customize");
            mainMenu.Add(customizeItem);

            mainMenu.ItemActivated += (sender, item) =>
            {
            };

            mainMenu.SelectedIndexChanged += (sender, args) =>
            {
            };

            return pool;
        }

        public static void SampleScaledText(string vehicleName, string vehicleClass)
        {
            PointF titlePos = SafeZone.GetPositionAt(new PointF(0.95f, 0.82f), Alignment.Right, GFXAlignment.Right);
            PointF classPos = SafeZone.GetPositionAt(new PointF(0.95f, 0.87f), Alignment.Right, GFXAlignment.Right);

            var title = new ScaledText(titlePos, vehicleName, 0.85f, GtaFont.ChaletComprimeCologne)
            {
                Alignment = Alignment.Right,
                Color = Color.White,
                Shadow = true,
            };

            var subtitle = new ScaledText(classPos, vehicleClass, 0.85f, GtaFont.HouseScript)
            {
                Alignment = Alignment.Right,
                Color = Color.DodgerBlue,
                Shadow = true,
            };

            title.Draw();
            subtitle.Draw();
        }

        public static bool IsCursorInsideMenuAreaApprox()
        {
            PointF topLeft = SafeZone.GetSafePosition(new PointF(0.0f, 0.0f));
            SizeF size = new SizeF(
                431f / GameScreen.AbsoluteResolution.Width,
                550f / GameScreen.AbsoluteResolution.Height);

            return GameScreen.IsCursorInArea(topLeft, size);
        }
    }
}