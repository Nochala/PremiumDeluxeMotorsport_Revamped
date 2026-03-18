using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI.Scaleform;
using Control = GTA.Control;

namespace PDMCD4
{
    public static class Helper
    {
        public static string VC_MOTORCYCLE, VC_COMPACT, VC_COUPE, VC_SEDAN, VC_SPORT, VC_CLASSIC, VC_SUPER, VC_MUSCLE, VC_OFF_ROAD, VC_SUV, VC_VAN;
        public static string VC_INDUSTRIAL, VC_BICYCLE, VC_BOAT, VC_HELI, VC_PLANE, VC_SERVICE, VC_EMERGENCY, VC_MILITARY, VC_COMMERCIAL, VC_UTILITY;

        public static ScriptSettings config = ScriptSettings.Load(@"scripts\PremiumDeluxeMotorsport\config.ini");
        public static ScriptSettings hiddenSave = ScriptSettings.Load(@"scripts\PremiumDeluxeMotorsport\database.ini");
        public static bool optRemoveColor = true;
        public static bool optRemoveImg = false;
        public static bool optRandomColor = true;
        public static bool optFade = true;
        public static int optLastVehHash = 0;
        public static string optLastVehName = null;
        public static string optLastVehMake = null;
        public static bool optLogging = true;
        public static Control keyZoom = Control.NextCamera;
        public static Control keyDoor = Control.ParachuteBrakeLeft;
        public static Control keyRoof = Control.VehicleRoof;
        public static Control keyCamera = Control.VehiclePushbikeSprint;

        public static InstructionalButton BtnRotLeft;
        public static InstructionalButton BtnRotRight;
        public static InstructionalButton BtnCamera;
        public static InstructionalButton BtnZoom;

        public static Vehicle VehPreview;
        public static Memory lastVehMemory;
        public static int TaskScriptStatus = -1;
        public static string SelectedVehicle;
        public static int PlayerCash;
        public static int VehiclePrice;
        public static Blip PdmBlip;
        public static bool HideHud = false, DrawSpotLight = false, ShowVehicleName = false;
        public static decimal Price = 0m;
        public static int Radius = 120, TestDrive = 1;
        public static string VehicleName = null;
        public static WorkshopCamera wsCamera = new WorkshopCamera();
        public static Vector3 PdmDoor, PlayerLastPos;
        public static Ped GPC, pdmPed;
        public static Player GP;
        public static float PdmDoorDist;
        public static Interior poly = new Interior(), testDeivePoly = new Interior();
        public static string blipName = "NULL";

        public static Vector3 VehPreviewPos = new Vector3(-44.45501f, -1096.976f, 26.42235f);
        public static Vector3 CameraPos = new Vector3(-47.45673f, -1101.28f, 27.54757f);
        public static Vector3 CameraRot = new Vector3(-18.12634f, 0f, -26.97177f);
        public static float PlayerHeading = 250.6701f;

        public static void LoadSettings()
        {
            optRemoveColor = config.GetValue("SETTINGS", "REMOVECOLOR", true);
            optRemoveImg = config.GetValue("SETTINGS", "REMOVESPRITE", false);
            optRandomColor = config.GetValue("SETTINGS", "RANDOMCOLOR", true);
            optFade = config.GetValue("SETTINGS", "FADEEFFECT", true);
            optLastVehHash = config.GetValue("SETTINGS", "LASTVEHHASH", -2022483795);
            optLastVehName = config.GetValue("SETTINGS", "LASTVEHNAME", "Pfister Comet Retro Custom");
            optLogging = config.GetValue("SETTINGS", "LOGGING", true);
            keyZoom = config.GetValue("CONTROLS", "ZOOM", Control.FrontendRt);
            keyDoor = config.GetValue("CONTROLS", "DOOR", Control.ParachuteBrakeLeft);
            keyRoof = config.GetValue("CONTROLS", "ROOF", Control.ParachuteBrakeRight);
            keyCamera = config.GetValue("CONTROLS", "CAMERA", Control.NextCamera);
        }

        private static string Gxt(string key) => Game.GetLocalizedString(key);

        public static string Name(this Ped ped)
        {
            switch ((uint)ped.Model.Hash)
            {
                case (uint)PedHash.Franklin:
                    return "Franklin";
                case (uint)PedHash.Michael:
                    return "Michael";
                case (uint)PedHash.Trevor:
                    return "Trevor";
                default:
                    return Game.Player.Name;
            }
        }

        public static void DisplayHelpTextThisFrame(string text)
        {
            Function.Call(Hash.BEGIN_TEXT_COMMAND_DISPLAY_HELP, "STRING");
            Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, text);
            Function.Call(Hash.END_TEXT_COMMAND_DISPLAY_HELP, 0, false, true, -1);
        }

        public enum IconType
        {
            ChatBox = 1,
            Email = 2,
            AddFriendRequest = 3,
            Nothing4 = 4,
            Nothing5 = 5,
            Nothing6 = 6,
            RightJumpingArrow = 7,
            RPIcon = 8,
            DollarSignIcon = 9,
        }

