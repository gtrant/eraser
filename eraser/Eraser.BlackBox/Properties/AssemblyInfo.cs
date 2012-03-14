using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Resources;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Eraser")]
[assembly: AssemblyDescription("Eraser - Secure Data Removal for Windows")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("The Eraser Project")]
[assembly: AssemblyProduct("Eraser")]
[assembly: AssemblyCopyright("Copyright © 2008-2012 The Eraser Project")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: NeutralResourcesLanguageAttribute("en")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("3460478d-ed1b-4ecc-96c9-2ca0e8500557")]

// The plugin is an optional Eraser plugin, which should default to not load.
[assembly: Eraser.Plugins.PluginLoadingPolicy(Eraser.Plugins.PluginLoadingPolicy.DefaultOff)]
