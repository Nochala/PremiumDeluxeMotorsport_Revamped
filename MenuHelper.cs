using System;
using System.Collections.Generic;
using GTA;
using GTA.UI;
using GtaScreen = GTA.UI.Screen;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
using LemonUI.Scaleform;
using LemonUI.Tools;

namespace PDMCD4
{
    public static class MenuHelper
    {
        public static NativeMenu MainMenu;
        public static NativeMenu ConfirmMenu;
        public static NativeMenu CustomiseMenu;
        public static NativeMenu VehicleMenu;
        public static NativeMenu PriColorMenu;
        public static NativeMenu ClassicColorMenu;
        public static NativeMenu MetallicColorMenu;
        public static NativeMenu MetalColorMenu;
        public static NativeMenu MatteColorMenu;
        public static NativeMenu ChromeColorMenu;
        public static NativeMenu PeaColorMenu;
        public static NativeMenu CPriColorMenu;
        public static NativeMenu ColorMenu;
        public static NativeMenu SecColorMenu;
        public static NativeMenu ClassicColorMenu2;
        public static NativeMenu MetallicColorMenu2;
        public static NativeMenu MetalColorMenu2;
        public static NativeMenu MatteColorMenu2;
        public static NativeMenu ChromeColorMenu2;
        public static NativeMenu CSecColorMenu;
        public static NativeMenu PlateMenu;

        public static NativeItem itemCat;
        public static NativeItem ItemCustomize;
        public static NativeItem ItemConfirm;
        public static NativeItem ItemColor;
        public static NativeItem ItemClassicColor;
        public static NativeItem ItemClassicColor2;
        public static NativeItem ItemMetallicColor;
        public static NativeItem ItemMetallicColor2;
        public static NativeItem ItemMetalColor;
        public static NativeItem ItemMetalColor2;
        public static NativeItem ItemMatteColor;
        public static NativeItem ItemMatteColor2;
        public static NativeItem ItemChromeColor;
        public static NativeItem ItemChromeColor2;
        public static NativeItem ItemCPriColor;
        public static NativeItem ItemCSecColor;
        public static NativeItem ItemPriColor;
        public static NativeItem ItemSecColor;
        public static NativeItem ItemPeaColor;
        public static NativeItem ItemPlate;

        public static string[] Parameters = { "[name]", "[price]", "[model]", "[gxt]", "[make]" };
        public static ObjectPool _menuPool;

        private static string Gxt(string key) => Game.GetLocalizedString(key);

        private static string LocalizedLicensePlate(LicensePlateStyle plateStyle)
        {
            switch (plateStyle)
            {
                case LicensePlateStyle.BlueOnWhite1:
                    return Gxt("CMOD_PLA_0");
                case LicensePlateStyle.BlueOnWhite2:
                    return Gxt("CMOD_PLA_1");
                case LicensePlateStyle.BlueOnWhite3:
                    return Gxt("CMOD_PLA_2");
                case LicensePlateStyle.YellowOnBlue:
                    return Gxt("CMOD_PLA_3");
                case LicensePlateStyle.YellowOnBlack:
                    return Gxt("CMOD_PLA_4");
                case LicensePlateStyle.NorthYankton:
                    return Gxt("CMOD_MOD_GLD2");
                default:
                    return plateStyle.ToString();
            }
        }

