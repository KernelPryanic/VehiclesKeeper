using System.Reflection;

// This legacy-style project does not honor the csproj <AssemblyVersion>, so the
// real DLL version lives here. The mod reads it at runtime for the menu title,
// and the release workflow (scripts/set-version.ps1) stamps the git tag into both
// this file and the csproj <Version> so everything matches the release.
[assembly: AssemblyTitle("VehicleKeeper")]
[assembly: AssemblyProduct("Vehicle Keeper")]
[assembly: AssemblyVersion("4.2.0.0")]
[assembly: AssemblyFileVersion("4.2.0.0")]
