using SteamShelf;
using SteamShelf.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ModsPanel
{
    internal static class ControllerGlyphs
    {
        private static readonly FieldInfo PlayerInputField = typeof(InputManager).GetField("playerInput",
            BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        internal static Sprite ForAction(string actionName)
        {
            if (!Singleton<InputManager>.HasInstance()) return null;
            PlayerInput input = PlayerInputField?.GetValue(Singleton<InputManager>.Instance) as PlayerInput;
            InputAction action = input?.actions?.FindAction(actionName);
            if (action == null) return null;
            foreach (InputBinding binding in action.bindings)
            {
                string path = binding.effectivePath;
                if (!binding.isComposite && path?.IndexOf("<Gamepad>/", StringComparison.OrdinalIgnoreCase) >= 0)
                    return ForPath(path);
            }
            return null;
        }

        internal static Sprite ForPath(string path)
        {
            string control = path?.Split('/').LastOrDefault()?.ToLowerInvariant();
            if (path?.IndexOf("/dpad/", StringComparison.OrdinalIgnoreCase) >= 0)
                control = "dpad" + control;
            if (string.IsNullOrEmpty(control)) return null;
            string family = Family();
            string file = FileName(family, control);
            return file == null ? null : Load(family, file);
        }

        private static string Family()
        {
            string name = ((Gamepad.current?.displayName ?? string.Empty) + " " +
                (Gamepad.current?.description.product ?? string.Empty) + " " +
                (Gamepad.current?.layout ?? string.Empty)).ToLowerInvariant();
            if (name.Contains("steam deck") || name.Contains("steamdeck")) return "SteamDeck";
            if (name.Contains("dualshock") || name.Contains("dualsense") || name.Contains("playstation")) return "PlayStation";
            if (name.Contains("switch") || name.Contains("nintendo") || name.Contains("joy-con")) return "Switch";
            return "Xbox";
        }

        private static string FileName(string family, string control)
        {
            string key = control switch
            {
                "buttonsouth" => "south",
                "buttoneast" => "east",
                "buttonwest" => "west",
                "buttonnorth" => "north",
                "dpadleft" => "left",
                "dpadright" => "right",
                _ => null
            };
            if (key == null) return null;
            if (family == "PlayStation") return key switch
            {
                "south" => "playstation_button_color_cross.png",
                "east" => "playstation_button_color_circle.png",
                "west" => "playstation_button_color_square.png",
                "north" => "playstation_button_color_triangle.png",
                "left" => "playstation_dpad_left_outline.png",
                _ => "playstation_dpad_right_outline.png"
            };
            string prefix = family == "SteamDeck" ? "steamdeck" : family == "Switch" ? "switch" : "xbox";
            if (key == "left" || key == "right")
                return family == "Switch" ? $"switch_buttons_{key}_outline.png" : $"{prefix}_dpad_{key}_outline.png";
            string face = family == "Switch"
                ? key switch { "south" => "b", "east" => "a", "west" => "y", _ => "x" }
                : key switch { "south" => "a", "east" => "b", "west" => "x", _ => "y" };
            return family == "Xbox" ? $"xbox_button_color_{face}.png" : $"{prefix}_button_{face}.png";
        }

        private static Sprite Load(string family, string file)
        {
            string key = family + "/" + file;
            if (Cache.TryGetValue(key, out Sprite cached)) return cached;
            Assembly assembly = typeof(ControllerGlyphs).Assembly;
            string resource = assembly.GetManifestResourceNames().FirstOrDefault(name =>
                name.EndsWith($"Glyphs.{family}.{file}", StringComparison.OrdinalIgnoreCase));
            if (resource == null) return null;
            using Stream stream = assembly.GetManifestResourceStream(resource);
            if (stream == null) return null;
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, bytes, false)) return null;
            texture.name = key;
            texture.filterMode = FilterMode.Bilinear;
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }
    }
}
