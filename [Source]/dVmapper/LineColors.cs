using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Kopernicus;
using Kopernicus.Components;
using Kopernicus.MaterialWrapper;


namespace SigmadVmapperPlugin
{
    public static class LineColors
    {
        public static void Load()
        {
            PSystemBody sun = PSystemManager.Instance.systemPrefab.GetComponentsInChildren<PSystemBody>().First(b => b.name == "Sun");
            if (sun != null)
            {
                SetRecursively(sun);
            }
        }

        static void SetRecursively(PSystemBody body)
        {
            Renderer renderer = body.scaledVersion.GetComponent<Renderer>();
            if (body.orbitRenderer != null && body.orbitRenderer.orbitColor != null)
            {
                SetFromOrbit(body);
            }
            else if (renderer != null && renderer.material != null && renderer.material.shader != null && renderer.material.shader.name == "Emissive Multi Ramp Sunspots")
            {
                EmissiveMultiRampSunspots star = new EmissiveMultiRampSunspots(renderer.material);
                Color color = Color.Lerp(star.emitColor0, star.emitColor1, 0.5f);
                color.a = 1;
                NameChanger renamer = body.celestialBody.GetComponent<NameChanger>();
                string name = renamer == null ? body.name : renamer.newName;
                Debug.Log("SigmaLog: t.name = " + body.celestialBody.transform.name + ", name = " + name);
                Assets.colors.Add(name, color);
            }
        }

        static void SetFromOrbit(PSystemBody body)
        { 
            if (body.orbitRenderer != null && body.orbitRenderer.orbitColor != null)
            {
                Color color = body.orbitRenderer.nodeColor;
                color.a = 1;
                Assets.background = color;
                Texture2D tex = new Texture2D(100, 100);
                PlanetSprites.Clear(tex);
                PlanetSprites.Fill(tex);
                PlanetSprites.Print(tex, body.name);
                for (int i = 0; i < body.children.Count; i++)
                {
                    ColorHSV moonHSV = new ColorHSV(color);
                    if (moonHSV.s < 0.5)
                        moonHSV.s *= 1.33f;
                    else
                        moonHSV.s *= 0.75f;
                    if (body.children.Count > 4)
                        moonHSV.v = (0.4f / (body.children.Count - 1)) * i + 0.18f;
                    else if (body.children.Count > 1)
                        moonHSV.v = (0.4f - 0.1f * body.children.Count) / (body.children.Count - 1) + (0.1f * i) + 0.18f;
                    else
                        moonHSV.v = moonHSV.v < 0.38 ? moonHSV.v * 1.33f : moonHSV.v * 0.75f;


                    Assets.background = moonHSV.ToColor();
                    PlanetSprites.Clear(tex);
                    PlanetSprites.Fill(tex);
                    PlanetSprites.Print(tex, body.name + "_" + body.children[i]);
                }
            }
        }
        static void PrintColor(Color color, string name)
        {
            Assets.background = color;
            Texture2D tex = new Texture2D(100, 100);
            PlanetSprites.Clear(tex);
            PlanetSprites.Fill(tex);
            PlanetSprites.Print(tex, name);
        }
    }
}
