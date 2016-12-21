using UnityEngine;
using Kopernicus;
using Kopernicus.Components;
using Kopernicus.MaterialWrapper;
using Kopernicus.Configuration;
using Kopernicus.OnDemand;
using System;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace SigmadVmapperPlugin
{
    public static class PlanetSprites
    {
        static CelestialBody body = null;
        static bool isStar = false;
        static int size = 60;
        static Texture2D MainTex = null;
        static double longitude = 250;

        public static void Generate()
        {
            foreach (CelestialBody cb in FlightGlobals.Bodies)
            {
                body = cb;
                NameChanger renamer = body.GetComponent<NameChanger>();
                string bodyName = renamer == null ? body.name : renamer.newName;
                GetTexture();
            }
        }

        static void GetTexture()
        {
            Renderer renderer = body.scaledBody.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null && renderer.material.shader != null && renderer.material.shader.name == "Emissive Multi Ramp Sunspots")
            {
                isStar = true;
                StarTexture(renderer.material);
                return;
            }
            else
            {
                isStar = false;
                MainTex = Utility.CreateReadable(body.scaledBody.GetComponent<Renderer>().material.GetTexture("_MainTex") as Texture2D);
            }
            if (MainTex != null)
            {
                double shift = ((longitude) % 360) / 360;
                if (shift < 0) shift += 1;
                shift = (shift * MainTex.width);
                if (shift > 0)
                {
                    Color[] block1 = MainTex.GetPixels(MainTex.width - (int)shift, 0, (int)shift, MainTex.height);
                    Color[] block2 = MainTex.GetPixels(0, 0, MainTex.width - (int)shift, MainTex.height);
                    MainTex.SetPixels(0, 0, (int)shift, MainTex.height, block1);
                    MainTex.SetPixels((int)shift, 0, MainTex.width - (int)shift, MainTex.height, block2);
                }
                Texture2D thumb = Crop(MainTex, size);
                Print(thumb, "square");
                GetShape(thumb);
            }
        }

        static void StarTexture(Material material)
        {
            EmissiveMultiRampSunspots star = new EmissiveMultiRampSunspots(material);
            Color color = Color.Lerp(star.emitColor0, star.emitColor1, 0.5f);

            Texture2D gradient = Utility.CreateReadable(Assets.orbits["starlerp"]);
            gradient.SetPixels(Assets.orbits["starlerp"].GetPixels());

            for (int x = 0; x < 60; x++)
            {
                for (int y = 0; y < 60; y++)
                {
                    gradient.SetPixel(x, y, Color.Lerp(Color.white, color, gradient.GetPixel(x, y).grayscale));
                }
            }

            Print(gradient, "star");
            GetShape(gradient);
        }

        static void GetShape(Texture2D thumb)
        {
            Texture2D newThumb = new Texture2D(size + 11, size + 11);
            Clear(newThumb);

            for (int x = 0; x < (size * 2); x++)
            {
                for (int y = 0; y < (size * 2); y++)
                {
                    double lat = y * 180d / (size * 2d) - 90d;
                    double lon = x * 180d / (size * 2d) - 90d + longitude;
                    Vector3 position = Utility.LLAtoECEF(lat, lon, 0.5, 0.5);
                    double altitude = 1;
                    if (body.pqsController != null)
                    {
                        altitude = body.pqsController.GetSurfaceHeight(position);
                        if (altitude < body.Radius)
                            altitude = body.Radius;
                        altitude /= body.pqsController.radiusMax;
                    }
                    position *= (float)altitude;

                    Vector3 plane = Utility.LLAtoECEF(0, longitude, 0.5, 0.5);
                    Vector3 projection = Vector3.ProjectOnPlane(position, plane);

                    double py = projection.y;
                    projection.y = 0;

                    double px = projection.magnitude;
                    if (lon < longitude)
                        px *= -1;

                    // From 0 to size
                    px = (px + 1) / 2 * size;
                    py = (py + 1) / 2 * size;

                    Color color = thumb.GetPixel(x / 2, y / 2);
                    color.a = 1;
                    newThumb.SetPixel((int)px + 6, (int)py + 6, color);
                }
            }
            UnityEngine.Object.DestroyImmediate(thumb);
            Print(newThumb, "shape");
            SetBorder(newThumb, 1);
        }

        static void SetBorder(Texture2D thumb, int border)
        {
            Color[] colors = thumb.GetPixels();
            Texture2D newThumb = new Texture2D(84, 84);
            Clear(newThumb);
            newThumb.SetPixels((int)((84.5 - thumb.width) / 2), (int)((84.5 - thumb.height) / 2), thumb.width, thumb.height, colors);

            for (int x = 0; x < newThumb.width; x++)
            {
                for (int y = 0; y < newThumb.height; y++)
                {
                    float alpha = newThumb.GetPixel(x, y).a;
                    if (alpha == 1f)
                    {
                        Color[] region = newThumb.GetPixels(x - border, y - border, 1 + border * 2, 1 + border * 2);
                        for (int i = 0; i < region.Length; i++)
                        {
                            if (region[i].a == 0f)
                            {
                                Color color = newThumb.GetPixel(x, y);
                                color = Color.Lerp(Assets.background, color, 0.5f);
                                color.a = 1;
                                newThumb.SetPixel(x, y, color);
                            }
                        }
                    }
                }
            }
            UnityEngine.Object.DestroyImmediate(thumb);
            Print(newThumb, "border");
            GetSymbol(newThumb);
        }

        static void GetSymbol(Texture2D thumb)
        {
            if (isStar)
            {
                Texture2D circle = Assets.orbits["circle_star"];
                Texture2D newThumb = new Texture2D(circle.width, circle.height);
                Clear(newThumb);
                newThumb.SetPixels((newThumb.width - thumb.width) / 2, (newThumb.height - thumb.height) / 2, thumb.width, thumb.height, thumb.GetPixels());
                SetSymbol(newThumb, circle.GetPixels());
            }
            else if (body == FlightGlobals.GetHomeBody())
            {
                Texture2D circle = Assets.orbits["circle_home"];
                Texture2D newThumb = new Texture2D(circle.width, circle.height);
                Clear(newThumb);
                newThumb.SetPixels((newThumb.width - thumb.width) / 2, (newThumb.height - thumb.height) / 2, thumb.width, thumb.height, thumb.GetPixels());
                SetSymbol(newThumb, circle.GetPixels());
            }
            else
            {
                Color[] top = Assets.orbits["circle_top"].GetPixels();
                Color[] bottom = Assets.orbits["circle_bottom"].GetPixels();

                for (int i = 0; i < top.Length; i++)
                {
                    if (bottom[i].a > 0)
                    {
                        top[i] = bottom[i];
                    }
                }
                SetSymbol(thumb, top);
            }
        }

        static void SetSymbol(Texture2D thumb, Color[] circle)
        {
            Color[] pixels = thumb.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                if (circle[i].a > 0)
                {
                    pixels[i] = circle[i];
                }
            }
            thumb.SetPixels(pixels);
            Fill(thumb);
            Print(thumb, "symbol");
            Assets.planets.Add(body.name, thumb);
        }

        public static Texture2D Crop(Texture2D MainTex, int size)
        {
            Texture2D thumb = new Texture2D(size, size);
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    double LAT = (y * 1d / size) * MainTex.height;
                    double LON = (x * 1d / size) * (MainTex.width / 2d);

                    double LATrange = MainTex.height / size;
                    double LONrange = (MainTex.width / 2) / size;

                    Color[] colors = MainTex.GetPixels((int)LON, (int)LAT, (int)LONrange, (int)LATrange);
                    float[] AVGcolor = { 0, 0, 0 };
                    foreach (Color color in colors)
                    {
                        AVGcolor[0] += color.r;
                        AVGcolor[1] += color.g;
                        AVGcolor[2] += color.b;
                    }
                    AVGcolor[0] = AVGcolor[0] / colors.Length;
                    AVGcolor[1] = AVGcolor[1] / colors.Length;
                    AVGcolor[2] = AVGcolor[2] / colors.Length;
                    Color pixel = new Color(AVGcolor[0], AVGcolor[1], AVGcolor[2]);
                    thumb.SetPixel((size - 1) - x, y, pixel);
                }
            }
            return thumb;
        }

        public static void Clear(Texture2D thumb)
        {
            for (int x = 0; x < thumb.width; x++)
            {
                for (int y = 0; y < thumb.height; y++)
                {
                    thumb.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
        }

        public static void Fill(Texture2D thumb)
        {
            Fill(thumb, Assets.background);
        }

        public static void Fill(Texture2D thumb, Color color)
        {
            Color[] pixels = thumb.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.Lerp(color, pixels[i], pixels[i].a);
                pixels[i].a = 1;
            }
            thumb.SetPixels(pixels);
        }

        public static void Print(Texture2D thumb, string name)
        {
            Debug.Log("SigmaLog: Thumbnail completed");
            byte[] png = thumb.EncodeToPNG();
            File.WriteAllBytes(Application.dataPath + "/../GameData/Sigma/dVmapper/Assets/Sprites/" + body.transform.name + "_" + name + ".png", png);
            Debug.Log("SigmaLog: Thumbnail saved");
        }

        public static void HomeStarSymbol()
        {
            HomeStarSymbol(FlightGlobals.GetHomeBody().referenceBody);
        }

        static void HomeStarSymbol(CelestialBody body)
        {
            Renderer renderer = body.scaledBody.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null && renderer.material.shader != null && renderer.material.shader.name == "Emissive Multi Ramp Sunspots")
            {
                HomeStarSymbol(Assets.planets[body.name]);
            }
            else if (body.referenceBody != null && body.referenceBody != body)
            {
                HomeStarSymbol(body.referenceBody);
            }
        }

        static void HomeStarSymbol(Texture2D thumb)
        {
            Color[] pixels = thumb.GetPixels();
            Color[] home = Assets.orbits["circle_home"].GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                if (home[i].a > 0)
                {
                    pixels[i] = Color.Lerp(pixels[i], home[i], home[i].a);
                    pixels[i].a = 1;
                }
            }
            thumb.SetPixels(pixels);
            Print(thumb, "homestar");
        }
    }
}
