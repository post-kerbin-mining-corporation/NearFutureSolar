using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;

namespace NearFutureSolar
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class NearFutureSolarFilters : MonoBehaviour
    {

        // Adds a proper filter icon
        // Thanks to stupid_chris for the tips here.
        private void SetupFilters()
        {
            // Load the icon
            Texture2D normal = new Texture2D(32, 32);
            Texture2D selected = new Texture2D(32, 32); 

            normal.LoadImage(File.ReadAllBytes(Utils.iconPath + "curvedSolarFilter.png"));
            selected.LoadImage(File.ReadAllBytes(Utils.iconPath + "curvedSolarFilter_selected.png"));

            PartCategorizer.Icon icon = new PartCategorizer.Icon("CurvedSolarPanel", normal, selected);

            //Set the button
            List<PartCategorizer.Category> modules = PartCategorizer.Instance.filters.Find(f => f.button.categoryName == "Filter by Module").subcategories;
            modules.Select(m => m.button).Single(b => b.categoryName == "Curved  Solar  Panel").SetIcon(icon);;
        }
     
        private void Awake()
        {
            GameEvents.onGUIEditorToolbarReady.Add(SetupFilters);
        }
        
        

    }
}
