# <b>Not</b>AutoMapper

#### Why?
Writing code that maps from one data representation to another is, to most developers, a simple task.
But simultaneously a time-consuming task that on the surface does not yield much of a result.
This can sometimes lead to the original representation being used in all types of contexts throughout an application.

To avoid this issue, some have turned to libraries such as AutoMapper, through which mapping can be somewhat automated.
[AutoMapper.org](https://automapper.org/) provides the following motivation:
> AutoMapper is a simple little library built to solve a deceptively complex problem - getting rid of code that mapped one object to another. This type of code is rather dreary and boring to write, so why not invent a tool to do it for us?

This project shares the same concerns, but use quite different means to achieve its results.

#### What?
An extension for Visual Studio that will assist in the generation of mapping methods.
Through quick-actions, the developer is able to auto-complete mapping methods, and is only required to fill in the details.