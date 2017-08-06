﻿using NLog;
using System;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace TombEditor
{
    // Just add properties to this class to add now configuration options.
    // They will be loaded and saved automatically.
    public class Configuration
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public float RenderingItem_NavigationSpeedMoveMouse { get; set; } = 200.0f;
        public float RenderingItem_NavigationSpeedTranslateMouse { get; set; } = 200.0f;
        public float RenderingItem_NavigationSpeedRotateMouse { get; set; } = 4.0f;

        public int Rendering3D_DrawRoomsMaxDepth { get; set; } = 6;
        public float Rendering3D_NavigationSpeedRotateKey { get; set; } = 0.17f;
        public float Rendering3D_NavigationSpeedZoomKey { get; set; } = 3000.0f;
        public float Rendering3D_NavigationSpeedMoveMouse { get; set; } = 0.22f;
        public float Rendering3D_NavigationSpeedTranslateMouse { get; set; } = 0.0044f;
        public float Rendering3D_NavigationSpeedRotateMouse { get; set; } = 2.2f;

        public float Map2D_NavigationSpeedScaleScroll { get; set; } = 0.001f;
        public float Map2D_NavigationSpeedScaleKey { get; set; } = 0.17f;
        public float Map2D_NavigationSpeedMoveKey { get; set; } = 107.0f;



        public static string GetDefaultPath()
        {
            return Path.GetDirectoryName(Application.ExecutablePath) + "/TombEditorConfiguration.xml";
        }

        public void Save(Stream stream)
        {
            new XmlSerializer(typeof(Configuration)).Serialize(stream, this);
        }

        public void Save(string path)
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                Save(stream);
        }

        public void Save()
        {
            Save(GetDefaultPath());
        }

        public void SaveTry()
        {
            try
            {
                Save();
            }
            catch (Exception exc)
            {
                logger.Info(exc, "Unable to save configuration to \"" + GetDefaultPath() + "\"");
            }
        }

        public static Configuration Load(Stream stream)
        {
            return (Configuration)(new XmlSerializer(typeof(Configuration)).Deserialize(stream));
        }

        public static Configuration Load(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                return Load(stream);
        }

        public static Configuration Load()
        {
            return Load(GetDefaultPath());
        }
        
        public static Configuration LoadOrUseDefault()
        {
            try
            {
                return Load();
            }
            catch (Exception exc)
            {
                logger.Info(exc, "Unable to load configuration from \"" + GetDefaultPath() + "\"");
                return new Configuration();
            }
        }
    }
}
