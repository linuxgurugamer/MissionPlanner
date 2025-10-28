using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP.Localization;   // for Localizer.Format

[KSPAddon(KSPAddon.Startup.MainMenu, false)]
public  class TraitUtil:MonoBehaviour
{
    public class DetailedTraits
    {
        public string name;
        public string title;
        public string desc;
        public List<string> effects;
    }

    public static List<string> traits = new List<string>();
    public static List<DetailedTraits> detailedTraits = new List<DetailedTraits>();

    void Start()
    {
        traits = GetAllTraitNames();
        detailedTraits = GetAllTraitsDetailed();
    }

    /// <summary>Returns distinct internal trait names (e.g., "Pilot", "Engineer").</summary>
    public List<string> GetAllTraitNames()
    {
        var cfgs = GameDatabase.Instance.GetConfigs("EXPERIENCE_TRAIT");
        return cfgs
            .Select(c => c.config.GetValue("name"))
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct()
            .ToList();
    }

    /// <summary>Yields (name, title, desc, effects[]).</summary>
    //public static IEnumerable<(string name, string title, string desc, List<string> effects)> GetAllTraitsDetailed()
    public List<DetailedTraits> GetAllTraitsDetailed()
    {
        List < DetailedTraits > traits = new List<DetailedTraits>();
        foreach (var cfg in GameDatabase.Instance.GetConfigs("EXPERIENCE_TRAIT"))
        {
            DetailedTraits detailedTraits = new DetailedTraits();
            var node = cfg.config;
            detailedTraits.name = node.GetValue("name") ?? "(unnamed)";
            detailedTraits.title = node.GetValue("title") ?? name;
            detailedTraits.desc = node.GetValue("desc") ?? "";

            // Resolve #autoLOC tokens if present
            if (detailedTraits.title.StartsWith("#")) detailedTraits.title = Localizer.Format(detailedTraits.title);
            if (detailedTraits.desc.StartsWith("#")) detailedTraits.desc = Localizer.Format(detailedTraits.desc);

            var effects = node
                .GetNodes("EFFECT")
                .Select(n => n.GetValue("name"))
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
            traits.Add(detailedTraits);
        }
        return traits;
        
    }
}
