namespace SpyTools
{
    using System;

    public class SpyEventArgs : System.EventArgs
    {
        private System.EventArgs args;
        private string evName;
        private SpyTools.EventSpy spy;

        public SpyEventArgs(SpyTools.EventSpy spy, string s, System.EventArgs e)
        {
            this.spy = spy;
            this.evName = s;
            this.args = e;
        }

        public System.EventArgs EventArgs
        {
            get
            {
                return this.args;
            }
        }

        public string EventName
        {
            get
            {
                return this.evName;
            }
        }

        public SpyTools.EventSpy EventSpy
        {
            get
            {
                return this.spy;
            }
        }
    }
}

