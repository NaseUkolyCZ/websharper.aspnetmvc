#load "tools/includes.fsx"
open IntelliFactory.Build

let bt =
    BuildTool().PackageId("WebSharper.AspNetMvc")
        .VersionFrom("WebSharper")
        .WithFSharpVersion(FSharpVersion.FSharp30)
        .WithFramework(fun fw -> fw.Net45)

let main =
    bt.WebSharper.Library("WebSharper.AspNetMvc")
        .SourcesFromProject()
        .References(fun r ->
            [
                r.Assembly("System.Web")
                r.Assembly("System.ComponentModel.DataAnnotations")
                r.NuGet("Microsoft.AspNet.Mvc").Version("[5.2.3]").Reference()
                r.NuGet("log4net").Version("[2.0.3]").Reference()
            ])

bt.Solution [
    main

    bt.NuGet.CreatePackage()
        .Configure(fun c ->
            { c with
                Title = Some "WebSharper.AspNetMvc"
                LicenseUrl = Some "http://websharper.com/licensing"
                ProjectUrl = Some "https://github.com/intellifactory/websharper.aspnetmvc"
                Description = "WebSharper module for ASP.NET MVC"
                RequiresLicenseAcceptance = true })
        .AddDependency("Microsoft.AspNet.Mvc", "[5.0,6.0)")
        .Add(main)
]
|> bt.Dispatch
