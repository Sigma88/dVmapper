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
    public static class PlanetSprites
    {
        static CelestialBody body = null;
        static int size = 60;
        static Texture2D MainTex = null;
        static double longitude = 150;

        public static void Generate()
        {
            foreach (CelestialBody cb in FlightGlobals.Bodies)
            {
                body = cb;
                GetTexture();
            }
        }

        static void GetTexture()
        {
            MainTex = Utility.CreateReadable(body.scaledBody.GetComponent<Renderer>().material.GetTexture("_MainTex") as Texture2D);
            if (MainTex != null)
            {
                double shift = ((longitude) % 360) / 360;
                if (shift < 0) shift += 1;
                shift = (shift * MainTex.width);
                Color[] block1 = MainTex.GetPixels(MainTex.width - (int)shift, 0, (int)shift, MainTex.height);
                Color[] block2 = MainTex.GetPixels(0, 0, MainTex.width - (int)shift, MainTex.height);
                MainTex.SetPixels(0, 0, (int)shift, MainTex.height, block1);
                MainTex.SetPixels((int)shift, 0, MainTex.width - (int)shift, MainTex.height, block2);

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
                GetShape(thumb);
            }
        }

        static void GetShape(Texture2D thumb)
        {
            Texture2D newThumb = new Texture2D(size + 5, size + 5);
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
                    newThumb.SetPixel((int)px + 2, newThumb.height - (int)py - 3, color);
                }
            }
            UnityEngine.Object.DestroyImmediate(thumb);
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
            AddCircle(newThumb);
        }

        static void AddCircle(Texture2D thumb)
        {
            Color[] top = Assets.orbits["circle_top"].GetPixels();
            Color[] bottom = Assets.orbits["circle_bottom"].GetPixels();

            Color[] sprite = thumb.GetPixels();

            for (int i = 0; i < top.Length; i++)
            {
                if (top[i].a > 0)
                {
                    sprite[i] = Color.Lerp(Assets.background, top[i], top[i].a);
                    sprite[i].a = 1;
                }
                else if (bottom[i].a > 0)
                {
                    sprite[i] = Color.Lerp(Assets.background, bottom[i], bottom[i].a);
                    sprite[i].a = 1;
                }
            }
            thumb.SetPixels(sprite);
            Fill(thumb);
            Assets.planets.Add(body.transform.name, thumb);
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

        static void Fill(Texture2D thumb)
        {
            Color[] pixels = thumb.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a == 0)
                {
                    pixels[i] = Assets.background;
                }
            }
            thumb.SetPixels(pixels);
        }

        static void Print(Texture2D thumb, string name)
        {
            Debug.Log("SigmaLog: Thumbnail completed");
            byte[] png = thumb.EncodeToPNG();
            File.WriteAllBytes(Application.dataPath + "/../GameData/Sigma/dVmapper/Assets/Sprites/" + body.transform.name + "_" + name + ".png", png);
            Debug.Log("SigmaLog: Thumbnail saved");
        }
    }
}
