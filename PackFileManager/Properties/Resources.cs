namespace PackFileManager.Properties
{
    using Common;
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.Globalization;
    using System.Resources;
    using System.Runtime.CompilerServices;

    [DebuggerNonUserCode, CompilerGenerated, GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "2.0.0.0")]
    internal class Resources
    {
        private static CultureInfo resourceCulture;
        private static System.Resources.ResourceManager resourceMan;

        internal Resources()
        {
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static CultureInfo Culture
        {
            get
            {
                return resourceCulture;
            }
            set
            {
                resourceCulture = value;
            }
        }

        internal static Icon PackFileManager
        {
            get
            {
                return (Icon) ResourceManager.GetObject("PackFileManager", resourceCulture);
            }
        }
        internal static System.Drawing.Icon Empire {
            get {
                object obj = ResourceManager.GetObject("Empire", resourceCulture);
                return ((System.Drawing.Icon)(obj));
            }
        }
        internal static System.Drawing.Icon Napoleon {
            get {
                object obj = ResourceManager.GetObject("Napoleon", resourceCulture);
                return ((System.Drawing.Icon)(obj));
            }
        }

        internal static System.Drawing.Icon Shogun {
            get {
                object obj = ResourceManager.GetObject("Shogun", resourceCulture);
                return ((System.Drawing.Icon)(obj));
            }
        }

        internal static System.Drawing.Icon Rome2 {
            get {
                object obj = ResourceManager.GetObject("Rome2", resourceCulture);
                return ((System.Drawing.Icon)(obj));
            }
        }

        internal static System.Drawing.Icon Attila
        {
            get
            {
                object obj = ResourceManager.GetObject("Attila", resourceCulture);
                return ((System.Drawing.Icon)(obj));
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static System.Resources.ResourceManager ResourceManager
        {
            get
            {
                if (object.ReferenceEquals(resourceMan, null))
                {
                    System.Resources.ResourceManager manager = new System.Resources.ResourceManager("PackFileManager.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = manager;
                }
                return resourceMan;
            }
        }

        internal static System.Drawing.Icon GetGameIcon(Game game) {
            if (game == Game.ATW)
            {
                return Attila;
            }
            else if (game == Game.ETW)
            {
                return Empire;
            }
            else if (game == Game.NTW)
            {
                return Napoleon;
            }
            else if (game == Game.R2TW)
            {
                return Rome2;
            }
            else if (game == Game.STW)
            {
                return Shogun;
            }
            return null;
        }
    }
}

