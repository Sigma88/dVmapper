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
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class dVmapper : MonoBehaviour
    {
        void Start()
        {
            Assets.Load();
        }
    }
}
