using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NuGet;

namespace NugetPackageUpdater
{
    class Program
    {
        public static Dictionary<string, string> SeenPackageVersions = new Dictionary<string, string>();
        public static string[] NugetRepos = new string[] { };

        private static void PackagesChanges(string packageName)
        {
            var files = FileHelpers.GetFilesRecursive(Directory.GetCurrentDirectory(), "packages.config").ToList();
            foreach (var f in files)
            {
                var content = FileHelpers.LoadFile(f);
                var r = new Regex($"(<package id=\"({packageName})\" version=\"(.*?)\".*?/>.*?)+", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                var nugets = r.Matches(content);
                var change = false;
                foreach (Match m in nugets)
                {
                    var oldblock = m.Groups[1].Value;
                    var package = m.Groups[2].Value;
                    var version = m.Groups[3].Value;

                    var newversion = NugetHelpers.GetNewestVersion(package, NugetRepos, SeenPackageVersions);
                    var newblock = oldblock.Replace(version, newversion);
                    content = content.Replace(oldblock, newblock);
                    change = true;
                }

                if (change)
                    FileHelpers.SaveToFile(f, content);
            }
        }

        private static void ProjectFileChanges(string filefilter, string packageName)
        {
            var files = FileHelpers.GetFilesRecursive(Directory.GetCurrentDirectory(), filefilter).ToList();

            foreach (var f in files)
            {
                var content = FileHelpers.LoadFile(f);
                var r = new Regex($"<Reference Include=\"({packageName}),.*?=(.*?),.*?</Reference>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                var nugets = r.Matches(content);
                var change = false;
                foreach (Match m in nugets)
                {
                    var oldblock = m.Groups[0].Value;
                    var package = m.Groups[1].Value;
                    var version = m.Groups[2].Value;

                    var newversion = NugetHelpers.GetNewestVersion(package, NugetRepos, SeenPackageVersions);
                    var newblock = oldblock.Replace(version, newversion);
                    content = content.Replace(oldblock, newblock);
                    change = true;
                }

                if (change)
                    FileHelpers.SaveToFile(f, content);
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Starting path:" + Environment.CurrentDirectory);
            Console.WriteLine("Enter package name to update:");
            var pack = Console.ReadLine();
            NugetRepos = NugetHelpers.GetLocalNugetRepos();
            if (args.Length == 0)
                args = new string[2] { "*.csproj", pack };
            //project file filter
            var filefilter = args[0];

            //package filter
            var packageName = args[1];

            ProjectFileChanges(filefilter, packageName);
            PackagesChanges(packageName);
        }
    }
}
