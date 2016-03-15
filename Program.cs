using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ANDREICSLIB.Helpers;
using NLog;
using NuGet;

namespace NugetPackageUpdater
{
    internal class Program
    {
     

        private static void Main(string[] args)
        {
            AsyncHelpers.RunSync(() => Controller.Run(args));

        }
    }
}