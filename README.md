[![Build status](https://ci.appveyor.com/api/projects/status/yj6pvlhnjpfrdrqn?svg=true)](https://ci.appveyor.com/project/LokiMidgard/fluent-generator)

# Fluent.Generator
This tool generates design classes of ftl files. It creates boilerplate code to use with [Fluent.Net](https://github.com/blushingpenguin/Fluent.Net)

## Getting Started

Install the [nuget package](https://www.nuget.org/packages/Fluent.Generator/) , add an FTL file to your project, set build action to `Embedded Resource` and Custom Tool to `MSBuild:GenerateFtlTask`.

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

## Parameter Type

Fluent.Generator trys to guess the type of parameters.
```
# $duration (Number) - The duration in seconds.
time-elapsed = Time elapsed: { $duration }s.
```

will result in

```c#
public string this[System.Double duration]
            {
                get
                {
                    return this.messageContext.Format(this.messageContext.GetMessage("time-elapsed"), new Dictionary<string, object>{{"duration", duration}});
                }
            }
```

In order to get correct types, you need to document those in the comments. If a line in the comments start with `$<variable name>` followed by the type in round parenthise `()` followed by a dash `-` and the variable exists
we'll set the parameter type to the provided. You can use .Net types and the string `Number` which will interpreted as double.

You can ommit the type and the parethisees. In that case the object type is used.

## ToDo
 - [x] support control of unicode isolataion marks
 - [x] Display comments on Propertys
 - [ ] Isolate the generation of files so we can actually have some dependecys
   Currently Fluent.net is put inside the nuget and Roslyn code to generate was droped :(
 - [ ] Not sure if the current approach works cross platform
