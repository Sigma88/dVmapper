using UnityEngine;
using Kopernicus;
using Kopernicus.Components;
using Kopernicus.Configuration;
using Kopernicus.OnDemand;
using System;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Collections.Generic;


namespace SigmadVmapperPlugin
{
    public static class Assets
    {
        public static Dictionary<string, Texture2D> planets = new Dictionary<string, Texture2D>();
        public static Dictionary<string, Texture2D> orbits = new Dictionary<string, Texture2D>();
        public static Dictionary<object, Color> colors = new Dictionary<object, Color>();
        public static Color background = new Color(1, 1, 1, 1);

        public static void Load()
        {
            // Load orbits sprites
            string[] names = new[] { "starlerp", "circle_top", "circle_middle", "circle_bottom", "circle_home", "circle_star", "orbit_low", "orbit_synch", "orbit_elliptic", "orbit_flyby" };

            foreach(Texture texture in Resources.FindObjectsOfTypeAll<Texture>())
            {
                string name = texture.name.Replace("Sigma/dVmapper/Assets/", "");
                if (names.Contains(name))
                {
                    Texture2D sprite = Utility.CreateReadable(texture as Texture2D);
                    orbits.Add(name, sprite);
                }
            }
            
            // Generate planets sprites
            PlanetSprites.Generate();
            PlanetSprites.HomeStarSymbol();

            // Load Colors
            LineColors.Load();
        }
    }
}
