using UndertaleModLib;
using System.Reflection;
using System.Diagnostics;
using System.Text.Json;
using UndertaleModLib.Models;
using GS2ML.Interop;

//NOTE TO PEOPLE LOOKING AT THIS CODE
//Path.Combine() breaks the thing sometimes. I DONT KNOW WHY, IT SHOULDNT BE HAPPENING.
//It's only SOME of the time too.
//Anyways, that's why I used the messy "path + "\\" + path + "\\" + path...... method. :(

class Program
{

    public static uint currentId = 1;

    public static void Main(string[] args)
    {
        void handler(string e) { Console.WriteLine("EXCEPTION WHILE READING DATA.WIN: \n" + e); };
        void handler2(string e) {};

        string originalDataWinPath = args[0];
        string gameExecutable = args[1];
        string gs2mlDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new();
        string outputDataWinPath = Path.Combine(Path.GetDirectoryName(originalDataWinPath) ?? throw new(), "GS2ML_CACHE_data.win");
        string modsDirectory = Path.Combine(gs2mlDirectory, "mods");

        if (!Directory.Exists(modsDirectory))
        {
            Directory.CreateDirectory(modsDirectory);
        }

        Console.WriteLine("Creating file stream...");
        FileStream readStream = File.OpenRead(originalDataWinPath);
        Console.WriteLine($"Reading unmodified data.win from \"{originalDataWinPath}\"...");
        UndertaleData unmodifiedData = UndertaleIO.Read(readStream, handler, handler2);
        readStream.Dispose();

        UndertaleData data = unmodifiedData;

        UndertaleExtensionFunction setFunction = new() {
            Name = data.Strings.MakeString("interop_set_function"),
            ExtName = data.Strings.MakeString("interop_set_function"),
            Kind = 11,
            ID = currentId
        };

        UndertaleExtensionFile extensionFile = new() {
            Kind = UndertaleExtensionKind.Dll,
            Filename = data.Strings.MakeString(Path.Combine("gs2ml", "gs2ml-interop.dll")),
            InitScript = data.Strings.MakeString(""),
            CleanupScript = data.Strings.MakeString("")
        };
        extensionFile.Functions.Add(setFunction);

        UndertaleExtension interop = new()
        {
            Name = data.Strings.MakeString("GS2ML"),
            ClassName = data.Strings.MakeString(""),
            Version = data.Strings.MakeString("1.0.0"),
            FolderName = data.Strings.MakeString("")
        };
        interop.Files.Add(extensionFile);
        data.Extensions.Add(interop);

        Console.WriteLine("Getting mod directory...");
        Console.WriteLine(modsDirectory);
        string[] modDirectories = Directory.GetDirectories(modsDirectory);
        bool hasErrored = false;

        List<ModInfo> modDataList = new List<ModInfo>();
        for (int i = 0; i < modDirectories.Length; i++)
        {
            string modPath = Path.Combine(modsDirectory, Path.GetFileName(modDirectories[i]));
            Console.WriteLine($"Getting mod info from \"{modPath}\"...");
            if(File.Exists(Path.Combine(modPath, "modinfo.json")))
            {
                string jsonText = File.ReadAllText(Path.Combine(modPath, "modinfo.json"));
                try
                {
                    ModInfo modData = JsonSerializer.Deserialize<ModInfo>(jsonText) ?? throw new();
                    modData.modPath = modDirectories[i];
                    modDataList.Add(modData);
                }
                catch
                {
                    Console.WriteLine("Mod has invalid modinfo.json! Please fix or contact mod developer!");
                    hasErrored = true;
                    break;
                }
            } else
            {
                Console.WriteLine($"There is no mod info file for \"{modPath}\".\nThis isn't an error (most likely).\nWe will still attempt to load the mod without the mod info json file.\nWARNING: THIS WILL ERROR IN A FUTURE VERSION OF GS2ML!!!\nPausing so this message is seen, press enter to continue loading.");
                Console.ReadLine();
                ModInfo modData = new()
                {
                    modName = "Unknown mod " + i,
                    authors = new string[] { "Unknown Author" },
                    description = "This mod does not have a modinfo.json file. This could be because it is an old mod or because the owner forgot to add one.",
                    priority = int.MaxValue, // If it doesn't have the json, it should load last.
                    modPath = modDirectories[i]
                };
                modDataList.Add(modData);
            }
        }
        List<ModInfo> prioritizedModInfo = modDataList.OrderBy(o => o.priority).ToList();
        for (int i = 0; i < prioritizedModInfo.Count; i++)
        {
            if (hasErrored) break;
            string modPath =  Path.Combine(modsDirectory, Path.GetFileName(prioritizedModInfo[i].modPath));
            Console.WriteLine($"Loading mod from \"{modPath}\"...");
            string dllPath = Path.Combine(modPath, Path.GetFileName(prioritizedModInfo[i].modPath) + ".dll");
            if (File.Exists(dllPath))
            {
                UndertaleData backupOfBeforeData = data;
                Console.WriteLine("Loading dll from " + dllPath);
                try
                {
                    Assembly assembly = Assembly.LoadFrom(dllPath);

                    Type[] types = assembly.GetTypes();

                    Type type = types[0];

                    MethodInfo? loadMethod = type.GetMethod("Load");

                    for (var t = 0; t < types.Length; t++)
                    {
                        if (loadMethod != null)
                        {
                            break;
                        }
                        type = types[t];
                        loadMethod = type.GetMethod("Load");
                    }

                    foreach (Type t in assembly.GetTypes())
                    {
                        foreach (MethodInfo methodInfo in t.GetMethods())
                        {
                            if (Attribute.IsDefined(methodInfo, typeof(GmlInterop)))
                            {
                                GmlInterop? attribute = (GmlInterop?)Attribute.GetCustomAttribute(methodInfo, typeof(GmlInterop));
                                // note to self pass full qualified name to the wrapper
                                Console.WriteLine($"Making interop fn: {attribute.Name}");

                                MakeInteropFunction(
                                    attribute.Name, 
                                    attribute.Argc, 
                                    methodInfo.DeclaringType.Namespace,
                                    methodInfo.DeclaringType.Name,
                                    methodInfo.Name,
                                    Path.GetFileNameWithoutExtension(dllPath),
                                    extensionFile,
                                    data
                                );
                            }
                        }
                    }

                    object instanceOfType = Activator.CreateInstance(type) ?? throw new();

                    if (loadMethod == null) continue;

                    Console.WriteLine("Number of types: " + types.Length);
                    
                    int audioGroup = 0;
                    loadMethod.Invoke(instanceOfType, new object[] { audioGroup, data });
                    Console.WriteLine($"Successfully loaded mod \"{Path.GetFileName(prioritizedModInfo[i].modPath)}\"");
                }
                catch (TargetInvocationException tie)
                {
                    Exception e = tie.InnerException ?? new();
                    Console.WriteLine("ERROR WHILE LOADING DLL:\n" + e.Message + "\nSTACK TRACE:\n" + e.StackTrace + "\nSkipping to next mod...");
                    data = backupOfBeforeData;
                    hasErrored = true;
                }
            }
            else
            {
                Console.WriteLine($"ERROR: Dll file does not exist: {dllPath}! Skipping to next mod...");
                hasErrored = true;
            }
        }

        if(hasErrored){
            Console.Write(
@"

********************
There was an error during the mod loading process!
Please review the above error!

If you wish to continue launching the game, type 'y' and press enter.
Any other input will close this window without launching the game.

If you continue to launch the game, the mods you have added may not work as expected, or even may not work at all.
********************
Continue? (y to continue, anything else to exit.)
>");
            string? Input = Console.ReadLine();
            if(Input != "y")
                return;
        }

        UndertaleData outputData = data;
        if (File.Exists(outputDataWinPath))
        {
            File.Delete(outputDataWinPath);
        }
        Console.WriteLine("Creating file stream...");
        FileStream writeStream = File.OpenWrite(outputDataWinPath);
        Console.WriteLine($"Writing modified data.win to \"{outputDataWinPath}\"...");
        UndertaleIO.Write(writeStream, outputData);
        writeStream.Dispose();
        Console.WriteLine("Done!");
        Console.WriteLine("Launching Executable from " + gameExecutable);
        string argstring = "";
        for(int i = 2; i < args.Length; i++)
        {
            argstring += $" \"{args[i]}\"";
        }
        Process.Start(gameExecutable, $"-game \"{outputDataWinPath}\"" + argstring);
    }

    public static void MakeInteropFunction(string name, ushort argc, string ns, string clazz, string fn, string dll, UndertaleExtensionFile extensionFile, UndertaleData data)
    {
        currentId++;
        UndertaleExtensionFunction function = new() {
            Name = data.Strings.MakeString(name + "_interop"),
            ExtName = data.Strings.MakeString("interop_function"),
            Kind = 11,
            ID = currentId
        };
        extensionFile.Functions.Add(function);
        string args = "";
        for (int i = 0; i < argc; i++)
        {
            args += $"argument{i}{(i != argc - 1 ? ", " : "")}";
        }
        CreateLegacyScript(data, name, $"interop_set_function(\"{dll}\", \"{ns}\", \"{clazz}\", \"{fn}\", {argc});\nreturn {name}_interop({args});", argc);
    }

    public static UndertaleCode CreateCode(UndertaleData data, UndertaleString name, out UndertaleCodeLocals locals) {
        locals = new UndertaleCodeLocals {
            Name = name
        };
        locals.Locals.Add(new UndertaleCodeLocals.LocalVar {
            Name = data.Strings.MakeString("arguments"),
            Index = 2
        });
        data.CodeLocals.Add(locals);

        UndertaleCode mainCode = new() {
            Name = name,
            LocalsCount = 1,
            ArgumentsCount = 0
        };
        data.Code.Add(mainCode);

        return mainCode;
    }

    public static UndertaleScript CreateLegacyScript(UndertaleData data, string name, string code, ushort argCount) {
        UndertaleString mainName = data.Strings.MakeString(name, out int nameIndex);
        UndertaleCode mainCode = CreateCode(data, mainName, out _);
        mainCode.ArgumentsCount = argCount;

        mainCode.ReplaceGML(code, data);

        UndertaleScript script = new() {
            Name = mainName,
            Code = mainCode
        };
        data.Scripts.Add(script);

        UndertaleFunction function = new() {
            Name = mainName,
            NameStringID = nameIndex
        };
        data.Functions.Add(function);

        return script;
    }

}

public class ModInfo
{
    public string modPath = "";
    public string? modName { get; set; }
    public string[]? authors { get; set; }
    public string? description { get; set; }
    public int priority { get; set; }
}