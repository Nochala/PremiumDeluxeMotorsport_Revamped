using GTA;
using GTA.Native;
using GTA.UI;
using LemonUI;
using LemonUI.Elements;
using LemonUI.Menus;
using LemonUI.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;

namespace PremiumDeluxeRevamped
{
    public class PDMeX : Script
    {
        private static readonly object BannerLock = new object();
        private static CustomSprite bannerSprite;
        private static DateTime bannerRetryAfterUtc = DateTime.MinValue;
        private static string bannerImagePath;

        public PDMeX()
        {
            Tick += PDMeX_Tick;
        }

        private const string BannerFileName = "shopui_title_pdm.png";

        private static string ResolveBannerImagePath()
        {
            string assemblyDirectory = Path.GetDirectoryName(typeof(PDMeX).Assembly.Location) ?? string.Empty;
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            string[] candidates =
            {
                Path.Combine(assemblyDirectory, BannerFileName),
                Path.Combine(assemblyDirectory, BannerFileName),
                Path.Combine(assemblyDirectory, "PremiumDeluxeMotorsport", "Banner", BannerFileName),
                Path.Combine(assemblyDirectory, "PremiumDeluxeMotorsport", "Banner", BannerFileName),
                Path.Combine(baseDirectory, "scripts", "PremiumDeluxeMotorsport", "Banner", BannerFileName),
                Path.Combine(baseDirectory, "scripts", "PremiumDeluxeMotorsport", "Banner", BannerFileName),
                Path.Combine(baseDirectory, "scripts", BannerFileName),
                Path.Combine(baseDirectory, "scripts", BannerFileName),
                Path.Combine(baseDirectory, BannerFileName),
                Path.Combine(baseDirectory, BannerFileName),
                Path.Combine(".", "scripts", "PremiumDeluxeMotorsport", "Banner", BannerFileName),
                Path.Combine(".", "scripts", "PremiumDeluxeMotorsport", "Banner", BannerFileName),
                Path.Combine(".", "scripts", BannerFileName),
                Path.Combine(".", "scripts", BannerFileName),
                Path.Combine(".", "PremiumDeluxeMotorsport", "Banner", BannerFileName),
                Path.Combine(".", "PremiumDeluxeMotorsport", "Banner", BannerFileName),
                Path.Combine(".", BannerFileName),
                Path.Combine(".", BannerFileName),
            };

            foreach (string candidate in candidates)
            {
                if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
                {
                    return candidate;
                }
            }

            Assembly ownAssembly = typeof(PDMeX).Assembly;
            string extractedPath = TryExtractBannerFromAssembly(ownAssembly);
            if (!string.IsNullOrWhiteSpace(extractedPath) && File.Exists(extractedPath))
            {
                return extractedPath;
            }

            extractedPath = TryExtractBannerFromAssemblyBinary(ownAssembly);
            if (!string.IsNullOrWhiteSpace(extractedPath) && File.Exists(extractedPath))
            {
                return extractedPath;
            }

            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            if (executingAssembly != null && executingAssembly != ownAssembly)
            {
                extractedPath = TryExtractBannerFromAssembly(executingAssembly);
                if (!string.IsNullOrWhiteSpace(extractedPath) && File.Exists(extractedPath))
                {
                    return extractedPath;
                }

                extractedPath = TryExtractBannerFromAssemblyBinary(executingAssembly);
                if (!string.IsNullOrWhiteSpace(extractedPath) && File.Exists(extractedPath))
                {
                    return extractedPath;
                }
            }

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly == null || assembly == ownAssembly || assembly == executingAssembly)
                {
                    continue;
                }

                extractedPath = TryExtractBannerFromAssembly(assembly);
                if (!string.IsNullOrWhiteSpace(extractedPath) && File.Exists(extractedPath))
                {
                    return extractedPath;
                }

                extractedPath = TryExtractBannerFromAssemblyBinary(assembly);
                if (!string.IsNullOrWhiteSpace(extractedPath) && File.Exists(extractedPath))
                {
                    return extractedPath;
                }
            }

