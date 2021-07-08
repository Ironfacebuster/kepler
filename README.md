## Welcome to Kepler!
This is my first ever attempt at creating my own language, and was done with little to no research beforehand (for better or for worse). While I'm sure my experience in languages like JavaScript, Python, and C# have influenced the syntax, I tried to make it as original as possible.

### What is Kepler?
Kepler is an interpreted coding language, designed to have very similar grammar as English. This language is mostly intended for beginners to aid in understanding syntax, layout, and other general programming ideas.

### Why interpretation?
Interpretation allows for faster prototyping and development. There's no having to wait for those pesky build times!

### How do I build it?
In order to build (and use) the compiler, you must have [.NET](https://dotnet.microsoft.com/download) installed.

After ensuring .NET is installed, you can use the following steps:
> **NOTICE:** If you're using Windows you might want to have `make` installed in some form.

#### **Windows** (with `make`)
1. Open a command prompt and navigate to the root of your local repository.
2. In the command prompt, simply type `make`
3. Wait for the build and test to complete.
4. Navigate your command prompt to the `BUILD_OUTPUT` folder.
5. Finally, you can run `./KeplerCompiler` to enter a live interpretation mode.

#### **MacOS** and **Windows** (without `make`)
1. Open a terminal (or command prompt) and navigate to the root of your local repository
2. Enter `dotnet build --output BUILD_OUTPUT` into your terminal to build an executable file.
3. Navigate your command prompt to the `BUILD_OUTPUT` folder.
4. Finally, you can run `./KeplerCompiler` to enter a live interpretation mode.

> Building in this way only produces a Windows executable. This will be changed in the future.

#### **Additionally**, when running the compiler, you can use provide the `--file` argument and a path to a `.sc` file, or the `--help` argument to display all arguments.
#### You can also use the compiler **without building** by using `dotnet run` in your terminal.