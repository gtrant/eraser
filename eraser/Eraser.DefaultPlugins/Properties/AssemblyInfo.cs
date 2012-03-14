using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Resources;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Default Plugins")]
[assembly: AssemblyDescription("Default PRNG and Erasure Methods for Eraser")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("The Eraser Project")]
[assembly: AssemblyProduct("Eraser")]
[assembly: AssemblyCopyright("Copyright © 2008-2010 The Eraser Project")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: NeutralResourcesLanguageAttribute("en")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("e2e55c15-f188-4293-a4b2-1d8a016103b5")]

// The plugin is a Core Eraser plugin, declare it so.
[assembly: Eraser.Plugins.PluginLoadingPolicy(Eraser.Plugins.PluginLoadingPolicy.Core)]
