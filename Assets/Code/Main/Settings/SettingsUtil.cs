using System.Collections.Generic;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Main.Settings.Options.Views;
using Awaken.TG.MVC;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Settings {
    public class SettingsUtil {
        public static void SpawnViews(IModel settingsUI, ISetting setting, Transform host, List<IVSetting> listToFill) {
            if (!setting.IsVisible) {
                return;
            }
            
            foreach (var option in setting.Options) {
                listToFill.Add(SpawnView(settingsUI, option, host));
            }
        }

        public static IVSetting SpawnView(IModel settingsUI, PrefOption option, Transform host) {
            IVSetting view = (IVSetting) World.SpawnView(settingsUI, option.ViewType, false, true, host);
            view.Setup(option);
            return view;
        }

        public static Selectable EstablishNavigation(Selectable previous, Selectable next) {
            if (previous == null) {
                return next;
            } else if (next != null) {
                Navigation previousNavi = previous.navigation;
                Navigation nextNavi = next.navigation;

                previousNavi.mode = Navigation.Mode.Explicit;
                previousNavi.selectOnDown = next;
                previous.navigation = previousNavi;

                nextNavi.mode = Navigation.Mode.Explicit;
                nextNavi.selectOnUp = previous;
                next.navigation = nextNavi;

                return next;
            }

            return previous;
        }
    }
}