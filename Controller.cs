using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ANDREICSLIB.ClassExtras;
using ANDREICSLIB.Licensing;
using NLog;

namespace NugetPackageUpdater
{
    public static class Controller
    {
        public static AssemblyValues v;
        private const String HelpString = "";

        private static readonly String OtherText =
            @"©" + DateTime.Now.Year +
            @" Andrei Gec (http://www.andreigec.net)

Licensed under GNU LGPL (http://www.gnu.org/)

Zip Assets © SharpZipLib (http://www.sharpdevelop.net/OpenSource/SharpZipLib/)
";

        public static Dictionary<string, string> SeenPackageVersions = new Dictionary<string, string>();
        public static string[] NugetRepos = new string[] { };
        private static Logger l = LogManager.GetCurrentClassLogger();


        private static void KeyboardAndClose()
        {
            Console.Write("\nPress any key to exit application");
            Console.ReadKey(true);
            Environment.Exit(0);
        }


        public static async Task Run(string[] args)
        {
            v = AssemblyExtras.GetEntryAssemblyInfo();
            var n = v.GetAppString();
            Console.Title = n;

            bool error = false;

            //parse args
            var packagename = "";
            var filetype = "";
            var version = "";
            var startpath = "";

            try
            {
                for (int ai = 0; ai < args.Length; ai += 2)
                {
                    var index = args[ai];
                    var value = args[ai + 1];

                    if (index == "-n")
                        packagename = value;
                    else if (index == "-t")
                        filetype = "*." + value;
                    else if (index == "-v")
                        version = value;
                    else if (index == "-p")
                        startpath = value;
                }
            }
            catch (Exception e)
            {
                error = true;
            }

            if (string.IsNullOrEmpty(version))
                version = null;

            if (string.IsNullOrEmpty(filetype))
                filetype = "*.csproj";
            else
            {
                if (filetype.StartsWith("*.") == false)
                    filetype = "*." + filetype;
            }

            if (string.IsNullOrEmpty(packagename))
            {
                error = true;
            }

            if (string.IsNullOrEmpty(startpath))
                startpath = null;

            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string aversion = fvi.FileVersion;

            if (args.Length == 0 || error)
            {
                Console.WriteLine("\r\nNugetPackageUpdater V" + aversion + "\n");
                Console.WriteLine("Args: NugetPackageUpdater.exe");
                Console.WriteLine("-n PackageName (eg Antlr)");
                Console.WriteLine("[-t FileType (default: csproj)]");
                Console.WriteLine("[-v Version (default: newest from nuget)]");
                Console.WriteLine("[-p Start Path (default: current directory)]");
                if (await Licensing.IsUpdateConsoleRequired(HelpString, OtherText) == false)
                    KeyboardAndClose();
            }

            Console.WriteLine("\r\nNugetPackageUpdater V" + aversion + "\n");

            try
            {
                NugetRepos = NugetHelpers.GetLocalNugetRepos();
                ProjectFileChanges(filetype, packagename, version, startpath);
                PackagesChanges(packagename, version, startpath);
            }
            catch (Exception ex)
            {
                l.Error(ex, "Error occured:" + ex);
            }
        }

        private static void PackagesChanges(string packageName, string forceversion = null, string startDir = null)
        {
            if (startDir == null)
                startDir = Directory.GetCurrentDirectory();

            var files = FileHelpers.GetFilesRecursive(startDir, "packages.config").ToList();

            foreach (var f in files)
            {
                var filename = f;
                var content = FileHelpers.LoadFile(f);
                var r = new Regex($"(<package id=\"({packageName})\" version=\"(.*?)\".*?/>.*?)+",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                var nugets = r.Matches(content);
                var change = false;
                foreach (Match m in nugets)
                {
                    var oldblock = m.Groups[1].Value;
                    var package = m.Groups[2].Value;
                    var version = m.Groups[3].Value;

                    var newversion = forceversion ??
                                     NugetHelpers.GetNewestVersion(package, NugetRepos, SeenPackageVersions);
                    var newblock = oldblock.Replace(version, newversion);
                    content = content.Replace(oldblock, newblock);
                    if (newblock != oldblock)
                    {
                        l.Trace($"{filename}/{package} Old={version}, New={newversion}");
                        change = true;
                    }
                }

                if (change)
                {
                    FileHelpers.SaveToFile(f, content);
                }
            }
        }

        private static void ProjectFileChanges(string filefilter, string packageName, string forceversion = null,
            string startDir = null)
        {
            if (startDir == null)
                startDir = Directory.GetCurrentDirectory();

            var files = FileHelpers.GetFilesRecursive(startDir, filefilter).ToList();

            foreach (var f in files)
            {
                var filename = Path.GetFileName(f);

                var content = FileHelpers.LoadFile(f);
                var r = new Regex($"<Reference Include=\"({packageName}),.*?=(.*?),.*?</Reference>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                var nugets = r.Matches(content);
                var change = false;

                foreach (Match m in nugets)
                {
                    var oldblock = m.Groups[0].Value;
                    var package = m.Groups[1].Value;
                    var version = m.Groups[2].Value;

                    var newversion = forceversion ??
                                     NugetHelpers.GetNewestVersion(package, NugetRepos, SeenPackageVersions);
                    var newblock = oldblock.Replace(version, newversion);
                    content = content.Replace(oldblock, newblock);

                    if (newblock != oldblock)
                    {
                        l.Trace($"{filename}/{package} Old={version}, New={newversion}");
                        change = true;
                    }
                }

                if (change)
                    FileHelpers.SaveToFile(f, content);
            }
        }
    }
}
