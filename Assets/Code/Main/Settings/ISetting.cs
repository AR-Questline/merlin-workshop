using System.Collections.Generic;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Settings {
    public interface ISetting : IModel {
        
        string SettingName { get; }
        
        /// <summary>
        /// Enumerates all options that given setting holds
        /// </summary>
        IEnumerable<PrefOption> Options { get; }
        
        /// <summary>
        /// Indicates if setting spawns views based on <see cref="Options"/>
        /// </summary>
        bool IsVisible { get; }
        
        /// <summary>
        /// If any settings with this flag is changed, there will be popup informing about necessity of restart
        /// </summary>
        bool RequiresRestart { get; }
        
        /// <summary>
        /// Used to initialize all settings, they need to be forced to change
        /// </summary>
        void InitialApply();

        /// <summary>
        /// Seal all changes made to options, Cancel won't do anything after that
        /// </summary>
        void Apply(out bool needRestart);
        
        /// <summary>
        /// Cancel all changes made to options
        /// </summary>
        void Cancel();
        
        /// <summary>
        /// Restore defaults of all options, but don't apply them yet
        /// </summary>
        void RestoreDefault();

        /// <summary>
        /// Settings that require restarting should apply on this callback, it is called when scene changes
        /// </summary>
        void PerformOnSceneChange();
    }
}