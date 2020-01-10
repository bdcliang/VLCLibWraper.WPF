using System;
using System.Collections.Generic;

using System.Text;

namespace LibVlcWraper.WPF
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class MaxLibVlcVersion : Attribute
    {
        public MaxLibVlcVersion(string maxVersion)
        {
            MaxVersion = maxVersion;
        }

        public string MaxVersion { get; private set; }
    }
}
