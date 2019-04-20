# Fluent.Generator
This tool generates design classes of ftl files.

## Getting Started

Install the nuget package (**TODO Create a nuget package!**), add an FTL file to your project, set build action to `Embedded Resource` and Custom Tool to `MSBuild:GenerateFtlTask`.

Having a file like this named TestFtl.ftl:
```ftl
# Simple things are simple.
hello-world = Hello, world!


# Complex things are possible.
shared-photos =
    {$userName} {$photoCount ->
        [one] added a new photo
       *[other] added {$photoCount} new photos
    } to {$userGender ->
        [male] his stream
        [female] her stream
       *[other] their stream
    }.  


```

And you can access the translations in code using
```c#
var firstString = TestFtl.HelloWorld;
var seccondString = TestFtl.SharedPhotos[userName:"Bob", photoCount:3, userGender:"male"];
```
