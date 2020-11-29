using System.Collections.Generic;
using System.Linq;
using JDef;
using Nez.Textures;

namespace MTD.Resources
{
    /// <summary>
    /// Resources are things like wood, stone, money, metal etc.
    /// Resources are intended to be very simple, there is no concept of data
    /// attached to an individual resource, unlike items.
    /// </summary>
    public class ResourceDef : Def
    {
        #region Static Methods

        public static IReadOnlyList<ResourceDef> All { get { return allDefs; } }
        private static Dictionary<string, ResourceDef> namedDefs;
        private static ResourceDef[] allDefs;

        public static void Load()
        {
            if (allDefs != null)
                return;

            allDefs = Main.Defs.GetAllOfType<ResourceDef>().ToArray();
            namedDefs = new Dictionary<string, ResourceDef>();
            foreach (var def in allDefs)
            {
                namedDefs.Add(def.DefName, def);
            }
        }

        public static ResourceDef Get(string defName)
        {
            if (defName == null)
                return null;
            return namedDefs.TryGetValue(defName, out var found) ? found : null;
        }

        #endregion

        public string Name;
        public Sprite Icon;

        public override void Validate()
        {
            base.Validate();

            if(!DefName.EndsWith("Resource"))
                ValidateWarn($"ResourceDef's should have a name that ends with 'Resource'. Suggested name: '{DefName}Resource'");
            if (string.IsNullOrWhiteSpace(Name))
                ValidateError("Null or blank resource Name");
            if (Icon == null)
                ValidateError("Icon is missing (null)");
        }
    }
}
