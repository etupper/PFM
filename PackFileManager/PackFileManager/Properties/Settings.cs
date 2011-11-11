namespace PackFileManager.Properties
{
    using System;
    using System.CodeDom.Compiler;
    using System.Configuration;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;

    [CompilerGenerated, GeneratedCode("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "9.0.0.0")]
    public sealed class Settings : ApplicationSettingsBase
    {
        private static Settings defaultInstance = ((Settings) SettingsBase.Synchronized(new Settings()));

        public static Settings Default
        {
            get
            {
                return defaultInstance;
            }
        }

        [DebuggerNonUserCode, DefaultSettingValue("False"), UserScopedSetting]
        public bool UseFirstColumnAsRowHeader
        {
            get
            {
                return (bool) this["UseFirstColumnAsRowHeader"];
            }
            set
            {
                this["UseFirstColumnAsRowHeader"] = value;
            }
        }

        [DebuggerNonUserCode, DefaultSettingValue("10595000"), UserScopedSetting]
        public string TwcThreadId
        {
            get
            {
                return (string)this["TwcThreadId"];
            }
            set
            {
                this["TwcThreadId"] = value;
            }
        }

        [DebuggerNonUserCode, DefaultSettingValue("False"), UserScopedSetting]
        public bool UseOnlineDefinitions
        {
            get
            {
                return (bool) this["UseOnlineDefinitions"];
            }
            set
            {
                this["UseOnlineDefinitions"] = value;
            }
        }
    }
}