        private static bool ContainsIgnoreCase(string value, string fragment)
        {
            return !string.IsNullOrEmpty(value) && value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void FadeOut(int time) => GtaScreen.FadeOut(time);

        private static void FadeIn(int time) => GtaScreen.FadeIn(time);

        private static VehicleModCollection Mods(Vehicle vehicle) => vehicle.Mods;

        private static void AddInstructionalButtonIfValid(NativeMenu menu, InstructionalButton button)
        {
            if (!string.IsNullOrEmpty(button.Description))
            {
                menu.Buttons.Add(button);
            }
        }


        public static void CreateMenus()
        {
            ItemCustomize = new NativeItem(Helper.GetLangEntry("BTN_CUSTOMIZE"));
            ItemConfirm = new NativeItem(Gxt("ITEM_YES"));
            ItemColor = new NativeItem(Gxt("IB_COLOR"), Gxt("CMOD_MOD_6_D"));
            ItemClassicColor = new NativeItem(Gxt("CMOD_COL1_1"), Gxt("CMOD_MOD_6_D"));
            ItemClassicColor2 = new NativeItem(Gxt("CMOD_COL1_1"), Gxt("CMOD_MOD_6_D"));
            ItemMetallicColor = new NativeItem(Gxt("CMOD_COL1_3"), Gxt("CMOD_MOD_6_D"));
            ItemMetallicColor2 = new NativeItem(Gxt("CMOD_COL1_3"), Gxt("CMOD_MOD_6_D"));
            ItemMetalColor = new NativeItem(Gxt("CMOD_COL1_4"), Gxt("CMOD_MOD_6_D"));
            ItemMetalColor2 = new NativeItem(Gxt("CMOD_COL1_4"), Gxt("CMOD_MOD_6_D"));
            ItemMatteColor = new NativeItem(Gxt("CMOD_COL1_5"), Gxt("CMOD_MOD_6_D"));
            ItemMatteColor2 = new NativeItem(Gxt("CMOD_COL1_5"), Gxt("CMOD_MOD_6_D"));
            ItemChromeColor = new NativeItem(Gxt("CMOD_COL1_0"), Gxt("CMOD_MOD_6_D"));
            ItemChromeColor2 = new NativeItem(Gxt("CMOD_COL1_0"), Gxt("CMOD_MOD_6_D"));
            ItemCPriColor = new NativeItem(Helper.GetLangEntry("BTN_CUSTOM_PRIMARY"), Gxt("CMOD_MOD_6_D"));
            ItemCSecColor = new NativeItem(Helper.GetLangEntry("BTN_CUSTOM_SECONDARY"), Gxt("CMOD_MOD_6_D"));
            ItemPriColor = new NativeItem(Gxt("CMOD_COL0_0"), Gxt("CMOD_MOD_6_D"));
            ItemSecColor = new NativeItem(Gxt("CMOD_COL0_1"), Gxt("CMOD_MOD_6_D"));
            ItemPeaColor = new NativeItem(Gxt("CMOD_COL1_6"), Gxt("CMOD_MOD_6_D"));
            ItemPlate = new NativeItem(Gxt("CMOD_MOD_PLA"), Gxt("CMOD_MOD_6_D"));

            CreateCategoryMenu();
            CreateConfirmMenu();
            CreateCustomizeMenu();
            CreateColorCategory();
            PlateMenu = NewMenu(Gxt("CMOD_MOD_PLA"), true, CustomiseMenu, ItemPlate);
            CreatePrimaryColor();
            CreateSecondaryColor();
            CPriColorMenu = NewMenu(Helper.GetLangEntry("BTN_CUSTOM_PRIMARY"), true, ColorMenu, ItemCPriColor);
            CSecColorMenu = NewMenu(Helper.GetLangEntry("BTN_CUSTOM_SECONDARY"), true, ColorMenu, ItemCSecColor);
            ClassicColorMenu = NewMenu(Gxt("CMOD_COL1_1"), true, PriColorMenu, ItemClassicColor);
            MetallicColorMenu = NewMenu(Gxt("CMOD_COL1_3"), true, PriColorMenu, ItemMetallicColor);
            MetalColorMenu = NewMenu(Gxt("CMOD_COL1_4"), true, PriColorMenu, ItemMetalColor);
            MatteColorMenu = NewMenu(Gxt("CMOD_COL1_5"), true, PriColorMenu, ItemMatteColor);
            ChromeColorMenu = NewMenu(Gxt("CMOD_COL1_0"), true, PriColorMenu, ItemChromeColor);
            PeaColorMenu = NewMenu(Gxt("CMOD_COL1_6"), true, PriColorMenu, ItemPeaColor);
            ClassicColorMenu2 = NewMenu(Gxt("CMOD_COL1_1"), true, SecColorMenu, ItemClassicColor2);
            MetallicColorMenu2 = NewMenu(Gxt("CMOD_COL1_3"), true, SecColorMenu, ItemMetallicColor2);
            MetalColorMenu2 = NewMenu(Gxt("CMOD_COL1_4"), true, SecColorMenu, ItemMetalColor2);
            MatteColorMenu2 = NewMenu(Gxt("CMOD_COL1_5"), true, SecColorMenu, ItemMatteColor2);
            ChromeColorMenu2 = NewMenu(Gxt("CMOD_COL1_0"), true, SecColorMenu, ItemChromeColor2);
        }

        private static NativeMenu NewMenu(string title, bool showStats)
        {
            NativeMenu menu = new NativeMenu(string.Empty, title)
            {
                MouseBehavior = MenuMouseBehavior.Disabled,
            };
            AddInstructionalButtons(menu);
            _menuPool ??= new ObjectPool();
            _menuPool.Add(menu);
            return menu;
        }

        private static NativeMenu NewMenu(string title, bool showStats, NativeMenu parentMenu, NativeItem parentItem)
        {
            NativeMenu menu = NewMenu(title, showStats);
            NativeSubmenuItem sub = new NativeSubmenuItem(menu, parentMenu, parentItem.Title);
            sub.Tag = parentItem.Tag;
            parentMenu.Add(sub);
            menu.Closed += (sender, args) => ModsMenuCloseHandler(sender as NativeMenu);
            menu.ItemActivated += (sender, args) => ModsMenuItemSelectHandler(sender as NativeMenu, args.Item, (sender as NativeMenu)?.SelectedIndex ?? 0);
            menu.SelectedIndexChanged += (sender, args) => ModsMenuIndexChangedHandler(sender as NativeMenu, args.Index);
            return menu;
        }

        private static void AddInstructionalButtons(NativeMenu menu)
        {
            AddInstructionalButtonIfValid(menu, Helper.BtnRotLeft);
            AddInstructionalButtonIfValid(menu, Helper.BtnRotRight);
            AddInstructionalButtonIfValid(menu, Helper.BtnCamera);
            AddInstructionalButtonIfValid(menu, Helper.BtnZoom);
        }

        private static void ResetSelection(NativeMenu menu)
        {
            if (menu == null || menu.Items.Count == 0)
            {
                return;
            }
            if (menu.SelectedIndex < 0 || menu.SelectedIndex >= menu.Items.Count)
            {
                menu.SelectedIndex = 0;
            }
        }

        public static void CreateCategoryMenu()
        {
            MainMenu = NewMenu(Gxt("CMOD_MOD_T"), true);
            foreach (string file in System.IO.Directory.GetFiles(@".\scripts\PremiumDeluxeMotorsport\Vehicles\", "*.ini"))
            {
                if (System.IO.File.Exists(file))
                {
                    itemCat = new NativeItem(Helper.GetLangEntry(System.IO.Path.GetFileNameWithoutExtension(file)));
                    itemCat.Tag = Tuple.Create(System.IO.File.ReadAllLines(file).Length, System.IO.Path.GetFileNameWithoutExtension(file));
                    MainMenu.Add(itemCat);
                }
            }
            ResetSelection(MainMenu);
            MainMenu.ItemActivated += (sender, args) => CategoryItemSelectHandler(sender as NativeMenu, args.Item, (sender as NativeMenu)?.SelectedIndex ?? 0);
            MainMenu.Closed += (sender, args) => MenuCloseHandler(sender as NativeMenu);
        }

        public static void CreateConfirmMenu()
        {
            ConfirmMenu = NewMenu(Helper.GetLangEntry("PURCHASE_ORDER"), true);
            ConfirmMenu.Add(ItemCustomize);
            ConfirmMenu.Add(new NativeItem(Helper.GetLangEntry("BTN_TEST_DRIVE")));
            ConfirmMenu.Add(new NativeItem(Gxt("ITEM_YES")));
            ResetSelection(ConfirmMenu);
            ConfirmMenu.Closed += (sender, args) => ConfirmCloseHandler(sender as NativeMenu);
            ConfirmMenu.ItemActivated += (sender, args) => ItemSelectHandler(sender as NativeMenu, args.Item, (sender as NativeMenu)?.SelectedIndex ?? 0);
        }

        public static void CreateCustomizeMenu()
        {
            CustomiseMenu = NewMenu(Helper.GetLangEntry("BTN_CUSTOMIZE").ToUpperInvariant(), true, ConfirmMenu, ItemCustomize);
            CustomiseMenu.Add(ItemColor);
            CustomiseMenu.Add(new NativeItem(Gxt("PERSO_MOD_PER"), Gxt("IE_MOD_OBJ4")));
            CustomiseMenu.Add(new NativeItem(Helper.GetLangEntry("BTN_PLATE_NUMBER_NAME"), Gxt("IE_MOD_OBJ2")));
            CustomiseMenu.Add(ItemPlate);
            ResetSelection(CustomiseMenu);
            CustomiseMenu.ItemActivated += (sender, args) => ItemSelectHandler(sender as NativeMenu, args.Item, (sender as NativeMenu)?.SelectedIndex ?? 0);
        }

        public static void CreateColorCategory()
        {
            ColorMenu = NewMenu(Gxt("CMOD_COL1_T"), true, CustomiseMenu, ItemColor);
            ColorMenu.Add(ItemPriColor);
            ColorMenu.Add(ItemSecColor);
            ColorMenu.Add(ItemCPriColor);
            ColorMenu.Add(ItemCSecColor);
            ResetSelection(ColorMenu);
        }

        public static void CreatePrimaryColor()
        {
            PriColorMenu = NewMenu(Gxt("CMOD_COL2_T"), true, ColorMenu, ItemPriColor);
            PriColorMenu.Add(ItemClassicColor);
            PriColorMenu.Add(ItemMetallicColor);
            PriColorMenu.Add(ItemMatteColor);
            PriColorMenu.Add(ItemMetalColor);
            PriColorMenu.Add(ItemChromeColor);
            PriColorMenu.Add(ItemPeaColor);
            ResetSelection(PriColorMenu);
        }

        public static void CreateSecondaryColor()
        {
            SecColorMenu = NewMenu(Gxt("CMOD_COL3_T"), true, ColorMenu, ItemSecColor);
            SecColorMenu.Add(ItemClassicColor2);
            SecColorMenu.Add(ItemMetallicColor2);
            SecColorMenu.Add(ItemMatteColor2);
            SecColorMenu.Add(ItemMetalColor2);
            SecColorMenu.Add(ItemChromeColor2);
            ResetSelection(SecColorMenu);
        }

        public static void MenuCloseHandler(NativeMenu sender)
        {
            try
            {
                Helper.TaskScriptStatus = -1;
                if (Helper.SelectedVehicle != null)
                {
                    Helper.SelectedVehicle = null;
                    Helper.VehPreview?.Delete();
                }
                Helper.wsCamera.Stop();
                Helper.DrawSpotLight = false;
                Helper.HideHud = false;
                Helper.VehicleName = null;
                Helper.ShowVehicleName = false;
                ResetSelection(CustomiseMenu);
                ResetSelection(ConfirmMenu);
                ResetSelection(MainMenu);
            }
            catch (Exception ex)
            {
                logger.Log(ex.Message + " " + ex.StackTrace);
            }
        }

        public static void ConfirmCloseHandler(NativeMenu sender)
        {
            try
            {
                MainMenu.Visible = true;
                ResetSelection(CustomiseMenu);
                ResetSelection(ConfirmMenu);
                ResetSelection(MainMenu);
            }
            catch (Exception ex)
            {
                logger.Log(ex.Message + ex.StackTrace);
            }
        }

        private static void MarkSelected(NativeItem item, bool selected)
        {
            if (item != null)
            {
                item.AltTitle = selected ? "●" : string.Empty;
            }
        }

        public static void RefreshColorMenuFor(NativeMenu menu, List<VehicleColor> colorList, string prisecpear)
        {
            try
            {
                menu.Items.Clear();
                foreach (VehicleColor col in colorList)
                {
                    NativeItem item = new NativeItem(Helper.GetLocalizedColorName(col))
                    {
                        Tag = col,
                    };
                    if (prisecpear == "Primary")
                    {
                        MarkSelected(item, Mods(Helper.VehPreview).PrimaryColor == col);
                    }
                    else if (prisecpear == "Secondary")
                    {
                        MarkSelected(item, Mods(Helper.VehPreview).SecondaryColor == col);
                    }
                    else if (prisecpear == "Pearlescent")
                    {
                        MarkSelected(item, Mods(Helper.VehPreview).PearlescentColor == col);
                    }
                    menu.Add(item);
                }
                ResetSelection(menu);
            }
            catch (Exception ex)
            {
                logger.Log(ex.Message + " " + ex.StackTrace);
            }
        }

        public static void RefreshRGBColorMenuFor(NativeMenu menu, string category)
        {
            try
            {
                menu.Items.Clear();
                HashSet<string> removeList = new HashSet<string>
                {
                    "R", "G", "B", "A", "IsKnownColor", "IsEmpty", "IsNamedColor", "IsSystemColor", "Name", "Transparent",
                };
                foreach (System.Reflection.PropertyInfo col in typeof(System.Drawing.Color).GetProperties())
                {
                    if (removeList.Contains(col.Name))
                    {
                        continue;
                    }
                    NativeItem item = new NativeItem(System.Text.RegularExpressions.Regex.Replace(col.Name, "[A-Z]", " $0").Trim())
                    {
                        Tag = System.Drawing.Color.FromName(col.Name),
                    };
                    if (category == "Primary")
                    {
                        MarkSelected(item, Mods(Helper.VehPreview).CustomPrimaryColor == System.Drawing.Color.FromName(col.Name));
                    }
                    else if (category == "Secondary")
                    {
                        MarkSelected(item, Mods(Helper.VehPreview).CustomSecondaryColor == System.Drawing.Color.FromName(col.Name));
                    }
                    menu.Add(item);
                }
                ResetSelection(menu);
            }
            catch (Exception ex)
            {
                logger.Log(ex.Message + " " + ex.StackTrace);
            }
        }

        public static void RefreshEnumModMenuFor(NativeMenu menu, Helper.EnumTypes enumType)
        {
            try
            {
                menu.Items.Clear();
                switch (enumType)
                {
                    case Helper.EnumTypes.NumberPlateType:
                        foreach (LicensePlateStyle enumItem in Enum.GetValues(typeof(LicensePlateStyle)))
                        {
                            NativeItem item = new NativeItem(LocalizedLicensePlate(enumItem)) { Tag = enumItem };
                            MarkSelected(item, Mods(Helper.VehPreview).LicensePlateStyle == enumItem);
                            menu.Add(item);
                        }
                        break;
                    case Helper.EnumTypes.VehicleWindowTint:
                        foreach (VehicleWindowTint enumItem in Enum.GetValues(typeof(VehicleWindowTint)))
                        {
                            NativeItem item = new NativeItem(Helper.LocalizedWindowsTint(enumItem)) { Tag = enumItem };
                            MarkSelected(item, Mods(Helper.VehPreview).WindowTint == enumItem);
                            menu.Add(item);
                        }
                        break;
                    case Helper.EnumTypes.VehicleColorTrim:
                        foreach (VehicleColor enumItem in Enum.GetValues(typeof(VehicleColor)))
                        {
                            NativeItem item = new NativeItem(Helper.GetLocalizedColorName(enumItem)) { Tag = enumItem };
                            MarkSelected(item, Mods(Helper.VehPreview).TrimColor == enumItem);
                            menu.Add(item);
                        }
                        break;
                    case Helper.EnumTypes.VehicleColorDashboard:
                        foreach (VehicleColor enumItem in Enum.GetValues(typeof(VehicleColor)))
                        {
                            NativeItem item = new NativeItem(Helper.GetLocalizedColorName(enumItem)) { Tag = enumItem };
                            MarkSelected(item, Mods(Helper.VehPreview).DashboardColor == enumItem);
                            menu.Add(item);
                        }
                        break;
                    case Helper.EnumTypes.VehicleColorRim:
                        foreach (VehicleColor enumItem in Enum.GetValues(typeof(VehicleColor)))
                        {
                            NativeItem item = new NativeItem(Helper.GetLocalizedColorName(enumItem)) { Tag = enumItem };
                            MarkSelected(item, Mods(Helper.VehPreview).RimColor == enumItem);
                            menu.Add(item);
                        }
                        break;
                }
                ResetSelection(menu);
            }
            catch (Exception ex)
            {
                logger.Log(ex.Message + " " + ex.StackTrace);
            }
        }

        public static void VehicleSelectHandler(NativeMenu sender, NativeItem selectedItem, int index)
        {
            if (selectedItem == null)
            {
                return;
            }

            if (selectedItem.Title == Helper.VehicleName)
            {
                Tuple<string, int, string, string> t = (Tuple<string, int, string, string>)selectedItem.Tag;
                sender.Visible = false;
                ConfirmMenu.Visible = true;
                Helper.VehicleName = selectedItem.Title;
                Helper.optLastVehMake = t.Item4;
                Helper.ShowVehicleName = true;
                RefreshRGBColorMenuFor(CPriColorMenu, "Primary");
                RefreshRGBColorMenuFor(CSecColorMenu, "Secondary");
                RefreshColorMenuFor(ClassicColorMenu, Helper.ClassicColor, "Primary");
                RefreshColorMenuFor(MetallicColorMenu, Helper.ClassicColor, "Primary");
                RefreshColorMenuFor(MetalColorMenu, Helper.MetalColor, "Primary");
                RefreshColorMenuFor(MatteColorMenu, Helper.MatteColor, "Primary");
                RefreshColorMenuFor(ChromeColorMenu, Helper.ChromeColor, "Primary");
                RefreshColorMenuFor(PeaColorMenu, Helper.PearlescentColor, "Pearlescent");
                RefreshColorMenuFor(ClassicColorMenu2, Helper.ClassicColor, "Secondary");
                RefreshColorMenuFor(MetallicColorMenu2, Helper.ClassicColor, "Secondary");
                RefreshColorMenuFor(MetalColorMenu2, Helper.MetalColor, "Secondary");
                RefreshColorMenuFor(MatteColorMenu2, Helper.MatteColor, "Secondary");
                RefreshColorMenuFor(ChromeColorMenu2, Helper.ChromeColor, "Secondary");
                RefreshEnumModMenuFor(PlateMenu, Helper.EnumTypes.NumberPlateType);
            }
            else
            {
                VehicleChangeHandler(sender, index);
            }
        }

        public static void VehicleChangeHandler(NativeMenu sender, int index)
        {
            try
            {
                Tuple<string, int, string, string> t = (Tuple<string, int, string, string>)sender.Items[index].Tag;
                Helper.SelectedVehicle = t.Item3;
                Helper.VehPreview?.Delete();
                if (sender.Items[index].Title.IndexOf("NULL", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    if (Helper.optFade)
                    {
                        FadeOut(200);
                        Script.Wait(200);
                        Helper.VehPreview = Helper.CreateVehicle(t.Item1, Helper.VehPreviewPos, Helper.Radius);
                        Script.Wait(200);
                        FadeIn(200);
                    }
                    else
                    {
                        Helper.VehPreview = Helper.CreateVehicle(t.Item1, Helper.VehPreviewPos, Helper.Radius);
                    }
                }
                if (Helper.optRandomColor && Helper.VehPreview != null)
                {
                    Random r = new Random();
                    int psc = r.Next(0, 160);
                    Mods(Helper.VehPreview).PrimaryColor = (VehicleColor)psc;
                    Mods(Helper.VehPreview).SecondaryColor = (VehicleColor)psc;
                    Mods(Helper.VehPreview).PearlescentColor = (VehicleColor)r.Next(0, 160);
                    Mods(Helper.VehPreview).TrimColor = (VehicleColor)r.Next(0, 160);
                    Mods(Helper.VehPreview).DashboardColor = (VehicleColor)r.Next(0, 160);
                    Mods(Helper.VehPreview).RimColor = (VehicleColor)r.Next(0, 160);
                }
                Helper.UpdateVehPreview();
                Helper.VehicleName = sender.Items[index].Title;
                Helper.optLastVehMake = t.Item4;
                Helper.ShowVehicleName = true;
                Helper.VehPreview.Heading = Helper.Radius;
                Helper.VehPreview.IsUndriveable = true;
                Helper.VehPreview.LockStatus = VehicleLockStatus.IgnoredByPlayer;
                Helper.VehPreview.DirtLevel = 0f;
                Helper.VehiclePrice = t.Item2;
                Helper.wsCamera.RepositionFor(Helper.VehPreview);
                Helper.optLastVehHash = Helper.VehPreview.Model.Hash;
                Helper.optLastVehName = Helper.VehicleName;
                Helper.config.SetValue("SETTINGS", "LastVehHash", Helper.VehPreview.Model.Hash);
                Helper.config.SetValue("SETTINGS", "LastVehName", Helper.VehicleName);
                Helper.config.Save();

                if (Helper.hiddenSave.GetValue("VEHICLES", Helper.VehPreview.Model.Hash.ToString(), 0) == 0)
                {
                    Helper.hiddenSave.SetValue("VEHICLES", Helper.VehPreview.Model.Hash.ToString(), 1);
                    Helper.hiddenSave.Save();
                }
            }
            catch (Exception ex)
            {
                logger.Log(ex.Message + " " + ex.StackTrace);
            }
        }

        public static void ItemSelectHandler(NativeMenu sender, NativeItem selectedItem, int index)
        {
            try
            {
                if (selectedItem == null)
                {
                    return;
                }

                if (selectedItem.Title == Gxt("ITEM_YES"))
                {
                    if (Helper.PlayerCash > Helper.VehiclePrice)
                    {
                        FadeOut(200);
                        Script.Wait(200);
                        Helper.GP.Money = Helper.PlayerCash - Helper.VehiclePrice;
                        ConfirmMenu.Visible = false;
                        Helper.wsCamera.Stop();
                        Helper.DrawSpotLight = false;
                        Helper.VehPreview.IsUndriveable = false;
                        Helper.VehPreview.LockStatus = VehicleLockStatus.Unlocked;
                        Function.Call(Hash.SET_VEHICLE_DOORS_SHUT, Helper.VehPreview, false);
                        Helper.VehPreview.Position = new GTA.Math.Vector3(-56.79958f, -1110.868f, 26.43581f);
                        Function.Call(Hash.TASK_WARP_PED_INTO_VEHICLE, Helper.GPC, Helper.VehPreview, -1);
                        Helper.VehPreview.MarkAsNoLongerNeeded();
                        Helper.VehPreview = null;
                        Helper.HideHud = false;
                        Script.Wait(200);
                        FadeIn(200);
                        Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "PROPERTY_PURCHASE", "HUD_AWARDS", false);
                        GtaScreen.ShowSubtitle("~y~" + Helper.GetLangEntry("VEHICLE_PURCHASED") + "\n~w~" + Helper.SelectedVehicle, 4000);
                        Helper.SelectedVehicle = null;
                        Helper.VehicleName = null;
                        Helper.ShowVehicleName = false;
                        Helper.TaskScriptStatus = -1;
                    }
                    else
                    {
                        if (Game.Player.Character.Name() == "Franklin")
                        {
                            Helper.DisplayNotificationThisFrame(Gxt("EMSTR_55"), string.Empty, Gxt("PI_BIK_HX8"), "CHAR_BANK_FLEECA", true, Helper.IconType.RightJumpingArrow);
                        }
                        else if (Game.Player.Character.Name() == "Trevor")
                        {
                            Helper.DisplayNotificationThisFrame(Gxt("EMSTR_58"), string.Empty, Gxt("PI_BIK_HX8"), "CHAR_BANK_BOL", true, Helper.IconType.RightJumpingArrow);
                        }
                        else
                        {
                            Helper.DisplayNotificationThisFrame(Gxt("EMSTR_52"), string.Empty, Gxt("PI_BIK_HX8"), "CHAR_BANK_MAZE", true, Helper.IconType.RightJumpingArrow);
                        }
                    }
                }
                else if (selectedItem.Title == Helper.GetLangEntry("BTN_TEST_DRIVE"))
                {
                    FadeOut(200);
                    Script.Wait(200);
                    Function.Call(Hash.TASK_WARP_PED_INTO_VEHICLE, Helper.GPC, Helper.VehPreview, -1);
                    ConfirmMenu.Visible = false;
                    Helper.wsCamera.Stop();
                    Helper.DrawSpotLight = false;
                    Helper.VehPreview.IsUndriveable = false;
                    Helper.VehPreview.LockStatus = VehicleLockStatus.Unlocked;
                    Function.Call(Hash.SET_VEHICLE_DOORS_SHUT, Helper.VehPreview, false);
                    Helper.DisplayHelpTextThisFrame(Helper.GetLangEntry("HELP_TEST_DRIVE"));
                    Helper.TestDrive += 1;
                    Helper.HideHud = false;
                    Helper.VehPreview.Position = new GTA.Math.Vector3(-56.79958f, -1110.868f, 26.43581f);
                    Script.Wait(200);
                    FadeIn(200);
                    Helper.ShowVehicleName = false;
                }

                if (selectedItem.Title == Gxt("PERSO_MOD_PER"))
                {
                    VehicleModCollection mods = Mods(Helper.VehPreview);
                    mods.InstallModKit();
                    mods[VehicleModType.Suspension].Index = Math.Max(mods[VehicleModType.Suspension].Count - 1, 0);
                    mods[VehicleModType.Engine].Index = Math.Max(mods[VehicleModType.Engine].Count - 1, 0);
                    mods[VehicleModType.Brakes].Index = Math.Max(mods[VehicleModType.Brakes].Count - 1, 0);
                    mods[VehicleModType.Transmission].Index = Math.Max(mods[VehicleModType.Transmission].Count - 1, 0);
                    mods[VehicleModType.Armor].Index = Math.Max(mods[VehicleModType.Armor].Count - 1, 0);
                    mods[VehicleToggleModType.XenonHeadlights].IsInstalled = true;
                    mods[VehicleToggleModType.Turbo].IsInstalled = true;
                    Function.Call(Hash.SET_VEHICLE_TYRES_CAN_BURST, Helper.VehPreview, false);
                    Function.Call(Hash.ANIMPOSTFX_PLAY, "MP_corona_switch_supermod", 0, true);
                    Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "Lowrider_Upgrade", "Lowrider_Super_Mod_Garage_Sounds", true);
                }
                else if (selectedItem.Title == Helper.GetLangEntry("BTN_PLATE_NUMBER_NAME"))
                {
                    string numPlateText = Game.GetUserInput(Mods(Helper.VehPreview).LicensePlate);
                    if (!string.IsNullOrEmpty(numPlateText))
                    {
                        Mods(Helper.VehPreview).LicensePlate = numPlateText;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(ex.Message + " " + ex.StackTrace);
            }
        }

        public static void CategoryItemSelectHandler(NativeMenu sender, NativeItem selectedItem, int index)
        {
            try
            {
                Tuple<int, string> t = (Tuple<int, string>)selectedItem.Tag;
                CreateVehicleMenu($@".\scripts\PremiumDeluxeMotorsport\Vehicles\{t.Item2}.ini", Helper.GetLangEntry(t.Item2));
                sender.Visible = false;
                VehicleMenu.Visible = true;
                ResetSelection(VehicleMenu);
            }
            catch (Exception ex)
            {
                logger.Log(ex.Message + " " + ex.StackTrace);
            }
        }

        public static void CreateVehicleMenu(string file, string subtitle)
        {
            try
            {
                Reader format = new Reader(file, Parameters);
                VehicleMenu = NewMenu(subtitle.ToUpperInvariant(), true);
                for (int ii = 0; ii < format.Count; ii++)
                {
                    int i = (format.Count - 1) - ii;
                    Helper.Price = decimal.TryParse(format[i]["price"], out decimal parsed) ? parsed : 0m;
                    NativeItem item = new NativeItem($"{Gxt(format[i]["make"])} {Gxt(format[i]["gxt"])}")
                    {
                        AltTitle = "$" + Helper.Price.ToString("N0"),
                        Tag = Tuple.Create(format[i]["model"], (int)Helper.Price, $"{Gxt(format[i]["make"])} {Gxt(format[i]["gxt"])}", format[i]["make"]),
                    };
                    if (item.Title.IndexOf("NULL", StringComparison.OrdinalIgnoreCase) >= 0) item.Title = Gxt(format[i]["gxt"]);
                    if (item.Title.IndexOf("NULL", StringComparison.OrdinalIgnoreCase) >= 0) item.Title = format[i]["name"];
                    Model model = new Model(format[i]["model"]);
                    if (Helper.hiddenSave.GetValue("VEHICLES", model.Hash.ToString(), 0) == 0)
                    {
                        item.AltTitle = item.AltTitle + " *";
                    }
                    if (model.IsInCdImage && model.IsValid)
                    {
                        VehicleMenu.Add(item);
                    }
                }
                ResetSelection(VehicleMenu);
                VehicleMenu.ItemActivated += (sender, args) => VehicleSelectHandler(sender as NativeMenu, args.Item, (sender as NativeMenu)?.SelectedIndex ?? 0);
                VehicleMenu.SelectedIndexChanged += (sender, args) => VehicleChangeHandler(sender as NativeMenu, args.Index);
                VehicleMenu.Closed += (sender, args) => MainMenu.Visible = true;
            }
            catch (Exception ex)
            {
                logger.Log(ex.Message + " " + ex.StackTrace);
            }
        }

        public static void ModsMenuIndexChangedHandler(NativeMenu sender, int index)
        {
            try
            {
                if (sender == null || Helper.VehPreview == null || index < 0 || index >= sender.Items.Count)
                {
                    return;
                }

                object tag = sender.Items[index].Tag;
                if (sender == ClassicColorMenu || sender == ChromeColorMenu || sender == MatteColorMenu || sender == MetalColorMenu)
                {
                    Mods(Helper.VehPreview).PrimaryColor = (VehicleColor)tag;
                }
                else if (sender == MetallicColorMenu)
                {
                    Mods(Helper.VehPreview).PrimaryColor = (VehicleColor)tag;
                    Mods(Helper.VehPreview).PearlescentColor = (VehicleColor)tag;
                }
                else if (sender == PeaColorMenu)
                {
                    Mods(Helper.VehPreview).PearlescentColor = (VehicleColor)tag;
                }
                else if (sender == ClassicColorMenu2 || sender == ChromeColorMenu2 || sender == MatteColorMenu2 || sender == MetalColorMenu2)
                {
                    Mods(Helper.VehPreview).SecondaryColor = (VehicleColor)tag;
                }
                else if (sender == MetallicColorMenu2)
                {
                    Mods(Helper.VehPreview).SecondaryColor = (VehicleColor)tag;
                    Mods(Helper.VehPreview).PearlescentColor = (VehicleColor)tag;
                }
                else if (sender == CPriColorMenu)
                {
                    Mods(Helper.VehPreview).CustomPrimaryColor = (System.Drawing.Color)tag;
                }
                else if (sender == CSecColorMenu)
                {
                    Mods(Helper.VehPreview).CustomSecondaryColor = (System.Drawing.Color)tag;
                }
                else if (sender == PlateMenu)
                {
                    Mods(Helper.VehPreview).LicensePlateStyle = (LicensePlateStyle)tag;
                }

                if (Helper.optRemoveColor)
                {
                    if (sender == CPriColorMenu)
                    {
                        Mods(Helper.VehPreview).PrimaryColor = VehicleColor.MetallicBlack;
                    }
                    else if (sender == CSecColorMenu)
                    {
                        Mods(Helper.VehPreview).SecondaryColor = VehicleColor.MetallicBlack;
                    }
                    else if (sender.Parent == PriColorMenu)
                    {
                        Mods(Helper.VehPreview).ClearCustomPrimaryColor();
                    }
                    else if (sender.Parent == SecColorMenu)
                    {
                        Mods(Helper.VehPreview).ClearCustomSecondaryColor();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(ex.Message + " " + ex.StackTrace);
            }
        }

        public static void ModsMenuItemSelectHandler(NativeMenu sender, NativeItem selectedItem, int index)
        {
            try
            {
                if (sender == null || selectedItem == null || Helper.VehPreview == null)
                {
                    return;
                }

                foreach (NativeItem i in sender.Items)
                {
                    i.AltTitle = string.Empty;
                }

                if (sender == ClassicColorMenu || sender == ChromeColorMenu || sender == MatteColorMenu || sender == MetalColorMenu)
                {
                    Mods(Helper.VehPreview).PrimaryColor = (VehicleColor)selectedItem.Tag;
                    MarkSelected(selectedItem, true);
                    Helper.lastVehMemory.PrimaryColor = (VehicleColor)selectedItem.Tag;
                }
                else if (sender == MetallicColorMenu)
                {
                    Mods(Helper.VehPreview).PrimaryColor = (VehicleColor)selectedItem.Tag;
                    Mods(Helper.VehPreview).PearlescentColor = (VehicleColor)selectedItem.Tag;
                    MarkSelected(selectedItem, true);
                    Helper.lastVehMemory.PrimaryColor = (VehicleColor)selectedItem.Tag;
                    Helper.lastVehMemory.PearlescentColor = (VehicleColor)selectedItem.Tag;
                }
                else if (sender == PeaColorMenu)
                {
                    Mods(Helper.VehPreview).PearlescentColor = (VehicleColor)selectedItem.Tag;
                    MarkSelected(selectedItem, true);
                    Helper.lastVehMemory.PearlescentColor = (VehicleColor)selectedItem.Tag;
                }
                else if (sender == ClassicColorMenu2 || sender == ChromeColorMenu2 || sender == MatteColorMenu2 || sender == MetalColorMenu2)
                {
                    Mods(Helper.VehPreview).SecondaryColor = (VehicleColor)selectedItem.Tag;
                    MarkSelected(selectedItem, true);
                    Helper.lastVehMemory.SecondaryColor = (VehicleColor)selectedItem.Tag;
                }
                else if (sender == MetallicColorMenu2)
                {
                    Mods(Helper.VehPreview).SecondaryColor = (VehicleColor)selectedItem.Tag;
                    Mods(Helper.VehPreview).PearlescentColor = (VehicleColor)selectedItem.Tag;
                    MarkSelected(selectedItem, true);
                    Helper.lastVehMemory.SecondaryColor = (VehicleColor)selectedItem.Tag;
                    Helper.lastVehMemory.PearlescentColor = (VehicleColor)selectedItem.Tag;
                }
                else if (sender == CPriColorMenu)
                {
                    Mods(Helper.VehPreview).CustomPrimaryColor = (System.Drawing.Color)selectedItem.Tag;
                    MarkSelected(selectedItem, true);
                    Helper.lastVehMemory.CustomPrimaryColor = (System.Drawing.Color)selectedItem.Tag;
                }
                else if (sender == CSecColorMenu)
                {
                    Mods(Helper.VehPreview).CustomSecondaryColor = (System.Drawing.Color)selectedItem.Tag;
                    MarkSelected(selectedItem, true);
                    Helper.lastVehMemory.CustomSecondaryColor = (System.Drawing.Color)selectedItem.Tag;
                }
                else if (sender == PlateMenu)
                {
                    Mods(Helper.VehPreview).LicensePlateStyle = (LicensePlateStyle)selectedItem.Tag;
                    MarkSelected(selectedItem, true);
                    Helper.lastVehMemory.NumberPlate = (LicensePlateStyle)selectedItem.Tag;
                }

                if (Helper.optRemoveColor)
                {
                    if (sender == CPriColorMenu)
                    {
                        Mods(Helper.VehPreview).PrimaryColor = VehicleColor.MetallicBlack;
                    }
                    else if (sender == CSecColorMenu)
                    {
                        Mods(Helper.VehPreview).SecondaryColor = VehicleColor.MetallicBlack;
                    }
                    else if (sender.Parent == PriColorMenu)
                    {
                        Mods(Helper.VehPreview).ClearCustomPrimaryColor();
                    }
                    else if (sender.Parent == SecColorMenu)
                    {
                        Mods(Helper.VehPreview).ClearCustomSecondaryColor();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(ex.Message + " " + ex.StackTrace);
            }
        }

        public static void ModsMenuCloseHandler(NativeMenu sender)
        {
            try
            {
                if (Helper.VehPreview == null || Helper.lastVehMemory == null)
                {
                    return;
                }
                Mods(Helper.VehPreview).PrimaryColor = Helper.lastVehMemory.PrimaryColor;
                Mods(Helper.VehPreview).SecondaryColor = Helper.lastVehMemory.SecondaryColor;
                Mods(Helper.VehPreview).PearlescentColor = Helper.lastVehMemory.PearlescentColor;
                Mods(Helper.VehPreview).LicensePlateStyle = (LicensePlateStyle)(int)Helper.lastVehMemory.NumberPlate;

                if (Helper.optRemoveColor)
                {
                    if (sender == CPriColorMenu)
                    {
                        Mods(Helper.VehPreview).PrimaryColor = VehicleColor.MetallicBlack;
                    }
                    else if (sender == CSecColorMenu)
                    {
                        Mods(Helper.VehPreview).SecondaryColor = VehicleColor.MetallicBlack;
                    }
                    else if (sender.Parent == PriColorMenu)
                    {
                        Mods(Helper.VehPreview).ClearCustomPrimaryColor();
                    }
                    else if (sender.Parent == SecColorMenu)
                    {
                        Mods(Helper.VehPreview).ClearCustomSecondaryColor();
                    }
                }

                if (sender == CPriColorMenu)
                {
                    Mods(Helper.VehPreview).CustomPrimaryColor = Helper.lastVehMemory.CustomPrimaryColor;
                }
                if (sender == CSecColorMenu)
                {
                    Mods(Helper.VehPreview).CustomSecondaryColor = Helper.lastVehMemory.CustomSecondaryColor;
                }
            }
            catch (Exception ex)
            {
                logger.Log(ex.Message + " " + ex.StackTrace);
            }
        }
    }
}
