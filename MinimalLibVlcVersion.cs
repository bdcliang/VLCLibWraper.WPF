using System;
using System.Collections.Generic;
using System.Text;

namespace LibVlcWraper.WPF
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class MinimalLibVlcVersion : Attribute
    {
        public MinimalLibVlcVersion(string minVersion)
        {
            MinimalVersion = minVersion;
        }

        public string MinimalVersion { get; private set; }
    }
}