            return null;
        }

        private static string GetPreferredBannerOutputDirectory()
        {
            string assemblyDirectory = Path.GetDirectoryName(typeof(PDMeX).Assembly.Location) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(assemblyDirectory) && Directory.Exists(assemblyDirectory))
            {
                return assemblyDirectory;
            }

            string scriptsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "PremiumDeluxeMotorsport", "Banner");
            Directory.CreateDirectory(scriptsDirectory);
            return scriptsDirectory;
        }

        private static string CreateExtractedBannerPath(string key)
        {
            string outputDirectory = GetPreferredBannerOutputDirectory();
            string fileName = "shopui_title_pdm.runtime.png";
            if (!string.IsNullOrWhiteSpace(key))
            {
                unchecked
                {
                    int hash = 17;
                    foreach (char c in key)
                    {
                        hash = (hash * 31) + c;
                    }

                    int positiveHash = hash & 0x7FFFFFFF;
                    fileName = $"shopui_title_pdm_{positiveHash:X8}.png";
                }
            }

            return Path.Combine(outputDirectory, fileName);
        }

        private static bool IsPngHeader(byte[] bytes)
        {
            return bytes != null
                && bytes.Length >= 8
                && bytes[0] == 0x89
                && bytes[1] == 0x50
                && bytes[2] == 0x4E
                && bytes[3] == 0x47
                && bytes[4] == 0x0D
                && bytes[5] == 0x0A
                && bytes[6] == 0x1A
                && bytes[7] == 0x0A;
        }

        private static byte[] ReadAllBytes(Stream stream)
        {
            if (stream == null)
            {
                return null;
            }

            using (MemoryStream ms = new MemoryStream())
            {
                if (stream.CanSeek)
                {
                    stream.Position = 0;
                }

                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        private static string SaveBannerStream(Stream stream, string key)
        {
            if (stream == null)
            {
                return null;
            }

            string outputPath = CreateExtractedBannerPath(key);
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            using (FileStream fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                stream.CopyTo(fileStream);
            }

            return outputPath;
        }

        private static string SaveBannerBytes(byte[] bytes, string key)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            string outputPath = CreateExtractedBannerPath(key);
            File.WriteAllBytes(outputPath, bytes);
            return outputPath;
        }

        private const int BannerWidth = 390;
        private const int BannerHeight = 116;
        private const float BannerMenuOffsetX = -0.35f;
        private const float BannerMenuOffsetY = -0.35f;
        private const float BannerMenuScale = 1.057f;
        private const float SectionLabelBannerOffsetX = 12f;
        private const float SectionLabelBannerOffsetY = 1.5f;
        private const float LemonUiToShvdnScaledRatio = 720f / 1080f;
        private const float VehicleStatsPanelOffsetX = 0f;
        private const float VehicleStatsPanelOffsetY = -10f;
        private const float VehicleStatsPanelWidth = 288f;
        private const float VehicleStatsPanelHeight = 100f;
        private const float VehicleStatsPanelPaddingLeft = 14f;
        private const float VehicleStatsPanelPaddingTop = 10f;
        private const float VehicleStatsRowSpacing = 21f;
        private const float VehicleStatsBarOffsetX = 118f;
        private const float VehicleStatsBarYOffset = 8f;
        private const float VehicleStatsBarWidth = 150f;
        private const float VehicleStatsBarHeight = 7f;
        private const int VehicleStatsBarSegments = 5;
        private const float VehicleStatsBarSegmentGap = 4f;
        private const float VehicleStatsBarMaxValue = 200f;
        private const float MenuAreaFallbackWidth = 431f;
        private const float MenuAreaFallbackHeight = 550f;
        private const float MenuAreaEstimatedBaseHeight = 170f;
        private const float MenuAreaEstimatedRowHeight = 38f;
        private const int MenuAreaEstimatedMaxVisibleRows = 10;
        private const float MenuAreaEstimatedFooterBaseHeight = 46f;
        private const float MenuAreaEstimatedFooterLineHeight = 18f;
        private const int MenuAreaEstimatedFooterWrapCharacters = 38;
        private static string SaveBannerBitmap(Bitmap bitmap, string key)
        {
            if (bitmap == null)
            {
                return null;
            }

            string outputPath = CreateExtractedBannerPath(key);

            using (Bitmap normalizedBitmap = new Bitmap(BannerWidth, BannerHeight, PixelFormat.Format32bppArgb))
            {
                using (Graphics graphics = Graphics.FromImage(normalizedBitmap))
                {
                    graphics.Clear(Color.Transparent);
                    graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                    float scaleX = (float)BannerWidth / bitmap.Width;
                    float scaleY = (float)BannerHeight / bitmap.Height;
                    float scale = Math.Min(scaleX, scaleY);

                    int drawWidth = Math.Max(1, (int)Math.Round(bitmap.Width * scale));
                    int drawHeight = Math.Max(1, (int)Math.Round(bitmap.Height * scale));
                    int offsetX = (BannerWidth - drawWidth) / 2;
                    int offsetY = (BannerHeight - drawHeight) / 2;

                    graphics.DrawImage(bitmap, new Rectangle(offsetX, offsetY, drawWidth, drawHeight));
                }

                normalizedBitmap.Save(outputPath, ImageFormat.Png);
            }

            return outputPath;
        }

        private static string TryExtractBannerResourceValue(object value, string key)
        {
            try
            {
                if (value is byte[] bytes)
                {
                    return SaveBannerBytes(bytes, key);
                }

                if (value is UnmanagedMemoryStream unmanagedMemoryStream)
                {
                    return SaveBannerStream(unmanagedMemoryStream, key);
                }

                if (value is MemoryStream memoryStream)
                {
                    memoryStream.Position = 0;
                    return SaveBannerStream(memoryStream, key);
                }

                if (value is Stream stream)
                {
                    return SaveBannerStream(stream, key);
                }

                if (value is Bitmap bitmap)
                {
                    return SaveBannerBitmap(bitmap, key);
                }

                if (value is Image image)
                {
                    using (Bitmap bitmapImage = new Bitmap(image))
                    {
                        return SaveBannerBitmap(bitmapImage, key);
                    }
                }

                if (value is string textValue && !string.IsNullOrWhiteSpace(textValue) && File.Exists(textValue))
                {
                    return NormalizeBannerFile(textValue, key);
                }
            }
            catch
            {
            }

            return null;
        }

        private static string TryExtractBannerFromResourceManager(Assembly assembly, Type resourcesType)
        {
            try
            {
                PropertyInfo resourceManagerProperty = resourcesType.GetProperty("ResourceManager", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                ResourceManager resourceManager = resourceManagerProperty?.GetValue(null, null) as ResourceManager;
                if (resourceManager == null)
                {
                    return null;
                }

                string assemblyName = assembly.GetName().Name;
                string[] keys =
                {
                    "shopui_title_pdm",
                    "shopui_title_pdm.png",
                    assemblyName + ".shopui_title_pdm",
                    assemblyName + ".shopui_title_pdm.png",
                };

                foreach (string key in keys)
                {
                    object resourceObject = null;
                    try
                    {
                        resourceObject = resourceManager.GetObject(key, CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                    }

                    string outputPath = TryExtractBannerResourceValue(resourceObject, assemblyName + "_" + key);
                    if (!string.IsNullOrWhiteSpace(outputPath) && File.Exists(outputPath))
                    {
                        return outputPath;
                    }

                    try
                    {
                        using (UnmanagedMemoryStream stream = resourceManager.GetStream(key, CultureInfo.InvariantCulture))
                        {
                            outputPath = SaveBannerStream(stream, assemblyName + "_" + key + "_stream");
                        }
                    }
                    catch
                    {
                        outputPath = null;
                    }

                    if (!string.IsNullOrWhiteSpace(outputPath) && File.Exists(outputPath))
                    {
                        return outputPath;
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        private static string TryExtractBannerFromResourcesType(Assembly assembly)
        {
            try
            {
                Type resourcesType = assembly.GetType(assembly.GetName().Name + ".Properties.Resources", false, true);
                if (resourcesType == null)
                {
                    return null;
                }

                string resourceManagerPath = TryExtractBannerFromResourceManager(assembly, resourcesType);
                if (!string.IsNullOrWhiteSpace(resourceManagerPath) && File.Exists(resourceManagerPath))
                {
                    return resourceManagerPath;
                }

                foreach (PropertyInfo property in resourcesType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    if (property == null || property.GetIndexParameters().Length != 0)
                    {
                        continue;
                    }

                    if (property.Name.IndexOf("shopui_title_pdm", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    object value = property.GetValue(null, null);
                    string outputPath = TryExtractBannerResourceValue(value, assembly.GetName().Name + "_" + property.Name);
                    if (!string.IsNullOrWhiteSpace(outputPath) && File.Exists(outputPath))
                    {
                        return outputPath;
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        private static string TryExtractBannerFromAssembly(Assembly assembly)
        {
            if (assembly == null)
            {
                return null;
            }

            try
            {
                foreach (string resourceName in assembly.GetManifestResourceNames())
                {
                    if (resourceName.EndsWith(BannerFileName, StringComparison.OrdinalIgnoreCase) || resourceName.IndexOf("shopui_title_pdm", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
                        {
                            string outputPath = SaveBannerStream(resourceStream, assembly.GetName().Name + "_" + resourceName);
                            if (!string.IsNullOrWhiteSpace(outputPath) && File.Exists(outputPath))
                            {
                                return outputPath;
                            }
                        }
                    }

                    if (!resourceName.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (resourceStream == null)
                        {
                            continue;
                        }

                        using (ResourceReader reader = new ResourceReader(resourceStream))
                        {
                            IDictionaryEnumerator enumerator = reader.GetEnumerator();
                            while (enumerator.MoveNext())
                            {
                                string key = enumerator.Key?.ToString() ?? string.Empty;
                                if (key.IndexOf("shopui_title_pdm", StringComparison.OrdinalIgnoreCase) < 0)
                                {
                                    continue;
                                }

                                string outputPath = TryExtractBannerResourceValue(enumerator.Value, assembly.GetName().Name + "_" + key);
                                if (!string.IsNullOrWhiteSpace(outputPath) && File.Exists(outputPath))
                                {
                                    return outputPath;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return TryExtractBannerFromResourcesType(assembly);
        }

        private static string NormalizeBannerFile(string sourcePath, string key)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
                {
                    return null;
                }

                byte[] imageBytes = File.ReadAllBytes(sourcePath);
                using (MemoryStream memoryStream = new MemoryStream(imageBytes))
                using (Image image = Image.FromStream(memoryStream, false, true))
                using (Bitmap bitmap = new Bitmap(image))
                {
                    return SaveBannerBitmap(bitmap, key);
                }
            }
            catch
            {
                return null;
            }
        }

        private static string TryExtractAnyPngFromAssembly(Assembly assembly)
        {
            if (assembly == null)
            {
                return null;
            }

            try
            {
                foreach (string resourceName in assembly.GetManifestResourceNames())
                {
                    using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (resourceStream == null)
                        {
                            continue;
                        }

                        byte[] bytes = ReadAllBytes(resourceStream);
                        if (IsPngHeader(bytes))
                        {
                            string outputPath = SaveBannerBytes(bytes, assembly.GetName().Name + "_png_" + resourceName);
                            if (!string.IsNullOrWhiteSpace(outputPath) && File.Exists(outputPath))
                            {
                                return outputPath;
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            try
            {
                foreach (string resourceName in assembly.GetManifestResourceNames())
                {
                    if (!resourceName.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (resourceStream == null)
                        {
                            continue;
                        }

                        using (ResourceReader reader = new ResourceReader(resourceStream))
                        {
                            IDictionaryEnumerator enumerator = reader.GetEnumerator();
                            while (enumerator.MoveNext())
                            {
                                object value = enumerator.Value;
                                if (value is byte[] pngBytes && IsPngHeader(pngBytes))
                                {
                                    string outputPath = SaveBannerBytes(pngBytes, assembly.GetName().Name + "_png_key_" + enumerator.Key);
                                    if (!string.IsNullOrWhiteSpace(outputPath) && File.Exists(outputPath))
                                    {
                                        return outputPath;
                                    }
                                }
                                else if (value is Stream valueStream)
                                {
                                    byte[] pngBytesFromStream = ReadAllBytes(valueStream);
                                    if (IsPngHeader(pngBytesFromStream))
                                    {
                                        string outputPath = SaveBannerBytes(pngBytesFromStream, assembly.GetName().Name + "_png_stream_" + enumerator.Key);
                                        if (!string.IsNullOrWhiteSpace(outputPath) && File.Exists(outputPath))
                                        {
                                            return outputPath;
                                        }
                                    }
                                }
                                else if (value is Image image)
                                {
                                    using (Bitmap bitmap = new Bitmap(image))
                                    {
                                        string outputPath = SaveBannerBitmap(bitmap, assembly.GetName().Name + "_png_img_" + enumerator.Key);
                                        if (!string.IsNullOrWhiteSpace(outputPath) && File.Exists(outputPath))
                                        {
                                            return outputPath;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return null;
        }


        private static int ReadPngInt32(byte[] data, int index)
        {
            if (data == null || index < 0 || index + 4 > data.Length)
            {
                return 0;
            }

            return (data[index] << 24)
                | (data[index + 1] << 16)
                | (data[index + 2] << 8)
                | data[index + 3];
        }

        private static bool TryGetPngSize(byte[] pngBytes, out int width, out int height)
        {
            width = 0;
            height = 0;

            if (!IsPngHeader(pngBytes) || pngBytes.Length < 24)
            {
                return false;
            }

            width = ReadPngInt32(pngBytes, 16);
            height = ReadPngInt32(pngBytes, 20);
            return width > 0 && height > 0;
        }

        private static int FindPngEnd(byte[] data, int startIndex)
        {
            if (data == null || startIndex < 0 || startIndex >= data.Length)
            {
                return -1;
            }

            for (int i = startIndex + 8; i <= data.Length - 12; i++)
            {
                if (data[i + 4] == (byte)'I'
                    && data[i + 5] == (byte)'E'
                    && data[i + 6] == (byte)'N'
                    && data[i + 7] == (byte)'D'
                    && data[i + 8] == 0xAE
                    && data[i + 9] == 0x42
                    && data[i + 10] == 0x60
                    && data[i + 11] == 0x82)
                {
                    return i + 12;
                }
            }

            return -1;
        }

        private static string TryExtractBannerFromAssemblyBinary(Assembly assembly)
        {
            try
            {
                string assemblyPath = assembly?.Location;
                if (string.IsNullOrWhiteSpace(assemblyPath) || !File.Exists(assemblyPath))
                {
                    return null;
                }

                byte[] dllBytes = File.ReadAllBytes(assemblyPath);
                List<byte[]> candidates = new List<byte[]>();

                for (int i = 0; i <= dllBytes.Length - 8; i++)
                {
                    if (dllBytes[i] != 0x89
                        || dllBytes[i + 1] != 0x50
                        || dllBytes[i + 2] != 0x4E
                        || dllBytes[i + 3] != 0x47
                        || dllBytes[i + 4] != 0x0D
                        || dllBytes[i + 5] != 0x0A
                        || dllBytes[i + 6] != 0x1A
                        || dllBytes[i + 7] != 0x0A)
                    {
                        continue;
                    }

                    int endIndex = FindPngEnd(dllBytes, i);
                    if (endIndex <= i)
                    {
                        continue;
                    }

                    int length = endIndex - i;
                    byte[] pngBytes = new byte[length];
                    Buffer.BlockCopy(dllBytes, i, pngBytes, 0, length);

                    if (TryGetPngSize(pngBytes, out int width, out int height) && width >= 256 && width > height)
                    {
                        candidates.Add(pngBytes);
                    }

                    i = endIndex - 1;
                }

                byte[] bestCandidate = null;
                int bestScore = int.MinValue;
                for (int i = 0; i < candidates.Count; i++)
                {
                    byte[] candidate = candidates[i];
                    if (!TryGetPngSize(candidate, out int width, out int height))
                    {
                        continue;
                    }

                    int score = (width * 10) - Math.Abs((width * 100 / Math.Max(1, height)) - 400);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestCandidate = candidate;
                    }
                }

                if (bestCandidate == null)
                {
                    return null;
                }

                return SaveBannerBytes(bestCandidate, assembly.GetName().Name + "_binary_png_banner");
            }
            catch
            {
                return null;
            }
        }

        private static void EnsureBannerSprite()
        {
            if (bannerSprite != null && !string.IsNullOrWhiteSpace(bannerImagePath) && File.Exists(bannerImagePath))
            {
                return;
            }

            if (DateTime.UtcNow < bannerRetryAfterUtc)
            {
                return;
            }

            lock (BannerLock)
            {
                if (bannerSprite != null && !string.IsNullOrWhiteSpace(bannerImagePath) && File.Exists(bannerImagePath))
                {
                    return;
                }

                string resolvedBannerImagePath = ResolveBannerImagePath();
                if (string.IsNullOrWhiteSpace(resolvedBannerImagePath) || !File.Exists(resolvedBannerImagePath))
                {
                    resolvedBannerImagePath = TryExtractAnyPngFromAssembly(typeof(PDMeX).Assembly);
                }

                if (!string.IsNullOrWhiteSpace(resolvedBannerImagePath) && File.Exists(resolvedBannerImagePath))
                {
                    string normalizedBannerPath = NormalizeBannerFile(resolvedBannerImagePath, "runtime_banner_texture");
                    if (!string.IsNullOrWhiteSpace(normalizedBannerPath) && File.Exists(normalizedBannerPath))
                    {
                        resolvedBannerImagePath = normalizedBannerPath;
                    }
                }

                if (string.IsNullOrWhiteSpace(resolvedBannerImagePath) || !File.Exists(resolvedBannerImagePath))
                {
                    bannerRetryAfterUtc = DateTime.UtcNow.AddSeconds(2);
                    bannerSprite = null;
                    bannerImagePath = null;
                    return;
                }

                bannerImagePath = Path.GetFullPath(resolvedBannerImagePath);
                PointF bannerPos = new PointF(0f, 0f);
                bannerSprite = new CustomSprite(bannerImagePath, new SizeF(BannerWidth, BannerHeight), bannerPos, Color.White)
                {
                    Centered = false,
                    Enabled = true,
                };
                bannerRetryAfterUtc = DateTime.MinValue;
            }
        }

        private static object GetReflectedMemberValue(object instance, string memberName)
        {
            if (instance == null || string.IsNullOrWhiteSpace(memberName))
            {
                return null;
            }

            Type type = instance.GetType();

            try
            {
                PropertyInfo property = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    return property.GetValue(instance, null);
                }
            }
            catch
            {
            }

            try
            {
                FieldInfo field = type.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    return field.GetValue(instance);
                }
            }
            catch
            {
            }

            return null;
        }

        private static bool TryFindVisibleNativeMenu(object value, int depth, HashSet<int> visited, out object menu)
        {
            menu = null;

            if (value == null || depth > 4)
            {
                return false;
            }

            if (value is string || value.GetType().IsPrimitive || value is decimal)
            {
                return false;
            }

            int identity = RuntimeHelpers.GetHashCode(value);
            if (!visited.Add(identity))
            {
                return false;
            }

            Type type = value.GetType();
            if (string.Equals(type.FullName, "LemonUI.Menus.NativeMenu", StringComparison.Ordinal))
            {
                object visibleValue = GetReflectedMemberValue(value, "Visible");
                if (visibleValue is bool visible && visible)
                {
                    menu = value;
                    return true;
                }
            }

            if (value is IEnumerable enumerable)
            {
                foreach (object entry in enumerable)
                {
                    if (TryFindVisibleNativeMenu(entry, depth + 1, visited, out menu))
                    {
                        return true;
                    }
                }
            }

            string[] preferredMembers =
            {
                "Menus",
                "Items",
                "Objects",
                "Drawables",
                "DrawableItems",
                "_menus",
                "_items",
                "_objects",
            };

            foreach (string memberName in preferredMembers)
            {
                object child = GetReflectedMemberValue(value, memberName);
                if (child != null && TryFindVisibleNativeMenu(child, depth + 1, visited, out menu))
                {
                    return true;
                }
            }

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (property == null || property.GetIndexParameters().Length != 0)
                {
                    continue;
                }

                if (string.Equals(property.Name, "Parent", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Type propertyType = property.PropertyType;
                if (propertyType == typeof(string) || propertyType.IsPrimitive)
                {
                    continue;
                }

                if (!typeof(IEnumerable).IsAssignableFrom(propertyType) && !string.Equals(propertyType.FullName, "LemonUI.Menus.NativeMenu", StringComparison.Ordinal))
                {
                    continue;
                }

                object child;
                try
                {
                    child = property.GetValue(value, null);
                }
                catch
                {
                    continue;
                }

                if (child != null && TryFindVisibleNativeMenu(child, depth + 1, visited, out menu))
                {
                    return true;
                }
            }

            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (field == null)
                {
                    continue;
                }

                Type fieldType = field.FieldType;
                if (fieldType == typeof(string) || fieldType.IsPrimitive)
                {
                    continue;
                }

                if (!typeof(IEnumerable).IsAssignableFrom(fieldType) && !string.Equals(fieldType.FullName, "LemonUI.Menus.NativeMenu", StringComparison.Ordinal))
                {
                    continue;
                }

                object child;
                try
                {
                    child = field.GetValue(value);
                }
                catch
                {
                    continue;
                }

                if (child != null && TryFindVisibleNativeMenu(child, depth + 1, visited, out menu))
                {
                    return true;
                }
            }

            return false;
        }

        private static PointF ConvertLemonUiPointToShvdnScaled(PointF value)
        {
            return new PointF(
                value.X * LemonUiToShvdnScaledRatio,
                value.Y * LemonUiToShvdnScaledRatio
            );
        }

        private static SizeF ConvertLemonUiSizeToShvdnScaled(SizeF value)
        {
            return new SizeF(
                value.Width * LemonUiToShvdnScaledRatio,
                value.Height * LemonUiToShvdnScaledRatio
            );
        }

        private static PointF ConvertNormalizedToScaledDraw(float normalizedX, float normalizedY)
        {
            return new PointF(
               (float)GTA.UI.Screen.ScaledWidth * normalizedX,
                720f * normalizedY
            );
        }

        private static bool TryGetVisibleMenuBannerBounds(out PointF position, out SizeF size)
        {
            position = PointF.Empty;
            size = SizeF.Empty;

            if (!TryFindVisibleNativeMenu(MenuHelper._menuPool, 0, new HashSet<int>(), out object visibleMenu) || visibleMenu == null)
            {
                return false;
            }

            object banner = GetReflectedMemberValue(visibleMenu, "Banner");
            if (banner == null)
            {
                return false;
            }

            object positionValue = GetReflectedMemberValue(banner, "Position");
            object sizeValue = GetReflectedMemberValue(banner, "Size");

            if (positionValue is PointF bannerPosition && sizeValue is SizeF bannerSize && bannerSize.Width > 0f && bannerSize.Height > 0f)
            {
                position = ConvertLemonUiPointToShvdnScaled(bannerPosition);
                size = ConvertLemonUiSizeToShvdnScaled(bannerSize);
                return true;
            }

            return false;
        }

        private static float ConvertScaledDrawXToNormalized(float scaledX)
        {
            float scaledWidth = (float)GTA.UI.Screen.ScaledWidth;
            if (scaledWidth <= 0f)
            {
                return 0f;
            }

            return scaledX / scaledWidth;
        }

        private static float ConvertScaledDrawYToNormalized(float scaledY)
        {
            return scaledY / 720f;
        }

        private static bool TryConvertToFloat(object value, out float result)
        {
            switch (value)
            {
                case float floatValue:
                    result = floatValue;
                    return true;
                case double doubleValue:
                    result = (float)doubleValue;
                    return true;
                case decimal decimalValue:
                    result = (float)decimalValue;
                    return true;
                case int intValue:
                    result = intValue;
                    return true;
                case long longValue:
                    result = longValue;
                    return true;
                case short shortValue:
                    result = shortValue;
                    return true;
                case byte byteValue:
                    result = byteValue;
                    return true;
                default:
                    result = 0f;
                    return false;
            }
        }

        private static bool TryExtractPointF(object value, out PointF point)
        {
            if (value is PointF pointF)
            {
                point = pointF;
                return true;
            }

            if (value is Point pointInt)
            {
                point = new PointF(pointInt.X, pointInt.Y);
                return true;
            }

            float x;
            float y;
            if (TryConvertToFloat(GetReflectedMemberValue(value, "X"), out x)
                && TryConvertToFloat(GetReflectedMemberValue(value, "Y"), out y))
            {
                point = new PointF(x, y);
                return true;
            }

            point = PointF.Empty;
            return false;
        }

        private static bool TryExtractSizeF(object value, out SizeF size)
        {
            if (value is SizeF sizeF)
            {
                size = sizeF;
                return true;
            }

            if (value is Size sizeInt)
            {
                size = new SizeF(sizeInt.Width, sizeInt.Height);
                return true;
            }

            if (value is RectangleF rectangleF)
            {
                size = rectangleF.Size;
                return true;
            }

            if (value is Rectangle rectangle)
            {
                size = rectangle.Size;
                return true;
            }

            float width;
            float height;
            if (TryConvertToFloat(GetReflectedMemberValue(value, "Width"), out width)
                && TryConvertToFloat(GetReflectedMemberValue(value, "Height"), out height))
            {
                size = new SizeF(width, height);
                return true;
            }

            size = SizeF.Empty;
            return false;
        }

        private static bool TryGetEnumerableCount(object value, out int count)
        {
            count = 0;
            if (value == null || value is string)
            {
                return false;
            }

            if (value is ICollection collection)
            {
                count = collection.Count;
                return true;
            }

            if (value is IEnumerable enumerable)
            {
                foreach (object _ in enumerable)
                {
                    count++;
                    if (count >= 512)
                    {
                        break;
                    }
                }

                return true;
            }

            return false;
        }

        private static bool TryGetVisibleMenuObject(out object visibleMenu)
        {
            return TryFindVisibleNativeMenu(MenuHelper._menuPool, 0, new HashSet<int>(), out visibleMenu) && visibleMenu != null;
        }

        private static object GetVisibleMenuSelectedItemObject(object visibleMenu)
        {
            if (visibleMenu == null)
            {
                return null;
            }

            string[] directItemMembers =
            {
                "SelectedItem",
                "CurrentItem",
                "HoveredItem",
                "ActiveItem",
            };

            foreach (string memberName in directItemMembers)
            {
                object selectedItem = GetReflectedMemberValue(visibleMenu, memberName);
                if (selectedItem != null)
                {
                    return selectedItem;
                }
            }

            if (!TryConvertToFloat(GetReflectedMemberValue(visibleMenu, "SelectedIndex"), out float selectedIndexValue))
            {
                return null;
            }

            int selectedIndex = Math.Max(0, (int)Math.Round(selectedIndexValue, MidpointRounding.AwayFromZero));
            object items = GetReflectedMemberValue(visibleMenu, "Items");
            if (items is IList itemList)
            {
                return selectedIndex >= 0 && selectedIndex < itemList.Count ? itemList[selectedIndex] : null;
            }

            if (items is IEnumerable enumerable)
            {
                int index = 0;
                foreach (object item in enumerable)
                {
                    if (index == selectedIndex)
                    {
                        return item;
                    }

                    index++;
                }
            }

            return null;
        }

        private static string GetSelectedMenuItemDescriptionText(object visibleMenu)
        {
            object selectedItem = GetVisibleMenuSelectedItemObject(visibleMenu);
            if (selectedItem == null)
            {
                return string.Empty;
            }

            string[] descriptionMembers =
            {
                "Description",
                "AltDescription",
                "HelpText",
                "Text",
            };

            foreach (string memberName in descriptionMembers)
            {
                object value = GetReflectedMemberValue(selectedItem, memberName);
                if (value is string textValue && !string.IsNullOrWhiteSpace(textValue))
                {
                    return textValue;
                }
            }

            return string.Empty;
        }

        private static float EstimateVisibleMenuFooterHeightLemonUi(object visibleMenu)
        {
            if (visibleMenu == null)
            {
                return 0f;
            }

            string[] footerHeightMembers =
            {
                "DescriptionHeight",
                "FooterHeight",
                "HelpHeight",
                "InfoHeight",
            };

            foreach (string memberName in footerHeightMembers)
            {
                if (TryConvertToFloat(GetReflectedMemberValue(visibleMenu, memberName), out float reflectedFooterHeight) && reflectedFooterHeight > 0f)
                {
                    return reflectedFooterHeight;
                }
            }

            string descriptionText = GetSelectedMenuItemDescriptionText(visibleMenu);
            if (string.IsNullOrWhiteSpace(descriptionText))
            {
                return 0f;
            }

            string normalizedDescription = descriptionText.Replace("\r", string.Empty).Trim();
            if (normalizedDescription.Length == 0)
            {
                return 0f;
            }

            string[] explicitLines = normalizedDescription.Split(new[] { '\n' }, StringSplitOptions.None);
            int wrappedLineCount = 0;
            foreach (string explicitLine in explicitLines)
            {
                string line = explicitLine?.Trim() ?? string.Empty;
                int estimatedSegments = Math.Max(1, (int)Math.Ceiling(line.Length / (double)MenuAreaEstimatedFooterWrapCharacters));
                wrappedLineCount += estimatedSegments;
            }

            wrappedLineCount = Math.Max(1, wrappedLineCount);
            return MenuAreaEstimatedFooterBaseHeight + ((wrappedLineCount - 1) * MenuAreaEstimatedFooterLineHeight);
        }

        private static SizeF EstimateVisibleMenuSizeLemonUi(object visibleMenu)
        {
            SizeF reflectedSize;
            string[] sizeMembers =
            {
                "Size",
                "MenuSize",
                "VisibleSize",
                "DrawSize",
                "Bounds",
                "Rectangle",
            };

            foreach (string memberName in sizeMembers)
            {
                if (TryExtractSizeF(GetReflectedMemberValue(visibleMenu, memberName), out reflectedSize)
                    && reflectedSize.Width > 0f
                    && reflectedSize.Height > 0f)
                {
                    return reflectedSize;
                }
            }

            float reflectedWidth;
            float reflectedHeight;
            bool hasWidth = TryConvertToFloat(GetReflectedMemberValue(visibleMenu, "Width"), out reflectedWidth) && reflectedWidth > 0f;
            bool hasHeight = TryConvertToFloat(GetReflectedMemberValue(visibleMenu, "Height"), out reflectedHeight) && reflectedHeight > 0f;
            if (hasWidth && hasHeight)
            {
                return new SizeF(reflectedWidth, reflectedHeight);
            }

            int maxVisibleRows = MenuAreaEstimatedMaxVisibleRows;
            string[] maxVisibleRowMembers =
            {
                "MaxItemsOnScreen",
                "MaxVisibleItems",
                "MaximumVisibleItems",
                "MaxItems",
            };

            foreach (string memberName in maxVisibleRowMembers)
            {
                if (TryConvertToFloat(GetReflectedMemberValue(visibleMenu, memberName), out float reflectedMaxRows) && reflectedMaxRows > 0f)
                {
                    maxVisibleRows = Math.Max(1, (int)Math.Round(reflectedMaxRows, MidpointRounding.AwayFromZero));
                    break;
                }
            }

            int visibleRowCount = 0;
            string[] visibleItemCollectionMembers =
            {
                "VisibleItems",
                "DisplayedItems",
                "ItemsToDraw",
            };

            foreach (string memberName in visibleItemCollectionMembers)
            {
                if (TryGetEnumerableCount(GetReflectedMemberValue(visibleMenu, memberName), out visibleRowCount) && visibleRowCount > 0)
                {
                    break;
                }

                visibleRowCount = 0;
            }

            if (visibleRowCount <= 0)
            {
                TryGetEnumerableCount(GetReflectedMemberValue(visibleMenu, "Items"), out visibleRowCount);
            }

            visibleRowCount = Math.Max(1, Math.Min(Math.Max(visibleRowCount, 1), Math.Max(1, maxVisibleRows)));

            float width = hasWidth ? reflectedWidth : MenuAreaFallbackWidth;
            float height = hasHeight ? reflectedHeight : (MenuAreaEstimatedBaseHeight + (visibleRowCount * MenuAreaEstimatedRowHeight));
            height += EstimateVisibleMenuFooterHeightLemonUi(visibleMenu);
            return new SizeF(width, height);
        }

        private static bool TryGetVisibleMenuAreaDrawBounds(out PointF position, out SizeF size)
        {
            position = PointF.Empty;
            size = SizeF.Empty;

            if (!TryGetVisibleMenuObject(out object visibleMenu))
            {
                return false;
            }

            if (TryExtractPointF(GetReflectedMemberValue(visibleMenu, "Position"), out PointF menuPosition))
            {
                position = ConvertLemonUiPointToShvdnScaled(menuPosition);
            }
            else if (TryGetVisibleMenuBannerBounds(out PointF bannerPosition, out SizeF _))
            {
                position = bannerPosition;
            }
            else
            {
                return false;
            }

            SizeF menuSize = EstimateVisibleMenuSizeLemonUi(visibleMenu);
            if (menuSize.Width <= 0f || menuSize.Height <= 0f)
            {
                return false;
            }

            size = ConvertLemonUiSizeToShvdnScaled(menuSize);
            return true;
        }

        private static void GetMenuBannerDrawBounds(out PointF position, out SizeF size)
        {
            PointF bannerPosition;
            SizeF bannerSize;

            if (!TryGetVisibleMenuBannerBounds(out bannerPosition, out bannerSize))
            {
                float safeZoneMargin = GetSafeZoneMargin();
                bannerPosition = ConvertNormalizedToScaledDraw(safeZoneMargin, safeZoneMargin);
                bannerSize = new SizeF(BannerWidth, BannerHeight);
            }

            float scaledWidth = bannerSize.Width * BannerMenuScale;
            float scaledHeight = bannerSize.Height * BannerMenuScale;
            float centeredX = bannerPosition.X + ((bannerSize.Width - scaledWidth) * 0.5f);
            float centeredY = bannerPosition.Y + ((bannerSize.Height - scaledHeight) * 0.5f);

            position = new PointF(
                centeredX + BannerMenuOffsetX,
                centeredY + BannerMenuOffsetY
            );
            size = new SizeF(scaledWidth, scaledHeight);
        }

        private static void DrawMenuBanner()
        {
            try
            {
                EnsureBannerSprite();
                if (bannerSprite == null)
                {
                    return;
                }

                GetMenuBannerDrawBounds(out PointF bannerDrawPosition, out SizeF bannerDrawSize);
                bannerSprite.Position = bannerDrawPosition;
                bannerSprite.Size = bannerDrawSize;
                bannerSprite.ScaledDraw();
            }
            catch
            {
                bannerSprite = null;
                bannerImagePath = null;
                bannerRetryAfterUtc = DateTime.UtcNow.AddSeconds(2);
            }
        }

        private static float GetSafeZoneMargin()
        {
            try
            {
                float safeZoneSize = Function.Call<float>(Hash.GET_SAFE_ZONE_SIZE);
                if (safeZoneSize <= 0f || safeZoneSize > 1f)
                {
                    return 0f;
                }

                return (1f - safeZoneSize) * 0.5f;
            }
            catch
            {
                return 0f;
            }
        }

        private static void DrawTextNormalized(string value, float x, float y, float scale, GTA.UI.Font font, Color color, bool rightAligned = false)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            Function.Call(Hash.SET_TEXT_FONT, (int)font);
            Function.Call(Hash.SET_TEXT_SCALE, 1.0f, scale);
            Function.Call(Hash.SET_TEXT_COLOUR, color.R, color.G, color.B, color.A);
            Function.Call(Hash.SET_TEXT_DROPSHADOW, 0, 0, 0, 0, 255);
            Function.Call(Hash.SET_TEXT_EDGE, 1, 0, 0, 0, 205);
            Function.Call(Hash.SET_TEXT_OUTLINE);

            if (rightAligned)
            {
                Function.Call(Hash.SET_TEXT_JUSTIFICATION, 2);
                Function.Call(Hash.SET_TEXT_WRAP, 0.0f, x);
                Function.Call(Hash.SET_TEXT_RIGHT_JUSTIFY, true);
            }
            else
            {
                Function.Call(Hash.SET_TEXT_JUSTIFICATION, 1);
                Function.Call(Hash.SET_TEXT_WRAP, x, 1.0f);
                Function.Call(Hash.SET_TEXT_RIGHT_JUSTIFY, false);
            }

            Function.Call(Hash.BEGIN_TEXT_COMMAND_DISPLAY_TEXT, "STRING");
            Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, value);
            Function.Call(Hash.END_TEXT_COMMAND_DISPLAY_TEXT, x, y, 0);
        }

        private static void DrawCurrentSectionLabel()
        {
            string menuTitle = MenuHelper.GetVisibleMenuTitle();
            if (string.IsNullOrWhiteSpace(menuTitle))
            {
                return;
            }

            GetMenuBannerDrawBounds(out PointF bannerDrawPosition, out SizeF bannerDrawSize);

            float sectionX = ConvertScaledDrawXToNormalized(bannerDrawPosition.X + SectionLabelBannerOffsetX);
            float sectionY = ConvertScaledDrawYToNormalized(bannerDrawPosition.Y + bannerDrawSize.Height + SectionLabelBannerOffsetY);

            menuTitle = menuTitle.Trim().ToUpperInvariant();
            DrawTextNormalized(menuTitle, sectionX, sectionY, 0.35f, GTA.UI.Font.ChaletLondon, Color.White);
        }

        private static bool IsVehicleViewerOverlayActive()
        {
            if (Helper.VehPreview == null || !Helper.VehPreview.Exists() || Helper.TaskScriptStatus != 0)
            {
                return false;
            }

            if (!Helper.poly.IsInInterior(Helper.VehPreview.Position))
            {
                return false;
            }

            if (MenuHelper._menuPool == null || !MenuHelper._menuPool.AreAnyVisible)
            {
                return false;
            }

            NativeMenu visibleMenu = MenuHelper.GetVisibleMenu();
            if (visibleMenu == null)
            {
                return false;
            }

            return !object.ReferenceEquals(visibleMenu, MenuHelper.MainMenu);
        }

        private static bool ShouldDrawVehiclePriceLabel()
        {
            return Helper.VehiclePrice > 0 && IsVehicleViewerOverlayActive();
        }

        private static void DrawVehiclePriceLabel()
        {
            if (!ShouldDrawVehiclePriceLabel())
            {
                return;
            }

            float safeZoneMargin = GetSafeZoneMargin();
            float rightX = 0.999f - safeZoneMargin;
            float priceY = 0.060f + (safeZoneMargin * 0.30f);
            string priceText = "PRICE $" + Helper.VehiclePrice.ToString("N0");
            DrawTextNormalized(priceText, rightX, priceY, 0.47f, GTA.UI.Font.ChaletLondon, Color.LightGreen, true);
        }

        private static float ConvertScaledWidthToNormalized(float scaledWidth)
        {
            float scaledScreenWidth = (float)GTA.UI.Screen.ScaledWidth;
            if (scaledScreenWidth <= 0f)
            {
                return 0f;
            }

            return scaledWidth / scaledScreenWidth;
        }

        private static float ConvertScaledHeightToNormalized(float scaledHeight)
        {
            return scaledHeight / 720f;
        }

        private static float Clamp01(float value)
        {
            if (value <= 0f)
            {
                return 0f;
            }

            if (value >= 1f)
            {
                return 1f;
            }

            return value;
        }

        private static void DrawRectNormalized(float centerX, float centerY, float width, float height, Color color)
        {
            if (width <= 0f || height <= 0f)
            {
                return;
            }

            Function.Call(Hash.DRAW_RECT, centerX, centerY, width, height, color.R, color.G, color.B, color.A, false);
        }

        private static void GetMenuAreaDrawBounds(out PointF position, out SizeF size)
        {
            if (TryGetVisibleMenuAreaDrawBounds(out position, out size))
            {
                return;
            }

            size = ConvertLemonUiSizeToShvdnScaled(new SizeF(MenuAreaFallbackWidth, MenuAreaFallbackHeight));

            if (TryGetVisibleMenuBannerBounds(out PointF bannerPosition, out SizeF _))
            {
                position = bannerPosition;
                return;
            }

            try
            {
                position = ConvertLemonUiPointToShvdnScaled(SafeZone.GetSafePosition(new PointF(0f, 0f)));
            }
            catch
            {
                float safeZoneMargin = GetSafeZoneMargin();
                position = ConvertNormalizedToScaledDraw(safeZoneMargin, safeZoneMargin);
            }
        }

        private static bool ShouldDrawVehicleStatsPanel()
        {
            return IsVehicleViewerOverlayActive();
        }

        private static void DrawVehicleStatBarNativeStyle(float value, float barLeftScaled, float barTopScaled)
        {
            float clampedRatio = Clamp01(value / VehicleStatsBarMaxValue);
            float segmentWidthScaled = (VehicleStatsBarWidth - (VehicleStatsBarSegmentGap * (VehicleStatsBarSegments - 1))) / VehicleStatsBarSegments;
            float activeSegments = clampedRatio * VehicleStatsBarSegments;
            float barCenterY = ConvertScaledDrawYToNormalized(barTopScaled + (VehicleStatsBarHeight * 0.5f));
            float segmentHeightNormalized = ConvertScaledHeightToNormalized(VehicleStatsBarHeight);

            for (int i = 0; i < VehicleStatsBarSegments; i++)
            {
                float segmentLeftScaled = barLeftScaled + (i * (segmentWidthScaled + VehicleStatsBarSegmentGap));
                float segmentCenterX = ConvertScaledDrawXToNormalized(segmentLeftScaled + (segmentWidthScaled * 0.5f));
                float segmentWidthNormalized = ConvertScaledWidthToNormalized(segmentWidthScaled);
                Color baseColor = Color.FromArgb(115, 55, 55, 55);
                Color fillColor = Color.FromArgb(235, 255, 255, 255);

                DrawRectNormalized(segmentCenterX, barCenterY, segmentWidthNormalized, segmentHeightNormalized, baseColor);

                float segmentFill = Clamp01(activeSegments - i);
                if (segmentFill > 0f)
                {
                    float fillWidthScaled = segmentWidthScaled * segmentFill;
                    float fillCenterX = ConvertScaledDrawXToNormalized(segmentLeftScaled + (fillWidthScaled * 0.5f));
                    float fillWidthNormalized = ConvertScaledWidthToNormalized(fillWidthScaled);
                    DrawRectNormalized(fillCenterX, barCenterY, fillWidthNormalized, segmentHeightNormalized, fillColor);
                }
            }
        }

        private static void DrawVehicleStatRow(string label, float value, float rowTopScaled, float panelLeftScaled)
        {
            float labelX = ConvertScaledDrawXToNormalized(panelLeftScaled + VehicleStatsPanelPaddingLeft);
            float textY = ConvertScaledDrawYToNormalized(rowTopScaled);
            float barLeftScaled = panelLeftScaled + VehicleStatsBarOffsetX;
            float barTopScaled = rowTopScaled + VehicleStatsBarYOffset;

            DrawTextNormalized(label, labelX, textY, 0.285f, GTA.UI.Font.ChaletLondon, Color.WhiteSmoke);
            DrawVehicleStatBarNativeStyle(value, barLeftScaled, barTopScaled);
        }

        private static void DrawVehicleStatsPanel()
        {
            if (!ShouldDrawVehicleStatsPanel())
            {
                return;
            }

            GetMenuAreaDrawBounds(out PointF menuPosition, out SizeF menuSize);

            float panelLeft = menuPosition.X + VehicleStatsPanelOffsetX;
            float panelTop = menuPosition.Y + menuSize.Height + VehicleStatsPanelOffsetY;
            float panelWidth = VehicleStatsPanelWidth;
            float panelHeight = VehicleStatsPanelHeight;

            float panelCenterX = ConvertScaledDrawXToNormalized(panelLeft + (panelWidth * 0.5f));
            float panelCenterY = ConvertScaledDrawYToNormalized(panelTop + (panelHeight * 0.5f));
            DrawRectNormalized(
                panelCenterX,
                panelCenterY,
                ConvertScaledWidthToNormalized(panelWidth),
                ConvertScaledHeightToNormalized(panelHeight),
                Color.FromArgb(120, 0, 0, 0));

            float firstRowTop = panelTop + VehicleStatsPanelPaddingTop;
            DrawVehicleStatRow("Top Speed", Helper.GetVehTopSpeed(Helper.VehPreview), firstRowTop, panelLeft);
            DrawVehicleStatRow("Acceleration", Helper.GetVehAcceleration(Helper.VehPreview), firstRowTop + VehicleStatsRowSpacing, panelLeft);
            DrawVehicleStatRow("Braking", Helper.GetVehBraking(Helper.VehPreview), firstRowTop + (VehicleStatsRowSpacing * 2f), panelLeft);
            DrawVehicleStatRow("Traction", Helper.GetVehTraction(Helper.VehPreview), firstRowTop + (VehicleStatsRowSpacing * 3f), panelLeft);
        }

        private void PDMeX_Tick(object sender, EventArgs e)
        {
            MenuHelper.RefreshMouseBehaviors();
            MenuHelper.RefreshInstructionalButtons();
            MenuHelper._menuPool?.Process();
            MenuHelper.ProcessPendingVehicleChange();
            MenuHelper.RecoverHiddenMenuIfNeeded();

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
                bool mouseEnabledForMenu = Helper.optEnableMouse && (Helper.wsCamera == null || !Helper.wsCamera.IsDragging);
                if (mouseEnabledForMenu)
                {
                    LemonUI.Tools.GameScreen.ShowCursorThisFrame();
                }

                DrawCurrentSectionLabel();
                DrawVehiclePriceLabel();
                DrawVehicleStatsPanel();
                if (Helper.ShowVehicleName && !string.IsNullOrEmpty(Helper.VehicleName) && Helper.VehPreview != null && Helper.poly.IsInInterior(Helper.VehPreview.Position) && Helper.TaskScriptStatus == 0)
                {
                    float safeZoneMargin = GetSafeZoneMargin();
                    float rightX = 0.995f - safeZoneMargin;
                    float classY = 0.928f - safeZoneMargin;
                    float nameY = classY - 0.040f;

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

                    DrawTextNormalized(Helper.VehicleName, rightX, nameY, 0.64f, titleFont, Color.White, true);
                    DrawTextNormalized(Helper.VehPreview.GetClassDisplayName(), rightX, classY, 0.40f, GTA.UI.Font.ChaletLondon, Color.DodgerBlue, true);
                }
            }
        }
    }
}
