## Welcome to Kepler!
This is my first ever attempt at creating my own language, and was done with little to no research beforehand (for better or for worse). While I'm sure my experience in languages like JavaScript, Python, and C# have influenced the syntax, I tried to make it as original as possible.

### What is Kepler?
Kepler is an interpreted coding language, designed to have very similar grammar as English. This language is mostly intended for beginners to aid in understanding syntax, layout, and other general programming ideas.

### Why interpretation?
Interpretation allows for faster prototyping and development. There's no having to wait for those pesky build times!

### How do I build it?
In order to build (and use) the compiler, you must have [.NET](https://dotnet.microsoft.com/download) installed.
**If using Windows** you must also have make installed in some form.

After ensuring .NET is installed, you can use the following steps:

#### **Windows**
1. Open a command prompt and navigate to the root of your local repository.
2. In the command prompt, simply type `make`
3. Wait for the build and test to complete.
4. Navigate your command prompt to the `BUILD_OUTPUT` folder.
5. Finally, you can run `./KeplerCompiler` to enter a live interpretation mode.

#### **MacOS**
1. Open a terminal and navigate to the root of your local repository
2. Type `dotnet run` into your terminal to enter a live interpretation mode.

**Additionally**, you can use provide the `--file` argument and a path to a `.sc` file, or the `--help` argument to display all arguments.