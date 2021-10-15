## Welcome to Kepler!
This is my first ever attempt at creating my own language, and was done with little to no research beforehand (for better or for worse). While I'm sure my experience in languages like JavaScript, Python, and C# have influenced the syntax, I tried to make it as original as possible.

### What is Kepler?
Kepler is an interpreted coding language, designed to have very similar grammar as English. This language is mostly intended for beginners to aid in understanding syntax, layout, and other general programming ideas.

### Why interpretation?
Interpretation allows for faster prototyping and development. There's no having to wait for those pesky build times!

### How do I build it?
In order to build (and use) the compiler, you must have [.NET](https://dotnet.microsoft.com/download) installed.

After ensuring .NET is installed, you can use the following steps:

#### **Building and Installing on Windows** (with `make`)
> These steps require [NSIS](https://nsis.sourceforge.io/Main_Page), and only produces a Windows installer.
1. Open a command prompt and navigate to the root of your local repository.
2. Enter `make all` into your command prompt. Wait for all builds and tests to complete.
3. Navigate to `/build` to find your newly created installation executable.
4. Run the executable, then Kepler will be installed.
5. Finally, restart any opened command prompts, and enter `kepler`!

#### **Building on Windows** (with `make`)
1. Open a command prompt and navigate to the root of your local repository.
2. In the command prompt, simply type `make publish`, and wait for the build to complete.
3. Navigate your command prompt to the `/build/VS_BUILD_OUTPUT` folder.
4. Finally, you can run `./kepler` to enter a live interpretation mode.

#### **Additionally**, when running the interpreter, you can use provide the `--file` argument and a path to a `.kep` file, or the `--help` argument to display all arguments.
#### You can also use the interpreter **without building** by entering `dotnet run` in your terminal.

### Todo list
- [ ] Make the directories associated with building less erratic
- [ ] Multiplatform