        public static void DisplayNotificationThisFrame(string sender, string subject, string message, string icon, bool flash, IconType type)
        {
            Function.Call(Hash.BEGIN_TEXT_COMMAND_THEFEED_POST, "STRING");
            Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, message);
            Function.Call(Hash.END_TEXT_COMMAND_THEFEED_POST_MESSAGETEXT, icon, icon, flash, (int)type, sender, subject);
            Function.Call(Hash.END_TEXT_COMMAND_THEFEED_POST_TICKER, false, true);
        }

        public enum ModType
        {
            Spoiler = 0,
            FBumper = 1,
            RBumper = 2,
            SSkirt = 3,
            Exhaust = 4,
            Frame = 5,
            Grille = 6,
            Hood = 7,
            Fender = 8,
            RFender = 9,
            Roof = 10,
            Engine = 11,
            Brakes = 12,
            Transmission = 13,
            Horns = 14,
            Suspension = 15,
            Armor = 16,
            FWheels = 23,
            BWheels = 24,
            PlateHolder = 25,
            TrimDesign = 27,
            Ornaments = 28,
            DialDesign = 30,
            Steering = 33,
            Shifter = 34,
            Plaques = 35,
            Hydraulics = 38,
        }

        public static string GetLangEntry(string lang)
        {
            string result = CFGRead.ReadCfgValue(lang, Application.StartupPath + @"\scripts\PremiumDeluxeMotorsport\Languages\" + Game.Language + ".cfg");
            return result ?? "NULL";
        }

        public static Vehicle CreateVehicle(string vehicleModel, Vector3 position, float heading = 0f)
        {
            Vehicle result = null;
            Model model = new Model(vehicleModel);
            model.Request(250);
            if (model.IsInCdImage && model.IsValid)
            {
                while (!model.IsLoaded)
                {
                    Script.Yield();
                }
                result = World.CreateVehicle(model, position, heading);
            }
            model.MarkAsNoLongerNeeded();
            return result;
        }

        public static Vehicle CreateVehicleFromHash(int vehicleHash, Vector3 position, float heading = 0f)
        {
            Vehicle result = null;
            Model model = new Model(vehicleHash);
            model.Request(250);
            if (model.IsInCdImage && model.IsValid)
            {
                while (!model.IsLoaded)
                {
                    Script.Yield();
                }
                result = World.CreateVehicle(model, position, heading);
            }
            model.MarkAsNoLongerNeeded();
            return result;
        }

        public static Prop CreateProp(int propModel, Vector3 position, Vector3 rotation)
        {
            Prop result = null;
            Model model = new Model(propModel);
            model.Request(250);
            if (model.IsInCdImage && model.IsValid)
            {
                while (!model.IsLoaded)
                {
                    Script.Wait(50);
                }
                result = World.CreateProp(model, position, rotation, false, false);
            }
            model.MarkAsNoLongerNeeded();
            return result;
        }

        public static void ToggleIPL(string iplToEnable, string iplToDisable)
        {
            if (Function.Call<bool>(Hash.IS_IPL_ACTIVE, iplToDisable))
            {
                Function.Call(Hash.REMOVE_IPL, iplToDisable);
                Function.Call(Hash.REQUEST_IPL, iplToEnable);
            }
            else
            {
                Function.Call(Hash.REQUEST_IPL, iplToEnable);
            }
        }

        public static void LoadMissingProps()
        {
            int showroomInterior = Function.Call<int>(Hash.GET_INTERIOR_AT_COORDS, -59.793598175048828, -1098.7840576171875, 27.2612);
            Function.Call(Hash.ACTIVATE_INTERIOR_ENTITY_SET, showroomInterior, "csr_beforeMission");
            Function.Call(Hash.REFRESH_INTERIOR, showroomInterior);
        }

        public enum EnumTypes
        {
            NumberPlateType,
            VehicleColorPrimary,
            VehicleColorSecondary,
            VehicleColorTrim,
            VehicleColorDashboard,
            VehicleColorRim,
            VehicleColorAccent,
            vehicleColorPearlescent,
            VehicleWindowTint,
        }

        public static bool IsCustomWheels() => VehPreview != null && Function.Call<bool>(Hash.GET_VEHICLE_MOD_VARIATION, VehPreview.Handle, 23);

        public static int GetInteriorID(Vector3 interior) => Function.Call<int>(Hash.GET_INTERIOR_AT_COORDS, interior.X, interior.Y, interior.Z);

        public static bool DoesGXTEntryExist(string entry) => Function.Call<bool>(Hash.DOES_TEXT_LABEL_EXIST, entry);

        private static int GetVehicleModIndex(Vehicle vehicle, int modType)
            => vehicle == null ? -1 : Function.Call<int>(Hash.GET_VEHICLE_MOD, vehicle.Handle, modType);

        private static bool IsVehicleToggleModOn(Vehicle vehicle, int modType)
            => vehicle != null && Function.Call<bool>(Hash.IS_TOGGLE_MOD_ON, vehicle.Handle, modType);

        private static bool IsVehicleNeonLightOn(Vehicle vehicle, VehicleNeonLight light)
            => vehicle != null && vehicle.Mods.IsNeonLightsOn(light);

        public static readonly List<VehicleColor> ClassicColor = new List<VehicleColor>
        {
            (VehicleColor)0, (VehicleColor)147, (VehicleColor)1, (VehicleColor)11, (VehicleColor)2, (VehicleColor)3, (VehicleColor)4, (VehicleColor)5, (VehicleColor)6, (VehicleColor)7, (VehicleColor)8, (VehicleColor)9, (VehicleColor)10, (VehicleColor)27, (VehicleColor)28, (VehicleColor)29, (VehicleColor)150, (VehicleColor)30, (VehicleColor)31, (VehicleColor)32, (VehicleColor)33, (VehicleColor)34, (VehicleColor)143, (VehicleColor)35, (VehicleColor)135, (VehicleColor)137, (VehicleColor)136, (VehicleColor)36, (VehicleColor)38, (VehicleColor)138, (VehicleColor)99, (VehicleColor)90, (VehicleColor)88, (VehicleColor)89, (VehicleColor)91, (VehicleColor)49, (VehicleColor)50, (VehicleColor)51, (VehicleColor)52, (VehicleColor)53, (VehicleColor)54, (VehicleColor)92, (VehicleColor)141, (VehicleColor)61, (VehicleColor)62, (VehicleColor)63, (VehicleColor)64, (VehicleColor)65, (VehicleColor)66, (VehicleColor)67, (VehicleColor)68, (VehicleColor)69, (VehicleColor)73, (VehicleColor)70, (VehicleColor)74, (VehicleColor)96, (VehicleColor)101, (VehicleColor)95, (VehicleColor)94, (VehicleColor)97, (VehicleColor)103, (VehicleColor)104, (VehicleColor)98, (VehicleColor)100, (VehicleColor)102, (VehicleColor)99, (VehicleColor)105, (VehicleColor)106, (VehicleColor)71, (VehicleColor)72, (VehicleColor)142, (VehicleColor)145, (VehicleColor)107, (VehicleColor)111, (VehicleColor)112
        };

        public static readonly List<VehicleColor> MatteColor = new List<VehicleColor>
        {
            (VehicleColor)12, (VehicleColor)13, (VehicleColor)14, (VehicleColor)131, (VehicleColor)83, (VehicleColor)82, (VehicleColor)84, (VehicleColor)149, (VehicleColor)148, (VehicleColor)39, (VehicleColor)40, (VehicleColor)41, (VehicleColor)42, (VehicleColor)55, (VehicleColor)128, (VehicleColor)151, (VehicleColor)155, (VehicleColor)152, (VehicleColor)153, (VehicleColor)154
        };

        public static readonly List<VehicleColor> MetalColor = new List<VehicleColor>
        {
            (VehicleColor)117, (VehicleColor)118, (VehicleColor)119, (VehicleColor)158, (VehicleColor)159, (VehicleColor)160
        };

        public static readonly List<VehicleColor> ChromeColor = new List<VehicleColor>
        {
            (VehicleColor)120
        };

        public static readonly List<VehicleColor> PearlescentColor = new List<VehicleColor>
        {
            (VehicleColor)0, (VehicleColor)147, (VehicleColor)1, (VehicleColor)11, (VehicleColor)2, (VehicleColor)3, (VehicleColor)4, (VehicleColor)5, (VehicleColor)6, (VehicleColor)7, (VehicleColor)8, (VehicleColor)9, (VehicleColor)10, (VehicleColor)27, (VehicleColor)28, (VehicleColor)29, (VehicleColor)150, (VehicleColor)30, (VehicleColor)31, (VehicleColor)32, (VehicleColor)33, (VehicleColor)34, (VehicleColor)143, (VehicleColor)35, (VehicleColor)135, (VehicleColor)137, (VehicleColor)136, (VehicleColor)36, (VehicleColor)38, (VehicleColor)138, (VehicleColor)99, (VehicleColor)90, (VehicleColor)88, (VehicleColor)89, (VehicleColor)91, (VehicleColor)49, (VehicleColor)50, (VehicleColor)51, (VehicleColor)52, (VehicleColor)53, (VehicleColor)54, (VehicleColor)92, (VehicleColor)141, (VehicleColor)61, (VehicleColor)62, (VehicleColor)63, (VehicleColor)64, (VehicleColor)65, (VehicleColor)66, (VehicleColor)67, (VehicleColor)68, (VehicleColor)69, (VehicleColor)73, (VehicleColor)70, (VehicleColor)74, (VehicleColor)96, (VehicleColor)101, (VehicleColor)95, (VehicleColor)94, (VehicleColor)97, (VehicleColor)103, (VehicleColor)104, (VehicleColor)98, (VehicleColor)100, (VehicleColor)102, (VehicleColor)99, (VehicleColor)105, (VehicleColor)106, (VehicleColor)71, (VehicleColor)72, (VehicleColor)142, (VehicleColor)145, (VehicleColor)107, (VehicleColor)111, (VehicleColor)112, (VehicleColor)117, (VehicleColor)118, (VehicleColor)119, (VehicleColor)158, (VehicleColor)159, (VehicleColor)160
        };

        private static readonly Dictionary<VehicleColor, Tuple<string, string>> _colorNames =
            new Dictionary<VehicleColor, Tuple<string, string>>
            {
                [(VehicleColor)0] = Tuple.Create("BLACK", "MetallicBlack"),
                [(VehicleColor)1] = Tuple.Create("GRAPHITE", "MetallicGraphiteBlack"),
                [(VehicleColor)2] = Tuple.Create("BLACK_STEEL", "MetallicBlackSteel"),
                [(VehicleColor)3] = Tuple.Create("DARK_SILVER", "MetallicDarkSilver"),
                [(VehicleColor)4] = Tuple.Create("SILVER", "MetallicSilver"),
                [(VehicleColor)5] = Tuple.Create("BLUE_SILVER", "MetallicBlueSilver"),
                [(VehicleColor)6] = Tuple.Create("ROLLED_STEEL", "MetallicSteelGray"),
                [(VehicleColor)7] = Tuple.Create("SHADOW_SILVER", "MetallicShadowSilver"),
                [(VehicleColor)8] = Tuple.Create("STONE_SILVER", "MetallicStoneSilver"),
                [(VehicleColor)9] = Tuple.Create("MIDNIGHT_SILVER", "MetallicMidnightSilver"),
                [(VehicleColor)10] = Tuple.Create("CAST_IRON_SIL", "MetallicGunMetal"),
                [(VehicleColor)11] = Tuple.Create("ANTHR_BLACK", "MetallicAnthraciteGray"),
                [(VehicleColor)12] = Tuple.Create("BLACK", "MatteBlack"),
                [(VehicleColor)13] = Tuple.Create("GREY", "MatteGray"),
                [(VehicleColor)14] = Tuple.Create("LIGHT_GREY", "MatteLightGray"),
                [(VehicleColor)15] = Tuple.Create("BLACK", "UtilBlack"),
                [(VehicleColor)16] = Tuple.Create("BLACK", "UtilBlackPoly"),
                [(VehicleColor)17] = Tuple.Create("DARK_SILVER", "UtilDarksilver"),
                [(VehicleColor)18] = Tuple.Create("SILVER", "UtilSilver"),
                [(VehicleColor)19] = Tuple.Create("CAST_IRON_SIL", "UtilGunMetal"),
                [(VehicleColor)20] = Tuple.Create("SHADOW_SILVER", "UtilShadowSilver"),
                [(VehicleColor)21] = Tuple.Create("BLACK", "WornBlack"),
                [(VehicleColor)22] = Tuple.Create("GRAPHITE", "WornGraphite"),
                [(VehicleColor)23] = Tuple.Create("ROLLED_STEEL", "WornSilverGray"),
                [(VehicleColor)24] = Tuple.Create("SILVER", "WornSilver"),
                [(VehicleColor)25] = Tuple.Create("BLUE_SILVER", "WornBlueSilver"),
                [(VehicleColor)26] = Tuple.Create("SHADOW_SILVER", "WornShadowSilver"),
                [(VehicleColor)27] = Tuple.Create("RED", "MetallicRed"),
                [(VehicleColor)28] = Tuple.Create("TORINO_RED", "MetallicTorinoRed"),
                [(VehicleColor)29] = Tuple.Create("FORMULA_RED", "MetallicFormulaRed"),
                [(VehicleColor)30] = Tuple.Create("BLAZE_RED", "MetallicBlazeRed"),
                [(VehicleColor)31] = Tuple.Create("GRACE_RED", "MetallicGracefulRed"),
                [(VehicleColor)32] = Tuple.Create("GARNET_RED", "MetallicGarnetRed"),
                [(VehicleColor)33] = Tuple.Create("SUNSET_RED", "MetallicDesertRed"),
                [(VehicleColor)34] = Tuple.Create("CABERNET_RED", "MetallicCabernetRed"),
                [(VehicleColor)35] = Tuple.Create("CANDY_RED", "MetallicCandyRed"),
                [(VehicleColor)36] = Tuple.Create("SUNRISE_ORANGE", "MetallicSunriseOrange"),
                [(VehicleColor)37] = Tuple.Create("GOLD", "MetallicClassicGold"),
                [(VehicleColor)38] = Tuple.Create("ORANGE", "MetallicOrange"),
                [(VehicleColor)39] = Tuple.Create("RED", "MatteRed"),
                [(VehicleColor)40] = Tuple.Create("DARK_RED", "MatteDarkRed"),
                [(VehicleColor)41] = Tuple.Create("ORANGE", "MatteOrange"),
                [(VehicleColor)42] = Tuple.Create("YELLOW", "MatteYellow"),
                [(VehicleColor)43] = Tuple.Create("RED", "UtilRed"),
                [(VehicleColor)44] = Tuple.Create("NULL", "UtilBrightRed"),
                [(VehicleColor)45] = Tuple.Create("GARNET_RED", "UtilGarnetRed"),
                [(VehicleColor)46] = Tuple.Create("RED", "WornRed"),
                [(VehicleColor)47] = Tuple.Create("NULL", "WornGoldenRed"),
                [(VehicleColor)48] = Tuple.Create("DARK_RED", "WornDarkRed"),
                [(VehicleColor)49] = Tuple.Create("DARK_GREEN", "MetallicDarkGreen"),
                [(VehicleColor)50] = Tuple.Create("RACING_GREEN", "MetallicRacingGreen"),
                [(VehicleColor)51] = Tuple.Create("SEA_GREEN", "MetallicSeaGreen"),
                [(VehicleColor)52] = Tuple.Create("OLIVE_GREEN", "MetallicOliveGreen"),
                [(VehicleColor)53] = Tuple.Create("BRIGHT_GREEN", "MetallicGreen"),
                [(VehicleColor)54] = Tuple.Create("PETROL_GREEN", "MetallicGasolineBlueGreen"),
                [(VehicleColor)55] = Tuple.Create("LIME_GREEN", "MatteLimeGreen"),
                [(VehicleColor)56] = Tuple.Create("DARK_GREEN", "UtilDarkGreen"),
                [(VehicleColor)57] = Tuple.Create("GREEN", "UtilGreen"),
                [(VehicleColor)58] = Tuple.Create("DARK_GREEN", "WornDarkGreen"),
                [(VehicleColor)59] = Tuple.Create("GREEN", "WornGreen"),
                [(VehicleColor)60] = Tuple.Create("NULL", "WornSeaWash"),
                [(VehicleColor)61] = Tuple.Create("GALAXY_BLUE", "MetallicMidnightBlue"),
                [(VehicleColor)62] = Tuple.Create("DARK_BLUE", "MetallicDarkBlue"),
                [(VehicleColor)63] = Tuple.Create("SAXON_BLUE", "MetallicSaxonyBlue"),
                [(VehicleColor)64] = Tuple.Create("BLUE", "MetallicBlue"),
                [(VehicleColor)65] = Tuple.Create("MARINER_BLUE", "MetallicMarinerBlue"),
                [(VehicleColor)66] = Tuple.Create("HARBOR_BLUE", "MetallicHarborBlue"),
                [(VehicleColor)67] = Tuple.Create("DIAMOND_BLUE", "MetallicDiamondBlue"),
                [(VehicleColor)68] = Tuple.Create("SURF_BLUE", "MetallicSurfBlue"),
                [(VehicleColor)69] = Tuple.Create("NAUTICAL_BLUE", "MetallicNauticalBlue"),
                [(VehicleColor)70] = Tuple.Create("ULTRA_BLUE", "MetallicBrightBlue"),
                [(VehicleColor)71] = Tuple.Create("PURPLE", "MetallicPurpleBlue"),
                [(VehicleColor)72] = Tuple.Create("SPIN_PURPLE", "MetallicSpinnakerBlue"),
                [(VehicleColor)73] = Tuple.Create("RACING_BLUE", "MetallicUltraBlue"),
                [(VehicleColor)74] = Tuple.Create("LIGHT_BLUE", "MetallicLightBlue"),
                [(VehicleColor)75] = Tuple.Create("DARK_BLUE", "UtilDarkBlue"),
                [(VehicleColor)76] = Tuple.Create("MIDNIGHT_BLUE", "UtilMidnightBlue"),
                [(VehicleColor)77] = Tuple.Create("BLUE", "UtilBlue"),
                [(VehicleColor)78] = Tuple.Create("NULL", "UtilSeaFoamBlue"),
                [(VehicleColor)79] = Tuple.Create("LIGHT_BLUE", "UtilLightningBlue"),
                [(VehicleColor)80] = Tuple.Create("NULL", "UtilMauiBluePoly"),
                [(VehicleColor)81] = Tuple.Create("NULL", "UtilBrightBlue"),
                [(VehicleColor)82] = Tuple.Create("DARK_BLUE", "MatteDarkBlue"),
                [(VehicleColor)83] = Tuple.Create("BLUE", "MatteBlue"),
                [(VehicleColor)84] = Tuple.Create("MIDNIGHT_BLUE", "MatteMidnightBlue"),
                [(VehicleColor)85] = Tuple.Create("DARK_BLUE", "WornDarkBlue"),
                [(VehicleColor)86] = Tuple.Create("BLUE", "WornBlue"),
                [(VehicleColor)87] = Tuple.Create("LIGHT_BLUE", "WornLightBlue"),
                [(VehicleColor)88] = Tuple.Create("YELLOW", "MetallicTaxiYellow"),
                [(VehicleColor)89] = Tuple.Create("RACE_YELLOW", "MetallicRaceYellow"),
                [(VehicleColor)90] = Tuple.Create("BRONZE", "MetallicBronze"),
                [(VehicleColor)91] = Tuple.Create("FLUR_YELLOW", "MetallicYellowBird"),
                [(VehicleColor)92] = Tuple.Create("LIME_GREEN", "MetallicLime"),
                [(VehicleColor)93] = Tuple.Create("NULL", "MetallicChampagne"),
                [(VehicleColor)94] = Tuple.Create("UMBER_BROWN", "MetallicPuebloBeige"),
                [(VehicleColor)95] = Tuple.Create("CREEK_BROWN", "MetallicDarkIvory"),
                [(VehicleColor)96] = Tuple.Create("CHOCOLATE_BROWN", "MetallicChocoBrown"),
                [(VehicleColor)97] = Tuple.Create("MAPLE_BROWN", "MetallicGoldenBrown"),
                [(VehicleColor)98] = Tuple.Create("SADDLE_BROWN", "MetallicLightBrown"),
                [(VehicleColor)99] = Tuple.Create("STRAW_BROWN", "MetallicStrawBeige"),
                [(VehicleColor)100] = Tuple.Create("MOSS_BROWN", "MetallicMossBrown"),
                [(VehicleColor)101] = Tuple.Create("BISON_BROWN", "MetallicBistonBrown"),
                [(VehicleColor)102] = Tuple.Create("WOODBEECH_BROWN", "MetallicBeechwood"),
                [(VehicleColor)103] = Tuple.Create("NULL", "MetallicDarkBeechwood"),
                [(VehicleColor)104] = Tuple.Create("SIENNA_BROWN", "MetallicChocoOrange"),
                [(VehicleColor)105] = Tuple.Create("SANDY_BROWN", "MetallicBeachSand"),
                [(VehicleColor)106] = Tuple.Create("BLEECHED_BROWN", "MetallicSunBleechedSand"),
                [(VehicleColor)107] = Tuple.Create("CREAM", "MetallicCream"),
                [(VehicleColor)108] = Tuple.Create("BROWN", "UtilBrown"),
                [(VehicleColor)109] = Tuple.Create("NULL", "UtilMediumBrown"),
                [(VehicleColor)110] = Tuple.Create("NULL", "UtilLightBrown"),
                [(VehicleColor)111] = Tuple.Create("WHITE", "MetallicWhite"),
                [(VehicleColor)112] = Tuple.Create("FROST_WHITE", "MetallicFrostWhite"),
                [(VehicleColor)113] = Tuple.Create("NULL", "WornHoneyBeige"),
                [(VehicleColor)114] = Tuple.Create("BROWN", "WornBrown"),
                [(VehicleColor)115] = Tuple.Create("DARK_BROWN", "WornDarkBrown"),
                [(VehicleColor)116] = Tuple.Create("STRAW_BROWN", "WornStrawBeige"),
                [(VehicleColor)117] = Tuple.Create("BR_STEEL", "BrushedSteel"),
                [(VehicleColor)118] = Tuple.Create("BR BLACK_STEEL", "BrushedBlackSteel"),
                [(VehicleColor)119] = Tuple.Create("BR_ALUMINIUM", "BrushedAluminium"),
                [(VehicleColor)120] = Tuple.Create("CHROME", "Chrome"),
                [(VehicleColor)121] = Tuple.Create("NULL", "WornOffWhite"),
                [(VehicleColor)122] = Tuple.Create("NULL", "UtilOffWhite"),
                [(VehicleColor)123] = Tuple.Create("ORANGE", "WornOrange"),
                [(VehicleColor)124] = Tuple.Create("NULL", "WornLightOrange"),
                [(VehicleColor)125] = Tuple.Create("NULL", "MetallicSecuricorGreen"),
                [(VehicleColor)126] = Tuple.Create("YELLOW", "WornTaxiYellow"),
                [(VehicleColor)127] = Tuple.Create("NULL", "PoliceCarBlue"),
                [(VehicleColor)128] = Tuple.Create("GREEN", "MatteGreen"),
                [(VehicleColor)129] = Tuple.Create("BROWN", "MatteBrown"),
                [(VehicleColor)130] = Tuple.Create("NULL", "SteelBlue"),
                [(VehicleColor)131] = Tuple.Create("WHITE", "MatteWhite"),
                [(VehicleColor)132] = Tuple.Create("WHITE", "WornWhite"),
                [(VehicleColor)133] = Tuple.Create("OLIVE_GREEN", "WornOliveArmyGreen"),
                [(VehicleColor)134] = Tuple.Create("WHITE", "PureWhite"),
                [(VehicleColor)135] = Tuple.Create("HOT PINK", "HotPink"),
                [(VehicleColor)136] = Tuple.Create("SALMON_PINK", "Salmonpink"),
                [(VehicleColor)137] = Tuple.Create("PINK", "MetallicVermillionPink"),
                [(VehicleColor)138] = Tuple.Create("BRIGHT_ORANGE", "Orange"),
                [(VehicleColor)139] = Tuple.Create("GREEN", "Green"),
                [(VehicleColor)140] = Tuple.Create("BLUE", "Blue"),
                [(VehicleColor)141] = Tuple.Create("MIDNIGHT_BLUE", "MettalicBlackBlue"),
                [(VehicleColor)142] = Tuple.Create("MIGHT_PURPLE", "MetallicBlackPurple"),
                [(VehicleColor)143] = Tuple.Create("WINE_RED", "MetallicBlackRed"),
                [(VehicleColor)144] = Tuple.Create("NULL", "HunterGreen"),
                [(VehicleColor)145] = Tuple.Create("BRIGHT_PURPLE", "MetallicPurple"),
                [(VehicleColor)146] = Tuple.Create("MIGHT_PURPLE", "MetaillicVDarkBlue"),
                [(VehicleColor)147] = Tuple.Create("BLACK_GRAPHITE", "ModshopBlack1"),
                [(VehicleColor)148] = Tuple.Create("PURPLE", "MattePurple"),
                [(VehicleColor)149] = Tuple.Create("MIGHT_PURPLE", "MatteDarkPurple"),
                [(VehicleColor)150] = Tuple.Create("LAVA_RED", "MetallicLavaRed"),
                [(VehicleColor)151] = Tuple.Create("MATTE_FOR", "MatteForestGreen"),
                [(VehicleColor)152] = Tuple.Create("MATTE_OD", "MatteOliveDrab"),
                [(VehicleColor)153] = Tuple.Create("MATTE_DIRT", "MatteDesertBrown"),
                [(VehicleColor)154] = Tuple.Create("MATTE_DESERT", "MatteDesertTan"),
                [(VehicleColor)155] = Tuple.Create("MATTE_FOIL", "MatteFoliageGreen"),
                [(VehicleColor)156] = Tuple.Create("NULL", "DefaultAlloyColor"),
                [(VehicleColor)157] = Tuple.Create("NULL", "EpsilonBlue"),
                [(VehicleColor)158] = Tuple.Create("GOLD_P", "PureGold"),
                [(VehicleColor)159] = Tuple.Create("GOLD_S", "BrushedGold"),
                [(VehicleColor)160] = Tuple.Create("NULL", "SecretGold")
            };

        public static string GetLocalizedColorName(VehicleColor vehColor)
        {
            if (!Function.Call<bool>(Hash.HAS_THIS_ADDITIONAL_TEXT_LOADED, "mod_mnu", 10))
            {
                Function.Call(Hash.CLEAR_ADDITIONAL_TEXT, 10, true);
                Function.Call(Hash.REQUEST_ADDITIONAL_TEXT, "mod_mnu", 10);
            }

            if (_colorNames.ContainsKey(vehColor))
            {
                Tuple<string, string> data = _colorNames[vehColor];
                if (DoesGXTEntryExist(data.Item1))
                {
                    return Gxt(data.Item1);
                }

                return Regex.Replace(data.Item2, "[A-Z]", " ${0}").Trim();
            }

            throw new ArgumentException("Vehicle Color is undefined", nameof(vehColor));
        }

        public static string LocalizedWindowsTint(VehicleWindowTint tint)
        {
            switch (tint)
            {
                case VehicleWindowTint.DarkSmoke:
                    return Gxt("CMOD_WIN_2");
                case VehicleWindowTint.Green:
                    return Gxt("GREEN");
                case VehicleWindowTint.LightSmoke:
                    return Gxt("CMOD_WIN_1");
                case VehicleWindowTint.Limo:
                    return Gxt("CMOD_WIN_3");
                case VehicleWindowTint.None:
                    return Gxt("CMOD_WIN_0");
                case VehicleWindowTint.PureBlack:
                    return Gxt("CMOD_WIN_5");
                case VehicleWindowTint.Stock:
                    return Gxt("CMOD_WIN_4");
                default:
                    return null;
            }
        }

        public static string LocalizedLicensePlate(LicensePlateStyle plateStyle)
        {
            switch ((int)plateStyle)
            {
                case 0:
                    return Gxt("CMOD_PLA_0");
                case 1:
                    return Gxt("CMOD_PLA_1");
                case 2:
                    return Gxt("CMOD_PLA_2");
                case 3:
                    return Gxt("CMOD_MOD_GLD2");
                case 4:
                    return Gxt("CMOD_PLA_4");
                case 5:
                    return Gxt("CMOD_PLA_3");
                default:
                    return null;
            }
        }

        public static string GetClassDisplayName(this Vehicle vehicle)
        {
            return Gxt("VEH_CLASS_" + ((int)vehicle.ClassType));
        }

        public static void AttachTo(Entity entity1, Entity entity2, int boneindex, Vector3 position, Vector3 rotation)
        {
            Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY, entity1.Handle, entity2.Handle, boneindex, position.X, position.Y, position.Z, rotation.X, rotation.Y, rotation.Z, false, false, true, false, 2, true);
        }

        public static float GetVehAcceleration(Vehicle veh)
        {
            float result = Function.Call<float>(Hash.GET_VEHICLE_ACCELERATION, veh) * 100f * 4.4f;
            if (result >= 200f) result = 200f;
            return result;
        }

        public static float GetVehTopSpeed(Vehicle veh)
        {
            float result = (Function.Call<float>((Hash)0x53AF99BAA671CA47, veh) * 3600f / 1609.344f) * 1.9f;
            if (result >= 200f) result = 200f;
            return result;
        }

        public static float GetVehBraking(Vehicle veh)
        {
            float result = Function.Call<float>(Hash.GET_VEHICLE_MAX_BRAKING, veh) * 70f;
            if (result >= 200f) result = 200f;
            return result;
        }

        public static float GetVehTraction(Vehicle veh)
        {
            float result = Function.Call<float>(Hash.GET_VEHICLE_MAX_TRACTION, veh) * 6.5f;
            if (result >= 200f) result = 200f;
            return result;
        }

        public static bool RequestAdditionTextFile(string textname, int timeout = 1000)
        {
            if (!Function.Call<bool>(Hash.HAS_THIS_ADDITIONAL_TEXT_LOADED, textname, 9))
            {
                Function.Call(Hash.CLEAR_ADDITIONAL_TEXT, 9, true);
                Function.Call(Hash.REQUEST_ADDITIONAL_TEXT, textname, 9);
                int end = Game.GameTime + timeout;

                while (Game.GameTime < end)
                {
                    if (Function.Call<bool>(Hash.HAS_THIS_ADDITIONAL_TEXT_LOADED, textname, 9))
                    {
                        return true;
                    }
                    Script.Yield();
                }

                return false;
            }

            return true;
        }

        public enum GlobalValue
        {
            b1_0_757_4 = 0x271803,
            b1_0_791_2 = 0x272A34,
            b1_0_877_1 = 0x2750BD,
            b1_0_944_2 = 0x279476,
            b1_0_1032_1 = 2593970,
            b1_0_1103_2 = 2599337,
            b1_0_1180_2 = 2606794,
            b1_0_1365_1 = 4265719,
            b1_0_1493_1 = 4266042,
            b1_0_1604_1 = 4266905,
            b1_0_1737_0 = 4267883,
            b1_0_1868_0 = 4268190,
            b1_0_2060_0 = 4268340,
        }

        public static GlobalValue GetGlobalValue()
        {
            return GlobalValue.b1_0_2060_0;
        }

        public static void UpdateVehPreview()
        {
            if (VehPreview == null)
            {
                return;
            }

            var mods = VehPreview.Mods;

            lastVehMemory = new Memory
            {
                Aerials = GetVehicleModIndex(VehPreview, 43),
                Trim = GetVehicleModIndex(VehPreview, 44),
                FrontBumper = GetVehicleModIndex(VehPreview, 1),
                RearBumper = GetVehicleModIndex(VehPreview, 2),
                SideSkirt = GetVehicleModIndex(VehPreview, 3),
                ColumnShifterLevers = GetVehicleModIndex(VehPreview, 34),
                Dashboard = GetVehicleModIndex(VehPreview, 29),
                DialDesign = GetVehicleModIndex(VehPreview, 30),
                Ornaments = GetVehicleModIndex(VehPreview, 28),
                Seats = GetVehicleModIndex(VehPreview, 32),
                SteeringWheels = GetVehicleModIndex(VehPreview, 33),
                TrimDesign = GetVehicleModIndex(VehPreview, 27),
                LightsColor = (int)mods.DashboardColor,
                TrimColor = mods.TrimColor,
                WheelType = mods.WheelType,
                AirFilter = GetVehicleModIndex(VehPreview, 40),
                EngineBlock = GetVehicleModIndex(VehPreview, 39),
                Struts = GetVehicleModIndex(VehPreview, 41),
                NumberPlate = (LicensePlateStyle)(int)mods.LicensePlateStyle,
                PlateHolder = GetVehicleModIndex(VehPreview, 25),
                VanityPlates = GetVehicleModIndex(VehPreview, 26),
                Armor = GetVehicleModIndex(VehPreview, 16),
                Brakes = GetVehicleModIndex(VehPreview, 12),
                Engine = GetVehicleModIndex(VehPreview, 11),
                Transmission = GetVehicleModIndex(VehPreview, 13),
                BackNeon = IsVehicleNeonLightOn(VehPreview, VehicleNeonLight.Back),
                FrontNeon = IsVehicleNeonLightOn(VehPreview, VehicleNeonLight.Front),
                LeftNeon = IsVehicleNeonLightOn(VehPreview, VehicleNeonLight.Left),
                RightNeon = IsVehicleNeonLightOn(VehPreview, VehicleNeonLight.Right),
                BackWheels = GetVehicleModIndex(VehPreview, 24),
                FrontWheels = GetVehicleModIndex(VehPreview, 23),
                Headlights = IsVehicleToggleModOn(VehPreview, 22),
                WheelsVariation = IsCustomWheels(),
                ArchCover = GetVehicleModIndex(VehPreview, 42),
                Exhaust = GetVehicleModIndex(VehPreview, 4),
                Fender = GetVehicleModIndex(VehPreview, 8),
                RightFender = GetVehicleModIndex(VehPreview, 9),
                DoorSpeakers = GetVehicleModIndex(VehPreview, 31),
                Frame = GetVehicleModIndex(VehPreview, 5),
                Grille = GetVehicleModIndex(VehPreview, 6),
                Hood = GetVehicleModIndex(VehPreview, 7),
                Horns = GetVehicleModIndex(VehPreview, 14),
                Hydraulics = GetVehicleModIndex(VehPreview, 38),
                Livery = GetVehicleModIndex(VehPreview, 48),
                Plaques = GetVehicleModIndex(VehPreview, 35),
                Roof = GetVehicleModIndex(VehPreview, 10),
                Speakers = GetVehicleModIndex(VehPreview, 36),
                Spoilers = GetVehicleModIndex(VehPreview, 0),
                Tank = GetVehicleModIndex(VehPreview, 45),
                Trunk = GetVehicleModIndex(VehPreview, 37),
                Turbo = IsVehicleToggleModOn(VehPreview, 18),
                Windows = GetVehicleModIndex(VehPreview, 46),
                Tint = mods.WindowTint,
                PearlescentColor = mods.PearlescentColor,
                PrimaryColor = mods.PrimaryColor,
                RimColor = mods.RimColor,
                SecondaryColor = mods.SecondaryColor,
                TireSmokeColor = mods.TireSmokeColor,
                NeonLightsColor = mods.NeonLightsColor,
                PlateNumbers = mods.LicensePlate,
                CustomPrimaryColor = mods.CustomPrimaryColor,
                CustomSecondaryColor = mods.CustomSecondaryColor,
                IsPrimaryColorCustom = mods.IsPrimaryColorCustom,
                IsSecondaryColorCustom = mods.IsSecondaryColorCustom,
                Suspension = GetVehicleModIndex(VehPreview, 15),
            };
        }

        public static void SuspendKeys()
        {
            Game.DisableControlThisFrame(Control.MoveUpDown);
            Game.DisableControlThisFrame(Control.MoveLeftRight);
            Game.DisableControlThisFrame(Control.MoveDown);
            Game.DisableControlThisFrame(Control.MoveDownOnly);
            Game.DisableControlThisFrame(Control.MoveLeft);
            Game.DisableControlThisFrame(Control.MoveLeftOnly);
            Game.DisableControlThisFrame(Control.MoveRight);
            Game.DisableControlThisFrame(Control.MoveRightOnly);
            Game.DisableControlThisFrame(Control.MoveUp);
            Game.DisableControlThisFrame(Control.MoveUpOnly);
            Game.DisableControlThisFrame(Control.Jump);
            Game.DisableControlThisFrame(Control.Cover);
            Game.DisableControlThisFrame(Control.Context);
            Game.DisableControlThisFrame(Control.VehicleAccelerate);
            Game.DisableControlThisFrame(Control.VehicleAim);
            Game.DisableControlThisFrame(Control.VehicleAttack);
            Game.DisableControlThisFrame(Control.VehicleAttack2);
            Game.DisableControlThisFrame(Control.VehicleBrake);
            Game.DisableControlThisFrame(Control.VehicleCinCam);
            Game.DisableControlThisFrame(Control.VehicleDuck);
            Game.DisableControlThisFrame(Control.VehicleExit);
            Game.DisableControlThisFrame(Control.VehicleHeadlight);
            Game.DisableControlThisFrame(Control.VehicleHorn);
            Game.DisableControlThisFrame(Control.VehicleMoveLeftOnly);
            Game.DisableControlThisFrame(Control.VehicleMoveRightOnly);
            Game.DisableControlThisFrame(Control.VehicleMoveLeft);
            Game.DisableControlThisFrame(Control.VehicleMoveRight);
            Game.DisableControlThisFrame(Control.VehicleSubTurnLeftRight);
            Game.DisableControlThisFrame(Control.VehicleSubTurnLeftOnly);
            Game.DisableControlThisFrame(Control.VehicleSubTurnRightOnly);
            Game.DisableControlThisFrame(Control.VehicleSubTurnHardLeft);
            Game.DisableControlThisFrame(Control.VehicleSubTurnHardRight);
            Game.DisableControlThisFrame(Control.VehicleMoveLeftRight);
            Game.DisableControlThisFrame(Control.VehicleLookLeft);
            Game.DisableControlThisFrame(Control.VehicleLookRight);
            Game.DisableControlThisFrame(Control.VehicleHotwireLeft);
            Game.DisableControlThisFrame(Control.VehicleHotwireRight);
            Game.DisableControlThisFrame(Control.VehicleGunLeftRight);
            Game.DisableControlThisFrame(Control.VehicleGunLeft);
            Game.DisableControlThisFrame(Control.VehicleGunRight);
            Game.DisableControlThisFrame(Control.VehicleCinematicLeftRight);
            Game.DisableControlThisFrame(Control.NextCamera);
            Game.DisableControlThisFrame(Control.VehicleRocketBoost);
            Game.DisableControlThisFrame(Control.VehicleJump);
            Game.DisableControlThisFrame(Control.VehicleCarJump);
            Game.DisableControlThisFrame(Control.Jump);
            Game.DisableControlThisFrame(keyCamera);
            Game.DisableControlThisFrame(keyDoor);
            Game.DisableControlThisFrame(keyRoof);
            Game.DisableControlThisFrame(keyZoom);
        }
    }
}
