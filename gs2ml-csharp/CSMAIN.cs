using UndertaleModLib;
using System.IO;
using System.Xml;
using System.Drawing;
using System.Reflection;
using System.Diagnostics;
using static System.Environment;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Linq;

//NOTE TO PEOPLE LOOKING AT THIS CODE
//Path.Combine() breaks the thing sometimes. I DONT KNOW WHY, IT SHOULDNT BE HAPPENING.
//It's only SOME of the time too.
//Anyways, that's why I used the messy "path + "\\" + path + "\\" + path...... method. :(

class GS2ML
{
    public static void Main(string[] args)
    {
        void handler(string e)
        {
            Console.WriteLine("EXCEPTION WHILE READING DATA.WIN: \n" + e);
            return;
        }
        void handler2(string e)
        {
            //Console.WriteLine(e);
            return;
        }
        string originalDataWinPath = args[0];
        string gameExecutable = args[1];
        string gs2mlDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        string outputDataWinPath = Path.Combine(Path.GetDirectoryName(originalDataWinPath), "GS2ML_CACHE_data.win");
        string modsDirectory = Path.Combine(gs2mlDirectory, "mods");

        if (!Directory.Exists(modsDirectory))
        {
            Directory.CreateDirectory(modsDirectory);
        }

        Console.WriteLine("Creating file stream...");
        FileStream readStream = File.OpenRead(originalDataWinPath);
        Console.WriteLine($"Reading unmodified data.win from \"{originalDataWinPath}\"...");
        UndertaleData unmodifiedData = UndertaleIO.Read(readStream, (UndertaleReader.WarningHandlerDelegate)handler, (UndertaleReader.MessageHandlerDelegate)handler2);
        readStream.Dispose();

        UndertaleData data = unmodifiedData;

        Console.WriteLine("Getting mod directory...");
        Console.WriteLine(modsDirectory);
        string[] modDirectories = Directory.GetDirectories(modsDirectory);
        bool hasErrored = false;
        string[] blacklisted;
        string[] whitelisted;
        if (File.Exists(Path.Combine(gs2mlDirectory, "blacklist.txt")))
        {
            blacklisted = File.ReadAllLines(Path.Combine(gs2mlDirectory, "blacklist.txt");
        }
        if (File.Exists(Path.Combine(gs2mlDirectory, "whitelist.txt")))
        {
            whitelistd = File.ReadAllLines(Path.Combine(gs2mlDirectory, "whitelist.txt");
        }
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
                    ModInfo modData = JsonSerializer.Deserialize<ModInfo>(jsonText);
                    modData.modPath = modDirectories[i];
                    modDataList.Add(modData);
                } catch(Exception e)
                {
                    Console.WriteLine("Mod has invalid modinfo.json! Please fix or contact mod developer!");
                    hasErrored = true;
                    break;
                }
            } else
            {
                Console.WriteLine($"There is no mod info file for \"{modPath}\".\nThis isn't an error (most likely).\nWe will still attempt to load the mod without the mod info json file.\nWARNING: THIS WILL ERROR IN A FUTURE VERSION OF GS2ML!!!\nPausing so this message is seen, press enter to continue loading.");
                Console.ReadLine();
                ModInfo modData = new ModInfo
                {
                    modName = "Unknown mod " + i.ToString(),
                    authors = new string[]{ "Unknown Author" },
                    description = "This mod does not have a modinfo.json file. This could be because it is an old mod or because the owner forgot to add one.",
                    priority = 999999 // If it doesn't have the json, it should load last.
                };

                if(whitelisted.Length != 0)
                {
                    if(!(Array.IndexOf(whitelisted, modData.modName) >= 0))
                        continue
                }
                if(Array.IndexOf(blacklisted, modData.modName) >= 0)
                {
                    continue
                }
                
                modData.modPath = modDirectories[i];
                modDataList.Add(modData);
            }
        }
        List<ModInfo> prioritizedModInfo = modDataList.OrderBy(o => o.priority).ToList();
        for (int i = 0; i < prioritizedModInfo.Count; i++)
        {
            if(hasErrored) break;
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
                    MethodInfo loadMethod = type.GetMethod("Load");
                    for (var t = 0; t < types.Length; t++)
                    {
                        if (loadMethod != null)
                        {
                            break;
                        }
                        type = types[t];
                        loadMethod = type.GetMethod("Load");
                    }
                    object instanceOfType = Activator.CreateInstance(type);

                    Console.WriteLine("Number of types: " + types.Length.ToString());

                    int audioGroup = 0;
                    loadMethod.Invoke(instanceOfType, new object[] { audioGroup, data });
                    Console.WriteLine($"Successfully loaded mod \"{Path.GetFileName(prioritizedModInfo[i].modPath)}\"");
                }
                catch (TargetInvocationException tie)
                {
                    Exception e = tie.InnerException;
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
            string Input = Console.ReadLine();
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
            argstring += " \"";
            argstring += args[i];
            argstring += "\"";
        }
        Process.Start(gameExecutable, $"-game \"{outputDataWinPath}\"" + argstring);
    }
}

public class ModInfo
{
    public string modPath = "";
    public string modName { get; set; }
    public string[] authors { get; set; }
    public string description { get; set; }
    public int priority { get; set; }
}
