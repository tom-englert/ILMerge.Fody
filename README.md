### This is an add-in for [Fody](https://github.com/Fody/Fody/) [![Build status](https://ci.appveyor.com/api/projects/status/3ntf6e6jp0bppo9q?svg=true)](https://ci.appveyor.com/project/tom-englert/ilmerge-fody) [![NuGet Status](http://img.shields.io/nuget/v/ILMerge.Fody.svg?style=flat-square)](https://www.nuget.org/packages/ILMerge.Fody)
![Icon](package_icon.png)

This add-in merges the referenced types of local dependencies as private types into the target assembly, and removes the references to the local dependencies.
This can help you to e.g. escape the DLL-hell.