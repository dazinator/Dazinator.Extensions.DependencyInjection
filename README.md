## Features
- Versioning done with GitVersion.
- Can build via AppVeyor and Azure Devops Pipelines.
- All projects will be SourceLinked to github thanks to `directory.props` file.

# [Getting Started]
- Clone this repo, then push to your own origin.
- Create your solution (.sln) and projects in the `/src` directory.
- Make sure global.json has the right version of the .net sdk that you require.
- For AppVeyor builds, update AppVeyor.yml:
    - dotnet sdk version (currently set to install latest pre-release).
    - Now you can add to AppVeyor.
- For Azure Devops builds:
    - Import pipelines yaml file into Azure Devops pipeline.  
