namespace Common
{
    using System;

    public class LocEntry
    {
        private string localised;
        private string tag;
        private bool tooltip;

        public LocEntry(string tag, string localised, bool tooltip)
        {
            this.tag = tag;
            this.localised = localised;
            this.tooltip = tooltip;
        }

        public string Localised
        {
            get
            {
                return this.localised;
            }
            set
            {
                this.localised = value;
            }
        }

        public string Tag
        {
            get
            {
                return this.tag;
            }
            set
            {
                this.tag = value;
            }
        }

        public bool Tooltip
        {
            get
            {
                return this.tooltip;
            }
            set
            {
                this.tooltip = value;
            }
        }
    }
}

