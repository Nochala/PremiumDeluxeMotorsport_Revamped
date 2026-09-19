using GTA;
using GTA.Native;
using GTA.UI;
using LemonUI;
using LemonUI.Elements;
using LemonUI.Menus;
using LemonUI.Scaleform;
using LemonUI.Tools;
using System;
using System.Collections.Generic;
using System.Drawing;
using GameScreen = LemonUI.Tools.GameScreen;
using GtaScreen = GTA.UI.Screen;

namespace PremiumDeluxeRevamped
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
        public static NativeItem ItemPerformance;

        public static string[] Parameters = { "[name]", "[price]", "[model]", "[gxt]", "[make]" };
        public static ObjectPool _menuPool;

        private static readonly List<NativeMenu> RegisteredMenus = new List<NativeMenu>();
        private static readonly Dictionary<NativeMenu, string> RegisteredMenuTitles = new Dictionary<NativeMenu, string>();
        private static bool suppressCloseHandlers;
        private static NativeMenu lastVisibleMenu;
        private static int lastVisibleMenuSeenAt;
        private const int HiddenMenuRecoveryDelayMs = 150;
        private const string SelectionMarker = ">>";
        private const string SubmenuIndicator = ">>>";
        private static readonly Dictionary<NativeItem, string> PreservedSubmenuAltTitles = new Dictionary<NativeItem, string>();
        private const float ViewerSpawnCleanupSearchRadius = 6.0f;
        private const float ViewerSpawnCleanupDeleteRadius = 2.9f;
        private static readonly GTA.Math.Vector3 TestDriveSpawnPosition = new GTA.Math.Vector3(-56.79958f, -1110.868f, 26.43581f);
        private const int TestDriveWarpRetryFrames = 30;
        private const string ConfirmActionTestDrive = "TEST_DRIVE";
        private const string ConfirmActionPurchase = "PURCHASE";
        private static bool viewerActionInProgress;
        private const int VehicleMenuBaseMaxTitleLength = 30;
        private const int VehicleMenuMinTitleLength = 20;
        private const string CategoryActionSearchVehicles = "SEARCH_VEHICLES";
        private const string EmergencyCategoryKey = "emergency";
        private const int PerformanceUpgradePrice = 77500;
        private static int PreviewVehicleBasePrice;
        private static Model PendingPreviewModel;
        private static Tuple<string, int, string, string> PendingVehicleSelection;
        private static bool HasPendingVehicleChange;
        public static int LegitimatePdmVehicleHandle { get; private set; }
        public static int LegitimatePdmVehicleUntil { get; private set; }

        public static void MarkLegitimatePdmVehicle(Vehicle vehicle, int durationMs)
        {
            LegitimatePdmVehicleHandle = (vehicle != null && vehicle.Exists()) ? vehicle.Handle : 0;
            LegitimatePdmVehicleUntil = Game.GameTime + Math.Max(durationMs, 0);
        }

        public static bool IsLegitimatePdmVehicle(Vehicle vehicle)
        {
            return vehicle != null && vehicle.Exists() && Game.GameTime <= LegitimatePdmVehicleUntil && vehicle.Handle == LegitimatePdmVehicleHandle;
        }

        public static void ClearLegitimatePdmVehicle()
        {
            LegitimatePdmVehicleHandle = 0;
            LegitimatePdmVehicleUntil = 0;
        }

        private static string Gxt(string key) => Game.GetLocalizedString(key);

        private static MenuMouseBehavior GetConfiguredMouseBehavior()
        {
            return Helper.optEnableMouse ? MenuMouseBehavior.Movement : MenuMouseBehavior.Disabled;
        }

        private static bool IsCursorInsideMenuArea()
        {
            System.Drawing.PointF topLeft = SafeZone.GetSafePosition(new System.Drawing.PointF(0f, 0f));
            System.Drawing.SizeF size = new System.Drawing.SizeF(431f, 550f);
            return GameScreen.IsCursorInArea(topLeft, size);
        }

        public static void RefreshMouseBehaviors()
        {
            bool anyMenuVisible = _menuPool != null && _menuPool.AreAnyVisible;
            bool cameraDragging = Helper.wsCamera != null && Helper.wsCamera.IsDragging;
            bool leftClickOutsideMenu = false;

            if (Helper.optEnableMouse && anyMenuVisible && !cameraDragging)
            {
                bool attackPressed =
                    Game.IsControlPressed(Control.Attack) ||
                    Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED, 0, (int)Control.Attack);

                if (attackPressed)
                {
                    leftClickOutsideMenu = !IsCursorInsideMenuArea();
                }
            }

            MenuMouseBehavior desired = (Helper.optEnableMouse && !cameraDragging && !leftClickOutsideMenu)
                ? MenuMouseBehavior.Movement
                : MenuMouseBehavior.Disabled;

            for (int i = 0; i < RegisteredMenus.Count; i++)
            {
                NativeMenu menu = RegisteredMenus[i];
                if (menu != null && menu.MouseBehavior != desired)
                {
                    menu.MouseBehavior = desired;
                }
            }
        }

        public static void CleanupVehicleViewerArea()
        {
            try
            {
                Vehicle[] nearbyVehicles = World.GetNearbyVehicles(Helper.VehPreviewPos, ViewerSpawnCleanupSearchRadius);
                if (nearbyVehicles == null || nearbyVehicles.Length == 0)
                {
                    return;
                }

                int currentPreviewHandle = (Helper.VehPreview != null && Helper.VehPreview.Exists()) ? Helper.VehPreview.Handle : 0;
                float deleteDistanceSquared = ViewerSpawnCleanupDeleteRadius * ViewerSpawnCleanupDeleteRadius;

                foreach (Vehicle vehicle in nearbyVehicles)
                {
                    if (vehicle == null || !vehicle.Exists())
                    {
                        continue;
                    }

                    if (vehicle.Handle == currentPreviewHandle)
                    {
                        continue;
                    }

                    if (vehicle.Position.DistanceToSquared(Helper.VehPreviewPos) > deleteDistanceSquared)
                    {
                        continue;
                    }

                    if (Helper.GPC != null && Helper.GPC.Exists() && Helper.GPC.IsInVehicle(vehicle))
                    {
                        continue;
                    }

                    try { vehicle.IsPersistent = false; } catch { }
                    try { vehicle.MarkAsNoLongerNeeded(); } catch { }
                    try { vehicle.Delete(); } catch { }
                }
            }
            catch (Exception ex)
            {
                logger.Log("Error CleanupVehicleViewerArea " + ex.Message + " " + ex.StackTrace);
            }
        }

        private static int ResolveConfiguredVehiclePrice(string modelName, decimal configuredPrice, string makeKey)
        {
            int basePrice = Math.Max((int)configuredPrice, 0);
            if (!Helper.optRealisticVehPricing)
            {
                return basePrice;
            }

            try
            {
                Model model = new Model(modelName);
                int vehicleClass = model.IsValid
                    ? Function.Call<int>(Hash.GET_VEHICLE_CLASS_FROM_NAME, model.Hash)
                    : -1;

                if (vehicleClass == 18)
                {
                    return ResolveEmergencyVehiclePrice(modelName, basePrice, makeKey);
                }

                int minimumPrice;
                int maximumPrice;
                GetRealisticPriceRange(vehicleClass, out minimumPrice, out maximumPrice);

                double normalizedPriceSignal = GetAdaptiveConfiguredPriceSignal(basePrice, GetConfiguredPricePivot(vehicleClass), 0.32d);
                double curvedSignal = Math.Pow(normalizedPriceSignal, GetConfiguredPriceCurve(vehicleClass));
                double estimatedPrice = minimumPrice + ((maximumPrice - minimumPrice) * curvedSignal);
                estimatedPrice *= GetManufacturerPriceMultiplier(makeKey);

                double lowerClamp = minimumPrice * 0.90d;
                double upperClamp = maximumPrice * 1.15d;
                estimatedPrice = Math.Max(lowerClamp, Math.Min(upperClamp, estimatedPrice));

                return RoundRealisticVehiclePrice(estimatedPrice);
            }
            catch (Exception ex)
            {
                logger.Log("Error ResolveConfiguredVehiclePrice " + ex.Message + " " + ex.StackTrace);
                return basePrice;
            }
        }

        private static double GetAdaptiveConfiguredPriceSignal(int configuredPrice, double pivotPrice, double defaultSignal)
        {
            if (configuredPrice <= 0)
            {
                return defaultSignal;
            }

            double safePivot = Math.Max(pivotPrice, 1d);
            double ratio = configuredPrice / (configuredPrice + safePivot);
            double signal = Math.Pow(ratio, 0.72d);
            return Math.Max(0.04d, Math.Min(0.985d, signal));
        }

        private static double GetConfiguredPricePivot(int vehicleClass)
        {
            switch (vehicleClass)
            {
                case 0: return 650000d;
                case 1: return 1100000d;
                case 2: return 1500000d;
                case 3: return 1300000d;
                case 4: return 1400000d;
                case 5: return 1800000d;
                case 6: return 1900000d;
                case 7: return 2600000d;
                case 8: return 900000d;
                case 9: return 1600000d;
                case 10: return 1900000d;
                case 11: return 1000000d;
                case 12: return 1000000d;
                case 13: return 50000d;
                case 14: return 1800000d;
                case 15: return 3200000d;
                case 16: return 4500000d;
                case 17: return 1200000d;
                case 19: return 3200000d;
                case 20: return 2200000d;
                case 22: return 3000000d;
                default: return 1500000d;
            }
        }

        private static double GetConfiguredPriceCurve(int vehicleClass)
        {
            switch (vehicleClass)
            {
                case 5:
                case 7:
                case 14:
                case 15:
                case 16:
                case 19:
                case 22:
                    return 1.08d;
                default:
                    return 1.0d;
            }
        }

        private static int ResolveEmergencyVehiclePrice(string modelName, int configuredPrice, string makeKey)
        {
            int minimumPrice;
            int maximumPrice;
            double pivotPrice;
            GetEmergencyVehiclePriceRange(modelName, out minimumPrice, out maximumPrice, out pivotPrice);

            double normalizedPriceSignal = GetAdaptiveConfiguredPriceSignal(configuredPrice, pivotPrice, 0.35d);
            double estimatedPrice = minimumPrice + ((maximumPrice - minimumPrice) * normalizedPriceSignal);

            double manufacturerMultiplier = GetManufacturerPriceMultiplier(makeKey);
            estimatedPrice *= 1.0d + ((manufacturerMultiplier - 1.0d) * 0.65d);

            double lowerClamp = minimumPrice * 0.95d;
            double upperClamp = maximumPrice * 1.10d;
            estimatedPrice = Math.Max(lowerClamp, Math.Min(upperClamp, estimatedPrice));

            return RoundRealisticVehiclePrice(estimatedPrice);
        }

        private static void GetEmergencyVehiclePriceRange(string modelName, out int minimumPrice, out int maximumPrice, out double pivotPrice)
        {
            string model = (modelName ?? string.Empty).Trim().ToLowerInvariant();

            if (model == "ambulance")
            {
                minimumPrice = 220000;
                maximumPrice = 380000;
                pivotPrice = 900000d;
                return;
            }

            if (model == "firetruk")
            {
                minimumPrice = 500000;
                maximumPrice = 950000;
                pivotPrice = 1200000d;
                return;
            }

            if (model.StartsWith("policeb", StringComparison.Ordinal))
            {
                minimumPrice = 24000;
                maximumPrice = 42000;
                pivotPrice = 1200000d;
                return;
            }

            if (model == "riot" || model == "riot2")
            {
                minimumPrice = 280000;
                maximumPrice = 700000;
                pivotPrice = 2200000d;
                return;
            }

            if (model == "pbus")
            {
                minimumPrice = 160000;
                maximumPrice = 280000;
                pivotPrice = 1300000d;
                return;
            }

            if (model.StartsWith("policet", StringComparison.Ordinal))
            {
                minimumPrice = 52000;
                maximumPrice = 95000;
                pivotPrice = 1600000d;
                return;
            }

            if (model == "fbi2"
                || model == "sheriff2"
                || model == "policeold1"
                || model == "poldorado"
                || model == "polcaracara"
                || model == "polterminus"
                || model == "pranger"
                || model == "lguard")
            {
                minimumPrice = 52000;
                maximumPrice = 120000;
                pivotPrice = 1900000d;
                return;
            }

            if (model == "polgauntlet"
                || model == "poldominator10"
                || model == "polcoquette4"
                || model == "polbuffalo6"
                || model == "polignus")
            {
                minimumPrice = 70000;
                maximumPrice = 210000;
                pivotPrice = 2700000d;
                return;
            }

            minimumPrice = 45000;
            maximumPrice = 105000;
            pivotPrice = 1800000d;
        }

        private static void GetRealisticPriceRange(int vehicleClass, out int minimumPrice, out int maximumPrice)
        {
            switch (vehicleClass)
            {
                case 0: minimumPrice = 18000; maximumPrice = 45000; return;
                case 1: minimumPrice = 24000; maximumPrice = 95000; return;
                case 2: minimumPrice = 30000; maximumPrice = 160000; return;
                case 3: minimumPrice = 30000; maximumPrice = 140000; return;
                case 4: minimumPrice = 32000; maximumPrice = 125000; return;
                case 5: minimumPrice = 40000; maximumPrice = 900000; return;
                case 6: minimumPrice = 45000; maximumPrice = 320000; return;
                case 7: minimumPrice = 180000; maximumPrice = 3500000; return;
                case 8: minimumPrice = 8000; maximumPrice = 45000; return;
                case 9: minimumPrice = 28000; maximumPrice = 130000; return;
                case 10: minimumPrice = 65000; maximumPrice = 240000; return;
                case 11: minimumPrice = 22000; maximumPrice = 100000; return;
                case 12: minimumPrice = 26000; maximumPrice = 90000; return;
                case 13: minimumPrice = 500; maximumPrice = 9000; return;
                case 14: minimumPrice = 25000; maximumPrice = 1500000; return;
                case 15: minimumPrice = 300000; maximumPrice = 8000000; return;
                case 16: minimumPrice = 150000; maximumPrice = 20000000; return;
                case 17: minimumPrice = 25000; maximumPrice = 160000; return;
                case 18: minimumPrice = 45000; maximumPrice = 105000; return;
                case 19: minimumPrice = 90000; maximumPrice = 3000000; return;
                case 20: minimumPrice = 80000; maximumPrice = 300000; return;
                case 22: minimumPrice = 750000; maximumPrice = 3500000; return;
                default: minimumPrice = 25000; maximumPrice = 180000; return;
            }
        }

        private static double GetManufacturerPriceMultiplier(string makeKey)
        {
            string make = (makeKey ?? string.Empty).ToUpperInvariant();

            if (make.Contains("TRUFFADE")) return 1.18d;
            if (make.Contains("PROGEN")) return 1.15d;
            if (make.Contains("OVERFLOD")) return 1.15d;
            if (make.Contains("PEGASSI")) return 1.12d;
            if (make.Contains("GROTTI")) return 1.12d;
            if (make.Contains("ENUS")) return 1.10d;
            if (make.Contains("DEWBAUCHEE")) return 1.08d;
            if (make.Contains("LAMPADATI")) return 1.08d;
            if (make.Contains("PFISTER")) return 1.07d;
            if (make.Contains("OCELOT")) return 1.06d;
            if (make.Contains("BENEFACTOR")) return 1.05d;
            if (make.Contains("OBEY")) return 1.04d;
            if (make.Contains("COIL")) return 1.04d;
            if (make.Contains("UBERMACHT")) return 1.03d;
            if (make.Contains("KARIN")) return 0.94d;
            if (make.Contains("DINKA")) return 0.94d;
            if (make.Contains("MAIBATSU")) return 0.95d;
            if (make.Contains("WEENY")) return 0.95d;

            return 1.0d;
        }

        private static int RoundRealisticVehiclePrice(double price)
        {
            int increment = price >= 500000d ? 5000 : price >= 100000d ? 1000 : 500;
            return Math.Max(increment, (int)(Math.Round(price / increment, MidpointRounding.AwayFromZero) * increment));
        }

        public static int ResolveVehiclePrice(int modelHash, string fallbackVehicleName = null)
        {
            try
            {
                foreach (string file in System.IO.Directory.GetFiles(@".\scripts\PremiumDeluxeMotorsport\Vehicles\", "*.ini"))
                {
                    Reader format = new Reader(file, Parameters);
                    for (int ii = 0; ii < format.Count; ii++)
                    {
                        string modelName = format[ii]["model"];
                        Model model = new Model(modelName);
                        if (!model.IsValid || model.Hash != modelHash)
                        {
                            continue;
                        }

                        if (decimal.TryParse(format[ii]["price"], out decimal parsedPrice))
                        {
                            return ResolveConfiguredVehiclePrice(modelName, parsedPrice, format[ii]["make"]);
                        }
                    }
                }

                for (int i = 0; i < Helper.AddonVehicles.Count; i++)
                {
                    Helper.AddonVehicleDefinition addon = Helper.AddonVehicles[i];
                    if (addon == null)
                    {
                        continue;
                    }

                    Model addonModel = new Model(addon.ModelName);
                    if (addonModel.IsValid && addonModel.Hash == modelHash)
                    {
                        return ResolveConfiguredVehiclePrice(addon.ModelName, addon.Price, addon.Make);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log("Error ResolveVehiclePrice " + ex.Message + " " + ex.StackTrace);
            }

            return Helper.VehiclePrice > 0 ? Helper.VehiclePrice : 0;
        }

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

        private static string CleanMenuText(string value, string fallback = null)
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "NULL", StringComparison.OrdinalIgnoreCase))
            {
                return fallback ?? "Unnamed";
            }

            return value;
        }

        private static string GetVehicleCategoryDisplayName(string categoryKey)
        {
            if (string.Equals(categoryKey, EmergencyCategoryKey, StringComparison.OrdinalIgnoreCase))
            {
                return "Emergency";
            }

            return CleanMenuText(Helper.GetLangEntry(categoryKey), categoryKey);
        }

        private static int GetAddonVehicleCountForCategory(string categoryKey)
        {
            int count = 0;
            for (int i = 0; i < Helper.AddonVehicles.Count; i++)
            {
                Helper.AddonVehicleDefinition addon = Helper.AddonVehicles[i];
                if (addon != null && string.Equals(addon.CategoryKey, categoryKey, StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }
            return count;
        }

        private static string GetAddonVehicleDisplayName(Helper.AddonVehicleDefinition addon)
        {
            if (addon == null || string.IsNullOrWhiteSpace(addon.ModelName))
            {
                return "Unnamed";
            }

            try
            {
                Model model = new Model(addon.ModelName);
                string displayLabel = model.IsValid
                    ? Function.Call<string>(Hash.GET_DISPLAY_NAME_FROM_VEHICLE_MODEL, model.Hash)
                    : string.Empty;
                string localizedName = string.IsNullOrWhiteSpace(displayLabel)
                    ? string.Empty
                    : Gxt(displayLabel);

                if (string.IsNullOrWhiteSpace(localizedName)
                    || string.Equals(localizedName, "NULL", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(localizedName, "CARNOTFOUND", StringComparison.OrdinalIgnoreCase))
                {
                    localizedName = addon.ModelName;
                }

                return localizedName;
            }
            catch
            {
                return addon.ModelName;
            }
        }

        private static HashSet<int> GetConfiguredVehicleModelHashes()
        {
            HashSet<int> hashes = new HashSet<int>();

            try
            {
                foreach (string file in System.IO.Directory.GetFiles(@".\scripts\PremiumDeluxeMotorsport\Vehicles\", "*.ini"))
                {
                    if (!System.IO.File.Exists(file))
                    {
                        continue;
                    }

                    Reader format = new Reader(file, Parameters);
                    for (int i = 0; i < format.Count; i++)
                    {
                        Model model = new Model(format[i]["model"]);
                        if (model.IsValid)
                        {
                            hashes.Add(model.Hash);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log("Error GetConfiguredVehicleModelHashes " + ex.Message + " " + ex.StackTrace);
            }

            return hashes;
        }

        private static void AddAddonVehiclesToMenu(NativeMenu menu, string categoryKey, string searchQuery, HashSet<int> existingModelHashes)
        {
            if (menu == null)
            {
                return;
            }

            for (int i = 0; i < Helper.AddonVehicles.Count; i++)
            {
                Helper.AddonVehicleDefinition addon = Helper.AddonVehicles[i];
                if (addon == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(categoryKey)
                    && !string.Equals(addon.CategoryKey, categoryKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Model model = new Model(addon.ModelName);
                if (!model.IsValid || !model.IsInCdImage)
                {
                    continue;
                }

                if (existingModelHashes != null && existingModelHashes.Contains(model.Hash))
                {
                    continue;
                }

                string modelDisplayName = GetAddonVehicleDisplayName(addon);
                if (!string.IsNullOrWhiteSpace(searchQuery)
                    && !DoesVehicleNameMatchSearch(searchQuery, modelDisplayName, addon.ModelName))
                {
                    continue;
                }

                int vehiclePrice = ResolveConfiguredVehiclePrice(addon.ModelName, addon.Price, addon.Make);
                string fullVehicleName = CleanMenuText(((addon.Make ?? string.Empty) + " " + modelDisplayName).Trim(), addon.ModelName);
                AddVehicleItemToMenu(menu, addon.ModelName, vehiclePrice, fullVehicleName, addon.Make);

                if (existingModelHashes != null)
                {
                    existingModelHashes.Add(model.Hash);
                }
            }
        }

        private static string BuildVehicleMenuTitle(string fullVehicleName, decimal price)
        {
            string cleanName = CleanMenuText(fullVehicleName, "Unnamed");
            string priceText = "$" + price.ToString("N0");
            int pricePenalty = Math.Max(0, priceText.Length - 8);
            int maxTitleLength = Math.Max(VehicleMenuMinTitleLength, VehicleMenuBaseMaxTitleLength - pricePenalty);

            if (cleanName.Length <= maxTitleLength)
            {
                return cleanName;
            }

            int truncatedLength = Math.Max(VehicleMenuMinTitleLength - 3, maxTitleLength - 3);
            string truncated = cleanName.Substring(0, truncatedLength).TrimEnd();
            return truncated + "...";
        }

        public static bool IsPreviewVehicleConvertible()
        {
            if (Helper.VehPreview == null || !Helper.VehPreview.Exists())
            {
                return false;
            }

            try
            {
                return Function.Call<bool>((Hash)0x52F357A30698BCCEuL, Helper.VehPreview, false);
            }
            catch
            {
                return false;
            }
        }

        private static bool ShouldShowRoofInstructionalButton()
        {
            NativeMenu visibleMenu = GetVisibleMenu();
            if (visibleMenu == null)
            {
                return false;
            }

            if (!object.ReferenceEquals(visibleMenu, VehicleMenu) &&
                !object.ReferenceEquals(visibleMenu, ConfirmMenu) &&
                !object.ReferenceEquals(visibleMenu, CustomiseMenu) &&
                !object.ReferenceEquals(visibleMenu, PriColorMenu) &&
                !object.ReferenceEquals(visibleMenu, ClassicColorMenu) &&
                !object.ReferenceEquals(visibleMenu, MetallicColorMenu) &&
                !object.ReferenceEquals(visibleMenu, MetalColorMenu) &&
                !object.ReferenceEquals(visibleMenu, MatteColorMenu) &&
                !object.ReferenceEquals(visibleMenu, ChromeColorMenu) &&
                !object.ReferenceEquals(visibleMenu, PeaColorMenu) &&
                !object.ReferenceEquals(visibleMenu, CPriColorMenu) &&
                !object.ReferenceEquals(visibleMenu, ColorMenu) &&
                !object.ReferenceEquals(visibleMenu, SecColorMenu) &&
                !object.ReferenceEquals(visibleMenu, ClassicColorMenu2) &&
                !object.ReferenceEquals(visibleMenu, MetallicColorMenu2) &&
                !object.ReferenceEquals(visibleMenu, MetalColorMenu2) &&
                !object.ReferenceEquals(visibleMenu, MatteColorMenu2) &&
                !object.ReferenceEquals(visibleMenu, ChromeColorMenu2) &&
                !object.ReferenceEquals(visibleMenu, CSecColorMenu) &&
                !object.ReferenceEquals(visibleMenu, PlateMenu))
            {
                return false;
            }

            return Helper.TaskScriptStatus == 0 && IsPreviewVehicleConvertible();
        }

        public static void RefreshInstructionalButtons()
        {
            for (int i = 0; i < RegisteredMenus.Count; i++)
            {
                NativeMenu menu = RegisteredMenus[i];
                if (menu == null)
                {
                    continue;
                }

                menu.Buttons.Clear();
                AddInstructionalButtonIfValid(menu, Helper.BtnRotLeft);
                if (ShouldShowRoofInstructionalButton())
                {
                    AddInstructionalButtonIfValid(menu, Helper.BtnRotRight);
                }
                AddInstructionalButtonIfValid(menu, Helper.BtnCamera);
                AddInstructionalButtonIfValid(menu, Helper.BtnZoom);
                AddInstructionalButtonIfValid(menu, Helper.BtnZoomOut);
            }
        }

        private static string NormalizeAltTitle(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("~s~", string.Empty)
                .Replace("~w~", string.Empty)
                .Replace("~g~", string.Empty)
                .Replace("~b~", string.Empty)
                .Replace("~y~", string.Empty)
                .Replace("~r~", string.Empty)
                .Trim();
        }

        private static bool IsSelectionMarkerAltTitle(string value)
        {
            string normalized = NormalizeAltTitle(value);
            return normalized == SelectionMarker || normalized == "●" || normalized == "■" || normalized == "□" || normalized == "*";
        }

        private static void RememberSubmenuAltTitle(NativeItem item)
        {
            if (item is NativeSubmenuItem && item != null && !PreservedSubmenuAltTitles.ContainsKey(item))
            {
                PreservedSubmenuAltTitles[item] = item.AltTitle ?? string.Empty;
            }
        }

        private static void RestoreSubmenuAltTitle(NativeItem item)
        {
            if (item is NativeSubmenuItem && item != null && PreservedSubmenuAltTitles.TryGetValue(item, out string altTitle))
            {
                item.AltTitle = altTitle ?? string.Empty;
            }
        }

        private static void RestoreSubmenuAltTitles(NativeMenu menu)
        {
            if (menu == null)
            {
                return;
            }

            for (int i = 0; i < menu.Items.Count; i++)
            {
                RestoreSubmenuAltTitle(menu.Items[i]);
            }
        }

        private static void ClearSelectionMarkers(NativeMenu menu)
        {
            if (menu == null)
            {
                return;
            }

            for (int i = 0; i < menu.Items.Count; i++)
            {
                NativeItem item = menu.Items[i];
                if (item == null)
                {
                    continue;
                }

                if (item is NativeSubmenuItem)
                {
                    RestoreSubmenuAltTitle(item);
                    continue;
                }

                if (IsSelectionMarkerAltTitle(item.AltTitle))
                {
                    item.AltTitle = string.Empty;
                }
            }
        }

        private static void FadeOut(int time) => GtaScreen.FadeOut(time);

        private static void FadeIn(int time) => GtaScreen.FadeIn(time);

        private static VehicleModCollection Mods(Vehicle vehicle) => vehicle.Mods;

        private static string GetPerformanceUpgradeApplyTitle()
            => CleanMenuText(Gxt("PERSO_MOD_PER"), "Performance");

        private static string GetPerformanceUpgradeRemoveTitle()
        {
            string localized = Helper.GetLangEntry("BTN_REMOVE_PERFORMANCE_UPGRADES");
            if (string.IsNullOrWhiteSpace(localized) || string.Equals(localized, "NULL", StringComparison.OrdinalIgnoreCase))
            {
                localized = "Remove Performance Upgrades";
            }

            return CleanMenuText(localized, "Remove Performance Upgrades");
        }

        private static bool IsPerformanceUpgradeActive(Vehicle vehicle)
        {
            if (vehicle == null || !vehicle.Exists())
            {
                return false;
            }

            try
            {
                VehicleModCollection mods = Mods(vehicle);
                mods.InstallModKit();

                bool hasUpgradeablePerformanceMods = false;
                VehicleModType[] indexedTypes =
                {
                    VehicleModType.Suspension,
                    VehicleModType.Engine,
                    VehicleModType.Brakes,
                    VehicleModType.Transmission,
                    VehicleModType.Armor,
                };

                foreach (VehicleModType modType in indexedTypes)
                {
                    try
                    {
                        int count = mods[modType].Count;
                        if (count <= 0)
                        {
                            continue;
                        }

                        hasUpgradeablePerformanceMods = true;
                        if (mods[modType].Index != count - 1)
                        {
                            return false;
                        }
                    }
                    catch
                    {
                    }
                }

                try
                {
                    bool turboInstalled = mods[VehicleToggleModType.Turbo].IsInstalled;
                    hasUpgradeablePerformanceMods = true;
                    if (!turboInstalled)
                    {
                        return false;
                    }
                }
                catch
                {
                }

                try
                {
                    bool xenonInstalled = mods[VehicleToggleModType.XenonHeadlights].IsInstalled;
                    hasUpgradeablePerformanceMods = true;
                    if (!xenonInstalled)
                    {
                        return false;
                    }
                }
                catch
                {
                }

                return hasUpgradeablePerformanceMods;
            }
            catch
            {
                return false;
            }
        }

        private static void RefreshPreviewVehiclePrice()
        {
            int basePrice = PreviewVehicleBasePrice;
            if (basePrice <= 0)
            {
                basePrice = Math.Max(Helper.VehiclePrice, 0);
            }

            Helper.VehiclePrice = basePrice + (IsPerformanceUpgradeActive(Helper.VehPreview) ? PerformanceUpgradePrice : 0);
        }

        private static string GetPerformanceUpgradeItemAltTitle()
        {
            string amount = "$" + PerformanceUpgradePrice.ToString("N0");
            return IsPerformanceUpgradeActive(Helper.VehPreview)
                ? "~r~-" + amount
                : "~g~+" + amount;
        }

        private static void UpdatePerformanceUpgradeItemState()
        {
            if (ItemPerformance == null)
            {
                return;
            }

            ItemPerformance.Title = IsPerformanceUpgradeActive(Helper.VehPreview)
                ? GetPerformanceUpgradeRemoveTitle()
                : GetPerformanceUpgradeApplyTitle();
            ItemPerformance.AltTitle = GetPerformanceUpgradeItemAltTitle();
            RefreshPreviewVehiclePrice();
        }

        private static void SetPerformanceModIndexToMax(VehicleModCollection mods, VehicleModType modType)
        {
            try
            {
                int count = mods[modType].Count;
                mods[modType].Index = count > 0 ? count - 1 : -1;
            }
            catch
            {
            }
        }

        private static void SetPerformanceModIndexToStock(VehicleModCollection mods, VehicleModType modType)
        {
            try
            {
                mods[modType].Index = -1;
            }
            catch
            {
            }
        }

        private static void SetTogglePerformanceModState(VehicleModCollection mods, VehicleToggleModType modType, bool installed)
        {
            try
            {
                mods[modType].IsInstalled = installed;
            }
            catch
            {
            }
        }

        private static void ApplyPerformanceUpgrades(Vehicle vehicle)
        {
            if (vehicle == null || !vehicle.Exists())
            {
                return;
            }

            VehicleModCollection mods = Mods(vehicle);
            mods.InstallModKit();
            SetPerformanceModIndexToMax(mods, VehicleModType.Suspension);
            SetPerformanceModIndexToMax(mods, VehicleModType.Engine);
            SetPerformanceModIndexToMax(mods, VehicleModType.Brakes);
            SetPerformanceModIndexToMax(mods, VehicleModType.Transmission);
            SetPerformanceModIndexToMax(mods, VehicleModType.Armor);
            SetTogglePerformanceModState(mods, VehicleToggleModType.XenonHeadlights, true);
            SetTogglePerformanceModState(mods, VehicleToggleModType.Turbo, true);
            Function.Call(Hash.SET_VEHICLE_TYRES_CAN_BURST, vehicle, false);
        }

        private static void RemovePerformanceUpgrades(Vehicle vehicle)
        {
            if (vehicle == null || !vehicle.Exists())
            {
                return;
            }

            VehicleModCollection mods = Mods(vehicle);
            mods.InstallModKit();
            SetPerformanceModIndexToStock(mods, VehicleModType.Suspension);
            SetPerformanceModIndexToStock(mods, VehicleModType.Engine);
            SetPerformanceModIndexToStock(mods, VehicleModType.Brakes);
            SetPerformanceModIndexToStock(mods, VehicleModType.Transmission);
            SetPerformanceModIndexToStock(mods, VehicleModType.Armor);
            SetTogglePerformanceModState(mods, VehicleToggleModType.XenonHeadlights, false);
            SetTogglePerformanceModState(mods, VehicleToggleModType.Turbo, false);
            Function.Call(Hash.SET_VEHICLE_TYRES_CAN_BURST, vehicle, true);
        }

        private static void AddInstructionalButtonIfValid(NativeMenu menu, InstructionalButton button)
        {
            if (!string.IsNullOrEmpty(button.Description))
            {
                menu.Buttons.Add(button);
            }
        }


        public static void CreateMenus()
        {
            ItemCustomize = new NativeItem(CleanMenuText(Helper.GetLangEntry("BTN_CUSTOMIZE"), "Customize"));
            ItemConfirm = new NativeItem(CleanMenuText(Gxt("ITEM_YES"), "Confirm"));
            ItemColor = new NativeItem(CleanMenuText(Gxt("IB_COLOR"), "Color"), CleanMenuText(Gxt("CMOD_MOD_6_D"), string.Empty));
            ItemClassicColor = new NativeItem(CleanMenuText(Gxt("CMOD_COL1_1"), "Classic"), CleanMenuText(Gxt("CMOD_MOD_6_D"), string.Empty));
            ItemClassicColor2 = new NativeItem(CleanMenuText(Gxt("CMOD_COL1_1"), "Classic"), CleanMenuText(Gxt("CMOD_MOD_6_D"), string.Empty));
            ItemMetallicColor = new NativeItem(CleanMenuText(Gxt("CMOD_COL1_3"), "Metallic"), CleanMenuText(Gxt("CMOD_MOD_6_D"), string.Empty));
            ItemMetallicColor2 = new NativeItem(CleanMenuText(Gxt("CMOD_COL1_3"), "Metallic"), CleanMenuText(Gxt("CMOD_MOD_6_D"), string.Empty));
            ItemMetalColor = new NativeItem(CleanMenuText(Gxt("CMOD_COL1_4"), "Metal"), CleanMenuText(Gxt("CMOD_MOD_6_D"), string.Empty));
            ItemMetalColor2 = new NativeItem(CleanMenuText(Gxt("CMOD_COL1_4"), "Metal"), CleanMenuText(Gxt("CMOD_MOD_6_D"), string.Empty));
            ItemMatteColor = new NativeItem(CleanMenuText(Gxt("CMOD_COL1_5"), "Matte"), CleanMenuText(Gxt("CMOD_MOD_6_D"), string.Empty));
            ItemMatteColor2 = new NativeItem(CleanMenuText(Gxt("CMOD_COL1_5"), "Matte"), CleanMenuText(Gxt("CMOD_MOD_6_D"), string.Empty));
            ItemChromeColor = new NativeItem(CleanMenuText(Gxt("CMOD_COL1_0"), "Chrome"), CleanMenuText(Gxt("CMOD_MOD_6_D"), string.Empty));
            ItemChromeColor2 = new NativeItem(CleanMenuText(Gxt("CMOD_COL1_0"), "Chrome"), CleanMenuText(Gxt("CMOD_MOD_6_D"), string.Empty));
            ItemCPriColor = new NativeItem(CleanMenuText(Helper.GetLangEntry("BTN_CUSTOM_PRIMARY"), "Custom Primary"), CleanMenuText(Gxt("CMOD_MOD_6_D"), string.Empty));
            ItemCSecColor = new NativeItem(CleanMenuText(Helper.GetLangEntry("BTN_CUSTOM_SECONDARY"), "Custom Secondary"), CleanMenuText(Gxt("CMOD_MOD_6_D"), string.Empty));
            ItemPriColor = new NativeItem(CleanMenuText(Gxt("CMOD_COL0_0"), "Primary Color"), CleanMenuText(Gxt("CMOD_MOD_6_D"), string.Empty));
            ItemSecColor = new NativeItem(CleanMenuText(Gxt("CMOD_COL0_1"), "Secondary Color"), CleanMenuText(Gxt("CMOD_MOD_6_D"), string.Empty));
            ItemPeaColor = new NativeItem(CleanMenuText(Gxt("CMOD_COL1_6"), "Pearlescent"), CleanMenuText(Gxt("CMOD_MOD_6_D"), string.Empty));
            ItemPlate = new NativeItem(CleanMenuText(Gxt("CMOD_MOD_PLA"), "Plate Style"), CleanMenuText(Gxt("CMOD_MOD_6_D"), string.Empty));

            CreateCategoryMenu();
            CreateConfirmMenu();
            CreateCustomizeMenu();
            CreateColorCategory();
            ItemPerformance = new NativeItem(GetPerformanceUpgradeApplyTitle(), CleanMenuText(Gxt("IE_MOD_OBJ4"), string.Empty))
            {
                AltTitle = GetPerformanceUpgradeItemAltTitle(),
            };
            CustomiseMenu.Add(ItemPerformance);
            CustomiseMenu.Add(new NativeItem(CleanMenuText(Helper.GetLangEntry("BTN_PLATE_NUMBER_NAME"), "Plate Number"), CleanMenuText(Gxt("IE_MOD_OBJ2"), string.Empty)));
            PlateMenu = NewMenu(CleanMenuText(Gxt("CMOD_MOD_PLA"), "Plate Style"), true, CustomiseMenu, ItemPlate);
            CreatePrimaryColor();
            CreateSecondaryColor();
            CPriColorMenu = NewMenu(CleanMenuText(Helper.GetLangEntry("BTN_CUSTOM_PRIMARY"), "Custom Primary"), true, ColorMenu, ItemCPriColor);
            CSecColorMenu = NewMenu(CleanMenuText(Helper.GetLangEntry("BTN_CUSTOM_SECONDARY"), "Custom Secondary"), true, ColorMenu, ItemCSecColor);
            ClassicColorMenu = NewMenu(CleanMenuText(Gxt("CMOD_COL1_1"), "Classic"), true, PriColorMenu, ItemClassicColor);
            MetallicColorMenu = NewMenu(CleanMenuText(Gxt("CMOD_COL1_3"), "Metallic"), true, PriColorMenu, ItemMetallicColor);
            MatteColorMenu = NewMenu(CleanMenuText(Gxt("CMOD_COL1_5"), "Matte"), true, PriColorMenu, ItemMatteColor);
            MetalColorMenu = NewMenu(CleanMenuText(Gxt("CMOD_COL1_4"), "Metal"), true, PriColorMenu, ItemMetalColor);
            ChromeColorMenu = NewMenu(CleanMenuText(Gxt("CMOD_COL1_0"), "Chrome"), true, PriColorMenu, ItemChromeColor);
            PeaColorMenu = NewMenu(CleanMenuText(Gxt("CMOD_COL1_6"), "Pearlescent"), true, PriColorMenu, ItemPeaColor);
            ClassicColorMenu2 = NewMenu(CleanMenuText(Gxt("CMOD_COL1_1"), "Classic"), true, SecColorMenu, ItemClassicColor2);
            MetallicColorMenu2 = NewMenu(CleanMenuText(Gxt("CMOD_COL1_3"), "Metallic"), true, SecColorMenu, ItemMetallicColor2);
            MatteColorMenu2 = NewMenu(CleanMenuText(Gxt("CMOD_COL1_5"), "Matte"), true, SecColorMenu, ItemMatteColor2);
            MetalColorMenu2 = NewMenu(CleanMenuText(Gxt("CMOD_COL1_4"), "Metal"), true, SecColorMenu, ItemMetalColor2);
            ChromeColorMenu2 = NewMenu(CleanMenuText(Gxt("CMOD_COL1_0"), "Chrome"), true, SecColorMenu, ItemChromeColor2);
        }

        private static void RegisterMenu(NativeMenu menu, string title = null)
        {
            if (menu == null)
            {
                return;
            }

            if (!RegisteredMenus.Contains(menu))
            {
                RegisteredMenus.Add(menu);
            }

            string cleanTitle = CleanMenuText(title, null);
            if (!string.IsNullOrWhiteSpace(cleanTitle))
            {
                RegisteredMenuTitles[menu] = cleanTitle;
            }
            else if (!RegisteredMenuTitles.ContainsKey(menu))
            {
                RegisteredMenuTitles[menu] = "Menu";
            }
        }

        public static NativeMenu GetVisibleMenu()
        {
            for (int i = 0; i < RegisteredMenus.Count; i++)
            {
                NativeMenu menu = RegisteredMenus[i];
                if (menu != null && menu.Visible)
                {
                    return menu;
                }
            }

            return null;
        }

        public static string GetVisibleMenuTitle()
        {
            NativeMenu menu = GetVisibleMenu();
            if (menu != null && RegisteredMenuTitles.TryGetValue(menu, out string title))
            {
                return CleanMenuText(title, string.Empty);
            }

            return string.Empty;
        }

        private static void RemoveDuplicateParentRows(NativeMenu parentMenu, NativeItem parentItem, string fallbackTitle)
        {
            if (parentMenu == null)
            {
                return;
            }

            string targetTitle = CleanMenuText(parentItem?.Title, fallbackTitle);
            for (int i = parentMenu.Items.Count - 1; i >= 0; i--)
            {
                NativeItem existing = parentMenu.Items[i];
                if (existing == null)
                {
                    continue;
                }

                bool sameReference = parentItem != null && object.ReferenceEquals(existing, parentItem);
                bool sameTitle = string.Equals(CleanMenuText(existing.Title, string.Empty), targetTitle, StringComparison.OrdinalIgnoreCase);
                bool isSubmenuItem = existing is NativeSubmenuItem;

                if ((sameReference || sameTitle) && !isSubmenuItem)
                {
                    parentMenu.Items.RemoveAt(i);
                }
            }
        }

        private static NativeMenu NewMenu(string title, bool showStats)
        {
            NativeMenu menu = new NativeMenu(string.Empty, string.Empty)
            {
                MouseBehavior = GetConfiguredMouseBehavior(),
            };

            try
            {
                menu.Banner = new ScaledTexture(PointF.Empty, new SizeF(0f, 108f), "shopui_title_premium_deluxe_motorsport", "shopui_title_premium_deluxe_motorsport");
            }
            catch (Exception ex)
            {
                logger.Log("NewMenu banner setup failed for '" + (title ?? string.Empty) + "': " + ex.Message);
            }

            AddInstructionalButtons(menu);
            _menuPool ??= new ObjectPool();
            _menuPool.Add(menu);
            RegisterMenu(menu, title);
            return menu;
        }

        private static NativeMenu NewMenu(string title, bool showStats, NativeMenu parentMenu, NativeItem parentItem)
        {
            NativeMenu menu = NewMenu(title, showStats);
            RemoveDuplicateParentRows(parentMenu, parentItem, title);
            NativeSubmenuItem sub = new NativeSubmenuItem(menu, parentMenu, SubmenuIndicator);
            if (parentItem != null)
            {
                sub.Title = CleanMenuText(parentItem.Title, title);
                sub.Description = CleanMenuText(parentItem.Description, string.Empty);
                sub.Tag = parentItem.Tag;
            }
            else
            {
                sub.Title = CleanMenuText(title, "Submenu");
                sub.Description = string.Empty;
            }
            sub.Activated += (sender, args) => ShowOnly(menu);
            parentMenu.Add(sub);
            RememberSubmenuAltTitle(sub);
            menu.Closed += (sender, args) =>
            {
                if (!suppressCloseHandlers)
                {
                    ModsMenuCloseHandler(sender as NativeMenu);
                }
            };
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
            AddInstructionalButtonIfValid(menu, Helper.BtnZoomOut);
        }

        public static void HideAllMenus()
        {
            try
            {
                suppressCloseHandlers = true;
                for (int i = 0; i < RegisteredMenus.Count; i++)
                {
                    NativeMenu menu = RegisteredMenus[i];
                    if (menu != null)
                    {
                        menu.Visible = false;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(ex.Message + " " + ex.StackTrace);
            }
            finally
            {
                suppressCloseHandlers = false;
            }
        }

        public static void ShowOnly(NativeMenu menu)
        {
            try
            {
                HideAllMenus();
                if (menu != null)
                {
                    if (object.ReferenceEquals(menu, CustomiseMenu))
                    {
                        UpdatePerformanceUpgradeItemState();
                    }

                    RestoreSubmenuAltTitles(menu);
                    menu.Visible = true;
                    ResetSelection(menu);
                    lastVisibleMenu = menu;
                    lastVisibleMenuSeenAt = Game.GameTime;
                }
            }
            catch (Exception ex)
            {
                logger.Log(ex.Message + " " + ex.StackTrace);
            }
        }

        private static bool ShouldRecoverHiddenMenu()
        {
            return Helper.TaskScriptStatus == 0
                && Helper.TestDrive != 2
                && Helper.TestDrive != 3
                && !viewerActionInProgress
                && Helper.GPC != null
                && Helper.GPC.Exists()
                && !Helper.GPC.IsDead;
        }

        private static NativeMenu GetHiddenMenuFallback(NativeMenu hiddenMenu)
        {
            if (hiddenMenu == null)
            {
                return Helper.SelectedVehicle != null && Helper.VehPreview != null && Helper.VehPreview.Exists()
                    ? ConfirmMenu
                    : MainMenu;
            }

            if (object.ReferenceEquals(hiddenMenu, MainMenu))
            {
                return null;
            }

            if (object.ReferenceEquals(hiddenMenu, VehicleMenu) || object.ReferenceEquals(hiddenMenu, ConfirmMenu))
            {
                return MainMenu;
            }

            NativeMenu parent = hiddenMenu.Parent;
            if (parent != null && RegisteredMenus.Contains(parent))
            {
                return parent;
            }

            return Helper.SelectedVehicle != null && Helper.VehPreview != null && Helper.VehPreview.Exists()
                ? ConfirmMenu
                : MainMenu;
        }

        public static void RecoverHiddenMenuIfNeeded()
        {
            try
            {
                NativeMenu visibleMenu = GetVisibleMenu();
                if (visibleMenu != null)
                {
                    lastVisibleMenu = visibleMenu;
                    lastVisibleMenuSeenAt = Game.GameTime;
                    return;
                }

                if (!ShouldRecoverHiddenMenu())
                {
                    return;
                }

                if (lastVisibleMenuSeenAt > 0 && Game.GameTime - lastVisibleMenuSeenAt < HiddenMenuRecoveryDelayMs)
                {
                    return;
                }

                NativeMenu fallbackMenu = GetHiddenMenuFallback(lastVisibleMenu);
                if (fallbackMenu == null)
                {
                    MenuCloseHandler(lastVisibleMenu);
                    return;
                }

                ShowOnly(fallbackMenu);
            }
            catch (Exception ex)
            {
                logger.Log(ex.Message + " " + ex.StackTrace);
            }
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
            MainMenu = NewMenu(CleanMenuText(Gxt("CMOD_MOD_T"), "Categories"), true);

            NativeItem searchVehiclesItem = new NativeItem("~h~" + CleanMenuText(Helper.GetLangEntry("BTN_SEARCH_VEHICLES"), "Search Vehicles"))
            {
                Tag = CategoryActionSearchVehicles,
            };
            MainMenu.Add(searchVehiclesItem);

            HashSet<string> addedCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string file in System.IO.Directory.GetFiles(@".\scripts\PremiumDeluxeMotorsport\Vehicles\", "*.ini"))
            {
                if (System.IO.File.Exists(file))
                {
                    string categoryKey = System.IO.Path.GetFileNameWithoutExtension(file);
                    itemCat = new NativeItem(GetVehicleCategoryDisplayName(categoryKey))
                    {
                        AltTitle = SubmenuIndicator,
                    };
                    int categoryCount = System.IO.File.ReadAllLines(file).Length + GetAddonVehicleCountForCategory(categoryKey);
                    itemCat.Tag = Tuple.Create(categoryCount, categoryKey);
                    MainMenu.Add(itemCat);
                    addedCategories.Add(categoryKey);
                }
            }

            for (int i = 0; i < Helper.AddonVehicles.Count; i++)
            {
                Helper.AddonVehicleDefinition addon = Helper.AddonVehicles[i];
                if (addon == null || string.IsNullOrWhiteSpace(addon.CategoryKey) || addedCategories.Contains(addon.CategoryKey))
                {
                    continue;
                }

                itemCat = new NativeItem(GetVehicleCategoryDisplayName(addon.CategoryKey))
                {
                    AltTitle = SubmenuIndicator,
                };
                itemCat.Tag = Tuple.Create(GetAddonVehicleCountForCategory(addon.CategoryKey), addon.CategoryKey);
                MainMenu.Add(itemCat);
                addedCategories.Add(addon.CategoryKey);
            }

            ResetSelection(MainMenu);
            MainMenu.ItemActivated += (sender, args) => CategoryItemSelectHandler(sender as NativeMenu, args.Item, (sender as NativeMenu)?.SelectedIndex ?? 0);
            MainMenu.Closed += (sender, args) =>
            {
                if (!suppressCloseHandlers)
                {
                    MenuCloseHandler(sender as NativeMenu);
                }
            };
        }

        public static void CreateConfirmMenu()
        {
            ConfirmMenu = NewMenu(CleanMenuText(Helper.GetLangEntry("PURCHASE_ORDER"), "Purchase Order"), true);

            NativeItem testDriveItem = new NativeItem(CleanMenuText(Helper.GetLangEntry("BTN_TEST_DRIVE"), "Test Drive"))
            {
                Tag = ConfirmActionTestDrive,
            };
            ConfirmMenu.Add(testDriveItem);

            NativeItem purchaseItem = new NativeItem(CleanMenuText(Gxt("ITEM_YES"), "Confirm"))
            {
                Tag = ConfirmActionPurchase,
            };
            ConfirmMenu.Add(purchaseItem);

            ResetSelection(ConfirmMenu);
            ConfirmMenu.Closed += (sender, args) =>
            {
                if (!suppressCloseHandlers)
                {
                    ConfirmCloseHandler(sender as NativeMenu);
                }
            };
            ConfirmMenu.ItemActivated += (sender, args) => ItemSelectHandler(sender as NativeMenu, args.Item, (sender as NativeMenu)?.SelectedIndex ?? 0);
        }

        public static void CreateCustomizeMenu()
        {
            CustomiseMenu = NewMenu(CleanMenuText(Helper.GetLangEntry("BTN_CUSTOMIZE"), "Customize").ToUpperInvariant(), true, ConfirmMenu, ItemCustomize);
            ResetSelection(CustomiseMenu);
            CustomiseMenu.ItemActivated += (sender, args) => ItemSelectHandler(sender as NativeMenu, args.Item, (sender as NativeMenu)?.SelectedIndex ?? 0);
        }

        public static void CreateColorCategory()
        {
            ColorMenu = NewMenu(CleanMenuText(Gxt("CMOD_COL1_T"), "Colors"), true, CustomiseMenu, ItemColor);
            ResetSelection(ColorMenu);
        }

        public static void CreatePrimaryColor()
        {
            PriColorMenu = NewMenu(CleanMenuText(Gxt("CMOD_COL2_T"), "Primary Color"), true, ColorMenu, ItemPriColor);
            ResetSelection(PriColorMenu);
        }

        public static void CreateSecondaryColor()
        {
            SecColorMenu = NewMenu(CleanMenuText(Gxt("CMOD_COL3_T"), "Secondary Color"), true, ColorMenu, ItemSecColor);
            ResetSelection(SecColorMenu);
        }

        public static void MenuCloseHandler(NativeMenu sender)
        {
            try
            {
                if (suppressCloseHandlers)
                {
                    return;
                }
                Helper.TaskScriptStatus = -1;
                if (Helper.SelectedVehicle != null)
                {
                    Helper.SelectedVehicle = null;
                    Helper.VehPreview?.Delete();
                }
                PreviewVehicleBasePrice = 0;
                Helper.wsCamera.Stop();
                Helper.DrawSpotLight = false;
                Helper.HideHud = false;
                Helper.VehicleName = null;
                Helper.ShowVehicleName = false;
                HideAllMenus();
                ResetSelection(CustomiseMenu);
                ResetSelection(ConfirmMenu);
                ResetSelection(MainMenu);
                lastVisibleMenu = null;
                lastVisibleMenuSeenAt = 0;
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
                if (suppressCloseHandlers)
                {
                    return;
                }

                if (VehicleMenu != null && RegisteredMenus.Contains(VehicleMenu))
                {
                    ShowOnly(VehicleMenu);
                    ResetSelection(VehicleMenu);
                }
                else
                {
                    ShowOnly(MainMenu);
                }

                ResetSelection(CustomiseMenu);
                ResetSelection(ConfirmMenu);
            }
            catch (Exception ex)
            {
                logger.Log(ex.Message + ex.StackTrace);
            }
        }

        private static void MarkSelected(NativeItem item, bool selected)
        {
            if (item == null)
            {
                return;
            }

            if (item is NativeSubmenuItem)
            {
                RestoreSubmenuAltTitle(item);
                return;
            }

            item.AltTitle = selected ? SelectionMarker : string.Empty;
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
                            NativeItem item = new NativeItem(CleanMenuText(LocalizedLicensePlate(enumItem), enumItem.ToString())) { Tag = enumItem };
                            MarkSelected(item, Mods(Helper.VehPreview).LicensePlateStyle == enumItem);
                            menu.Add(item);
                        }
                        break;
                    case Helper.EnumTypes.VehicleWindowTint:
                        foreach (VehicleWindowTint enumItem in Enum.GetValues(typeof(VehicleWindowTint)))
                        {
                            NativeItem item = new NativeItem(CleanMenuText(Helper.LocalizedWindowsTint(enumItem), enumItem.ToString())) { Tag = enumItem };
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

            Tuple<string, int, string, string> t = selectedItem.Tag as Tuple<string, int, string, string>;
            if (t == null)
            {
                return;
            }

            string selectedFullVehicleName = t.Item3 ?? selectedItem.Title;

            if (string.Equals(selectedFullVehicleName, Helper.VehicleName, StringComparison.OrdinalIgnoreCase))
            {
                PreviewVehicleBasePrice = Math.Max(t.Item2, 0);
                RefreshPreviewVehiclePrice();
                ShowOnly(ConfirmMenu);
                Helper.VehicleName = selectedFullVehicleName;
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
                if (sender == null || index < 0 || index >= sender.Items.Count)
                {
                    return;
                }

                Tuple<string, int, string, string> t = sender.Items[index].Tag as Tuple<string, int, string, string>;
                if (t == null || sender.Items[index].Title.IndexOf("NULL", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return;
                }

                ClearPendingVehicleChange();

                Model model = new Model(t.Item1);
                if (!model.IsInCdImage || !model.IsValid)
                {
                    model.MarkAsNoLongerNeeded();
                    return;
                }

                PendingPreviewModel = model;
                PendingVehicleSelection = t;
                HasPendingVehicleChange = true;
                Function.Call(Hash.REQUEST_MODEL, model.Hash);
            }
            catch (Exception ex)
            {
                logger.Log(ex.Message + " " + ex.StackTrace);
            }
        }

        private static void ClearPendingVehicleChange()
        {
            if (HasPendingVehicleChange)
            {
                try
                {
                    PendingPreviewModel.MarkAsNoLongerNeeded();
                }
                catch
                {
                }
            }

            PendingVehicleSelection = null;
            HasPendingVehicleChange = false;
        }

        public static void ProcessPendingVehicleChange()
        {
            if (!HasPendingVehicleChange || PendingVehicleSelection == null)
            {
                return;
            }

            if (VehicleMenu == null || !VehicleMenu.Visible || Helper.TaskScriptStatus != 0)
            {
                ClearPendingVehicleChange();
                return;
            }

            try
            {
                Function.Call(Hash.REQUEST_MODEL, PendingPreviewModel.Hash);
                if (!Function.Call<bool>(Hash.HAS_MODEL_LOADED, PendingPreviewModel.Hash))
                {
                    return;
                }

                Tuple<string, int, string, string> selection = PendingVehicleSelection;
                Model model = PendingPreviewModel;
                PendingVehicleSelection = null;
                HasPendingVehicleChange = false;

                Vehicle previousVehicle = Helper.VehPreview;
                Vehicle nextVehicle = World.CreateVehicle(model, Helper.VehPreviewPos, Helper.Radius);
                model.MarkAsNoLongerNeeded();

                if (nextVehicle == null || !nextVehicle.Exists())
                {
                    return;
                }

                Helper.VehPreview = nextVehicle;
                Helper.SelectedVehicle = selection.Item3;
                Helper.VehicleName = selection.Item3;
                PreviewVehicleBasePrice = Math.Max(selection.Item2, 0);
                Helper.VehiclePrice = PreviewVehicleBasePrice;

                if (Helper.optRandomColor)
                {
                    Random r = new Random();
                    int psc = r.Next(0, 160);
                    Mods(nextVehicle).PrimaryColor = (VehicleColor)psc;
                    Mods(nextVehicle).SecondaryColor = (VehicleColor)psc;
                    Mods(nextVehicle).PearlescentColor = (VehicleColor)r.Next(0, 160);
                    Mods(nextVehicle).TrimColor = (VehicleColor)r.Next(0, 160);
                    Mods(nextVehicle).DashboardColor = (VehicleColor)r.Next(0, 160);
                    Mods(nextVehicle).RimColor = (VehicleColor)r.Next(0, 160);
                }

                Helper.UpdateVehPreview();
                nextVehicle.IsUndriveable = true;
                nextVehicle.LockStatus = VehicleLockStatus.IgnoredByPlayer;
                nextVehicle.DirtLevel = 0f;
                UpdatePerformanceUpgradeItemState();
                Helper.wsCamera.RetargetFor(nextVehicle);

                if (previousVehicle != null && previousVehicle.Exists() && previousVehicle.Handle != nextVehicle.Handle)
                {
                    previousVehicle.Delete();
                }

                CleanupVehicleViewerArea();
                Helper.optLastVehHash = nextVehicle.Model.Hash;
                Helper.optLastVehName = Helper.VehicleName;
                Helper.hiddenSave.SetValue("SETTINGS", "LASTVEHHASH", nextVehicle.Model.Hash);
                Helper.hiddenSave.SetValue("SETTINGS", "LASTVEHNAME", Helper.VehicleName);

                if (Helper.hiddenSave.GetValue("VEHICLES", nextVehicle.Model.Hash.ToString(), 0) == 0)
                {
                    Helper.hiddenSave.SetValue("VEHICLES", nextVehicle.Model.Hash.ToString(), 1);
                }

                Helper.hiddenSave.Save();
            }
            catch (Exception ex)
            {
                logger.Log(ex.Message + " " + ex.StackTrace);
                ClearPendingVehicleChange();
            }
        }

        private static bool TryWarpPlayerIntoVehicle(Vehicle vehicle)
        {
            if (vehicle == null || !vehicle.Exists() || Helper.GPC == null || !Helper.GPC.Exists())
            {
                return false;
            }

            try { Function.Call(Hash.CLEAR_PED_TASKS_IMMEDIATELY, Helper.GPC); } catch { }

            try
            {
                Function.Call(Hash.SET_PED_INTO_VEHICLE, Helper.GPC, vehicle, -1);
            }
            catch
            {
            }

            for (int i = 0; i < TestDriveWarpRetryFrames; i++)
            {
                Script.Yield();
                if (Helper.GPC.IsInVehicle(vehicle))
                {
                    return true;
                }
            }

            try
            {
                Function.Call(Hash.TASK_WARP_PED_INTO_VEHICLE, Helper.GPC, vehicle, -1);
            }
            catch
            {
            }

            for (int i = 0; i < TestDriveWarpRetryFrames; i++)
            {
                Script.Yield();
                if (Helper.GPC.IsInVehicle(vehicle))
                {
                    return true;
                }
            }

            return Helper.GPC.IsInVehicle(vehicle);
        }

        private static void RestoreViewerStateAfterFailedTestDrive()
        {
            if (Helper.VehPreview == null || !Helper.VehPreview.Exists())
            {
                return;
            }

            Helper.VehPreview.IsUndriveable = true;
            Helper.VehPreview.LockStatus = VehicleLockStatus.IgnoredByPlayer;
            Helper.VehPreview.Position = Helper.VehPreviewPos;
            Helper.VehPreview.Heading = Helper.Radius;

            try { Function.Call(Hash.SET_VEHICLE_DOORS_SHUT, Helper.VehPreview, false); } catch { }
            try { Function.Call(Hash.SET_VEHICLE_FIXED, Helper.VehPreview); } catch { }

            Helper.HideHud = true;
            Helper.ShowVehicleName = true;
            Helper.TaskScriptStatus = 0;
            Helper.TestDrive = 1;
            Helper.wsCamera.RepositionFor(Helper.VehPreview);
            ShowOnly(ConfirmMenu);
        }

        private static bool StartTestDrive()
        {
            if (Helper.VehPreview == null || !Helper.VehPreview.Exists() || Helper.GPC == null || !Helper.GPC.Exists())
            {
                return false;
            }

            FadeOut(200);
            Script.Wait(200);

            HideAllMenus();
            Helper.wsCamera.Stop();
            Helper.DrawSpotLight = false;
            Helper.HideHud = false;
            Helper.ShowVehicleName = false;

            Helper.TestDrive = 2;
            Helper.VehPreview.IsUndriveable = false;
            Helper.VehPreview.LockStatus = VehicleLockStatus.Unlocked;
            Helper.VehPreview.IsPositionFrozen = false;
            Helper.VehPreview.Position = TestDriveSpawnPosition;

            try { Function.Call(Hash.SET_VEHICLE_DOORS_SHUT, Helper.VehPreview, false); } catch { }
            try { Function.Call(Hash.SET_VEHICLE_ON_GROUND_PROPERLY, Helper.VehPreview); } catch { }

            bool warpSucceeded = TryWarpPlayerIntoVehicle(Helper.VehPreview);
            if (warpSucceeded)
            {
                Helper.TestDrive = 3;
                Script.Wait(200);
                FadeIn(200);
                return true;
            }

            RestoreViewerStateAfterFailedTestDrive();
            Script.Wait(200);
            FadeIn(200);
            return false;
        }

        public static void ItemSelectHandler(NativeMenu sender, NativeItem selectedItem, int index)
        {
            try
            {
                if (selectedItem == null)
                {
                    return;
                }

                string action = selectedItem.Tag as string;
                if ((action == ConfirmActionPurchase || action == ConfirmActionTestDrive) && viewerActionInProgress)
                {
                    return;
                }

                if (action == ConfirmActionPurchase || selectedItem.Title == Gxt("ITEM_YES"))
                {
                    viewerActionInProgress = true;
                    try
                    {
                        int currentCash = Game.Player.Money;
                        Helper.PlayerCash = currentCash;
                        if (currentCash >= Helper.VehiclePrice)
                        {
                            FadeOut(200);
                            Script.Wait(200);
                            Helper.GP.Money = currentCash - Helper.VehiclePrice;
                            HideAllMenus();
                            Helper.wsCamera.Stop();
                            Helper.DrawSpotLight = false;
                            Helper.VehPreview.IsUndriveable = false;
                            Helper.VehPreview.LockStatus = VehicleLockStatus.Unlocked;
                            Function.Call(Hash.SET_VEHICLE_DOORS_SHUT, Helper.VehPreview, false);
                            Helper.VehPreview.Position = TestDriveSpawnPosition;
                            MarkLegitimatePdmVehicle(Helper.VehPreview, 4000);
                            Function.Call(Hash.SET_PED_INTO_VEHICLE, Helper.GPC, Helper.VehPreview, -1);
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
                            Helper.DisplayNotificationThisFrame(Gxt("EMSTR_52"), string.Empty, Gxt("PI_BIK_HX8"), "CHAR_BANK_MAZE", true, Helper.IconType.RightJumpingArrow);
                        }
                    }
                    finally
                    {
                        viewerActionInProgress = false;
                    }
                }
                else if (action == ConfirmActionTestDrive || selectedItem.Title == Helper.GetLangEntry("BTN_TEST_DRIVE"))
                {
                    viewerActionInProgress = true;
                    try
                    {
                        if (StartTestDrive())
                        {
                            Helper.DisplayHelpTextThisFrame(Helper.GetLangEntry("HELP_TEST_DRIVE"));
                        }
                    }
                    finally
                    {
                        viewerActionInProgress = false;
                    }
                }

                if (object.ReferenceEquals(selectedItem, ItemPerformance))
                {
                    if (IsPerformanceUpgradeActive(Helper.VehPreview))
                    {
                        RemovePerformanceUpgrades(Helper.VehPreview);
                    }
                    else
                    {
                        ApplyPerformanceUpgrades(Helper.VehPreview);
                        Function.Call(Hash.ANIMPOSTFX_PLAY, "MP_corona_switch_supermod", 0, true);
                    }

                    UpdatePerformanceUpgradeItemState();
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
                if (selectedItem == null)
                {
                    return;
                }

                string categoryAction = selectedItem.Tag as string;
                if (categoryAction == CategoryActionSearchVehicles)
                {
                    ShowOnly(MainMenu);
                    string searchQuery = Game.GetUserInput(string.Empty);
                    if (string.IsNullOrWhiteSpace(searchQuery))
                    {
                        return;
                    }

                    if (CreateVehicleSearchMenu(searchQuery))
                    {
                        ShowOnly(VehicleMenu);
                    }
                    else
                    {
                        GtaScreen.ShowSubtitle("~r~No vehicles found for: ~w~" + searchQuery, 3500);
                        ShowOnly(MainMenu);
                    }

                    return;
                }

                Tuple<int, string> t = selectedItem.Tag as Tuple<int, string>;
                if (t == null)
                {
                    return;
                }

                CreateVehicleMenu($@".\scripts\PremiumDeluxeMotorsport\Vehicles\{t.Item2}.ini", GetVehicleCategoryDisplayName(t.Item2));
                ShowOnly(VehicleMenu);
            }
            catch (Exception ex)
            {
                logger.Log(ex.Message + " " + ex.StackTrace);
            }
        }

        private static bool DoesVehicleNameMatchSearch(string query, string localizedModelName, string rawModelName)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return false;
            }

            string trimmedQuery = query.Trim();
            return ContainsIgnoreCase(localizedModelName, trimmedQuery)
                || ContainsIgnoreCase(rawModelName, trimmedQuery);
        }

        private static void AddVehicleItemToMenu(NativeMenu menu, string modelName, int price, string fullVehicleName, string makeKey)
        {
            if (menu == null)
            {
                return;
            }

            string vehicleMenuTitle = BuildVehicleMenuTitle(fullVehicleName, price);
            NativeItem item = new NativeItem(vehicleMenuTitle)
            {
                AltTitle = "$" + price.ToString("N0"),
                Tag = Tuple.Create(modelName, price, fullVehicleName, makeKey),
            };

            Model model = new Model(modelName);
            if (model.IsInCdImage && model.IsValid)
            {
                menu.Add(item);
            }
        }

        private static bool CreateVehicleSearchMenu(string searchQuery)
        {
            try
            {
                if (VehicleMenu != null)
                {
                    VehicleMenu.Visible = false;
                }

                VehicleMenu = NewMenu(CleanMenuText(Helper.GetLangEntry("BTN_SEARCH_RESULTS"), "Search Results").ToUpperInvariant(), true);
                HashSet<int> existingModelHashes = new HashSet<int>();

                foreach (string file in System.IO.Directory.GetFiles(@".\scripts\PremiumDeluxeMotorsport\Vehicles\", "*.ini"))
                {
                    if (!System.IO.File.Exists(file))
                    {
                        continue;
                    }

                    Reader format = new Reader(file, Parameters);
                    for (int ii = 0; ii < format.Count; ii++)
                    {
                        int i = (format.Count - 1) - ii;
                        string configuredModelName = format[i]["model"];
                        Model configuredModel = new Model(configuredModelName);
                        if (configuredModel.IsValid)
                        {
                            existingModelHashes.Add(configuredModel.Hash);
                        }

                        decimal parsedPrice = decimal.TryParse(format[i]["price"], out decimal parsed) ? parsed : 0m;
                        int vehiclePrice = ResolveConfiguredVehiclePrice(configuredModelName, parsedPrice, format[i]["make"]);
                        string makeName = CleanMenuText(Gxt(format[i]["make"]), format[i]["make"]);
                        string localizedModelName = CleanMenuText(Gxt(format[i]["gxt"]), format[i]["name"]);
                        string rawModelName = CleanMenuText(format[i]["name"], localizedModelName);
                        string fullVehicleName = CleanMenuText((makeName + " " + localizedModelName).Trim(), rawModelName);

                        if (!DoesVehicleNameMatchSearch(searchQuery, localizedModelName, rawModelName))
                        {
                            continue;
                        }

                        AddVehicleItemToMenu(VehicleMenu, configuredModelName, vehiclePrice, fullVehicleName, format[i]["make"]);
                    }
                }

                AddAddonVehiclesToMenu(VehicleMenu, null, searchQuery, existingModelHashes);

                if (VehicleMenu.Items.Count == 0)
                {
                    return false;
                }

                ResetSelection(VehicleMenu);
                VehicleMenu.ItemActivated += (menuSender, args) => VehicleSelectHandler(menuSender as NativeMenu, args.Item, (menuSender as NativeMenu)?.SelectedIndex ?? 0);
                VehicleMenu.SelectedIndexChanged += (menuSender, args) => VehicleChangeHandler(menuSender as NativeMenu, args.Index);
                VehicleMenu.Closed += (menuSender, args) =>
                {
                    if (!suppressCloseHandlers)
                    {
                        ShowOnly(MainMenu);
                    }
                };

                return true;
            }
            catch (Exception ex)
            {
                logger.Log("Error CreateVehicleSearchMenu " + ex.Message + " " + ex.StackTrace);
                return false;
            }
        }

        public static void CreateVehicleMenu(string file, string subtitle)
        {
            try
            {
                if (VehicleMenu != null)
                {
                    VehicleMenu.Visible = false;
                }

                VehicleMenu = NewMenu(subtitle.ToUpperInvariant(), true);
                HashSet<int> existingModelHashes = GetConfiguredVehicleModelHashes();
                string categoryKey = System.IO.Path.GetFileNameWithoutExtension(file);

                if (System.IO.File.Exists(file))
                {
                    Reader format = new Reader(file, Parameters);
                    for (int ii = 0; ii < format.Count; ii++)
                    {
                        int i = (format.Count - 1) - ii;
                        string configuredModelName = format[i]["model"];
                        Model configuredModel = new Model(configuredModelName);
                        if (configuredModel.IsValid)
                        {
                            existingModelHashes.Add(configuredModel.Hash);
                        }

                        Helper.Price = decimal.TryParse(format[i]["price"], out decimal parsed) ? parsed : 0m;
                        int vehiclePrice = ResolveConfiguredVehiclePrice(configuredModelName, Helper.Price, format[i]["make"]);
                        string makeName = CleanMenuText(Gxt(format[i]["make"]), format[i]["make"]);
                        string modelName = CleanMenuText(Gxt(format[i]["gxt"]), format[i]["name"]);
                        string fullVehicleName = CleanMenuText(($"{makeName} {modelName}").Trim(), format[i]["name"]);
                        AddVehicleItemToMenu(VehicleMenu, configuredModelName, vehiclePrice, fullVehicleName, format[i]["make"]);
                    }
                }

                AddAddonVehiclesToMenu(VehicleMenu, categoryKey, null, existingModelHashes);
                ResetSelection(VehicleMenu);
                VehicleMenu.ItemActivated += (sender, args) => VehicleSelectHandler(sender as NativeMenu, args.Item, (sender as NativeMenu)?.SelectedIndex ?? 0);
                VehicleMenu.SelectedIndexChanged += (sender, args) => VehicleChangeHandler(sender as NativeMenu, args.Index);
                VehicleMenu.Closed += (sender, args) =>
                {
                    if (!suppressCloseHandlers)
                    {
                        ShowOnly(MainMenu);
                    }
                };
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

                ClearSelectionMarkers(sender);

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
                if (suppressCloseHandlers)
                {
                    return;
                }
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
