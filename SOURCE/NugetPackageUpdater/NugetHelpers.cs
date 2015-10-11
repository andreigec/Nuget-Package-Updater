using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NuGet;

namespace NugetPackageUpdater
{
    public static class NugetHelpers
    {
        private static string GetNewestVersion(string nugetPackage, string packagerepo, Dictionary<string, string> cache)
        {
            if (cache.ContainsKey(nugetPackage))
                return cache[nugetPackage];

            //Connect to the official package repository
            var repo = PackageRepositoryFactory.Default.CreateRepository(packagerepo);
            var version = repo.FindPackagesById(nugetPackage).Max(p => p.Version);
            if (version == null)
                return null;

            cache.Add(nugetPackage, version.Version.ToString());

            return version.Version.ToString();
        }

        public static string GetNewestVersion(string nugetPackage, string[] packagerepos, Dictionary<string, string> cache)
        {
            return packagerepos.Select(s => GetNewestVersion(nugetPackage, s, cache)).First(s => s != null);
        }

        public static string[] GetLocalNugetRepos()
        {

            var path = $@"C:\Users\{Environment.UserName}\AppData\Roaming\NuGet\NuGet.Config";
            var r = new Regex("<packageSources>.*?(<add key=\".*?\" value=\"(.*?)\".*?/>.*?)+</packageSources>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var content = FileHelpers.LoadFile(path);
            var paths = r.Matches(content);//
            var paths2 = paths.Cast<Match>().ToList();
            var ret = new List<string>();
            foreach (var p in paths2)
            {
                ret.AddRange(from object p1 in p.Groups[2].Captures select p1.ToString());
            }

            var ret2 = ret.ToArray().Reverse().ToArray();
            return ret2;
        }

    }
}
