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

        public static void Load()
        {
            // Load orbits sprites
            string[] names = new string[] { "circle_top", "circle_middle", "circle_bottom", "orbit_low", "orbit_synch", "orbit_elliptic", "orbit_flyby" };
            foreach (string name in names)
            {
                Texture2D sprite = Utility.CreateReadable(Resources.FindObjectsOfTypeAll<Texture>().First(t => t.name == ("Sigma/dVmapper/" + name)) as Texture2D);
                if (sprite != null)
                {
                    orbits.Add(name, sprite);
                }
            }

            // Load planets sprites
            Thumbnail();
        }


        static CelestialBody body = null;
        static int size = 60;

        static void Thumbnail()
        {
            Texture2D MainTex = Utility.CreateReadable(body.scaledBody.GetComponent<Renderer>().material.GetTexture("_MainTex") as Texture2D);
            if (MainTex != null)
            {
                Texture2D thumb = new Texture2D(size, size);
                for (int x = 0; x < size; x++)
                {
                    for (int y = 0; y < size; y++)
                    {
                        Color[] colors = MainTex.GetPixels((x * MainTex.width / (size * 2)) + (MainTex.width / 4), y * MainTex.height / (size), MainTex.width / (size * 2), MainTex.height / (size));
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
                Shape(thumb);
            }
        }

        static void Shape(Texture2D thumb)
        {
            Texture2D newThumb = new Texture2D(size + 5, size + 5);
            Clear(newThumb);

            for (int x = 0; x < (size * 2); x++)
            {
                for (int y = 0; y < (size * 2); y++)
                {
                    double lat = 90 - (y * 90d / size);
                    double lon = (x * 90d / size) - 90;
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
                    Vector3 plane = new Vector3(1, 0, 0);
                    Vector3 projection = Vector3.ProjectOnPlane(position, plane);
                    int px = (int)Math.Round(projection.z * size * 0.5);
                    int py = (int)Math.Round(projection.y * size * 0.5);
                    px += (size / 2);
                    py = (size / 2) - py;

                    Color color = thumb.GetPixel((int)Math.Round(x * 0.5), (int)Math.Round(y * 0.5));
                    color.a = 1;
                    newThumb.SetPixel(px + 2, py + 2, color);
                }
            }
            Border(newThumb, 1);
        }

        static void Border(Texture2D thumb, int border)
        {
            Texture2D newThumb = new Texture2D(thumb.width + 4 * border, thumb.height + 4 * border);
            Clear(newThumb);
            newThumb.SetPixels(2 * border, 2 * border, thumb.width, thumb.height, thumb.GetPixels());

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
                                color.a = 0.5f;
                                newThumb.SetPixel(x, y, color);
                            }
                        }
                    }
                }
            }

            Color[] modifications = newThumb.GetPixels(2 * border, 2 * border, thumb.width, thumb.height);
            thumb.SetPixels(modifications);

            Circle(thumb);
        }
        // input a 84x84 texture to Circle !!!!!!!!
        static void Circle(Texture2D thumb)
        {
            Color[] top = orbits["circle_top"].GetPixels();
            Color[] bottom =  orbits["circle_bottom"].GetPixels();
            Color[] sprite = thumb.GetPixels();
            for (int i = 0; i < top.Length; i++)
            {
                if (top[i].a > 0)
                {
                    sprite[i] = top[i];
                }
                else if (bottom[i].a > 0)
                {
                    sprite[i] = bottom[i];
                }
            }
            thumb.SetPixels(sprite);
            planets.Add(body.transform.name, thumb);
        }

        static void Clear(Texture2D thumb)
        {
            for (int x = 0; x < thumb.width; x++)
            {
                for (int y = 0; y < thumb.height; y++)
                {
                    thumb.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
        }
    }
}
