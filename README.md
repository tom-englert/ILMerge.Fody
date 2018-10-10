### This is an add-in for [Fody](https://github.com/Fody/Fody/) ![badge](https://tom-englert.visualstudio.com/_apis/public/build/definitions/75bf84d2-d359-404a-a712-07c9f693f635/[todo]/badge) [![NuGet Status](http://img.shields.io/nuget/v/AutoProperties.Fody.svg?style=flat-square)](https://www.nuget.org/packages/ILMerge.Fody)
![Icon](package_icon.png)

This add-in merges the referenced types of local dependencies as private types into the target assembly, and removes the references to the local dependencies.
This can help you to e.g. escape the DLL-hell.