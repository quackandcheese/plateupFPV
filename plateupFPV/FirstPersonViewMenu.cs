using Kitchen;
using Kitchen.Modules;
using KitchenLib;
using KitchenLib.Preferences;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KitchenFirstPersonView
{
    public class FirstPersonViewMenu<T> : KLMenu<T>
    {
        public FirstPersonViewMenu(Transform container, ModuleList moduleList) : base(container, moduleList)
        {

        }

        public override void Setup(int player_id)
        {
            AddLabel("Sensitivity");
            AddSelect<float>(SensitivityOption);
            SensitivityOption.OnChanged += delegate (object _, float result)
            {
                PreferenceFloat preferenceFloat = Mod.PrefManager.GetPreference<PreferenceFloat>(Mod.SENSITIVITY_ID);
                preferenceFloat.Set(result);
                Mod.PrefManager.Save();
            };

            //New<SpacerElement>();

            New<SpacerElement>(true);
            New<SpacerElement>(true);
            AddButton(base.Localisation["MENU_BACK_SETTINGS"], delegate (int i)
            {
                this.RequestPreviousMenu();
            }, 0, 1f, 0.2f);
        }
        
        private Option<float> SensitivityOption = new Option<float>(
            new List<float> { 1f, 1.5f, 2f, 2.5f, 3f, 3.5f, 4f, 4.5f, 5f, 5.5f, 6f, 6.5f, 7f, 7.5f, 8f, 8.5f, 9f }, 
            (float)Mod.PrefManager.Get<PreferenceFloat>(Mod.SENSITIVITY_ID), 
            new List<string> { "1", "1.5", "2", "2.5", "3", "3.5", "4", "4.5", "5", "5.5", "6", "6.5", "7", "7.5", "8", "8.5", "9" 
            });
    }
}
