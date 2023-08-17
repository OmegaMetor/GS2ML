using UndertaleModLib;
using System.IO;
using System.Xml;
using System.Drawing;
using System.Reflection;
using System.Diagnostics;
using static System.Environment;

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
        for (int i = 0; i < modDirectories.Length; i++)
        {
            string modPath =  Path.Combine(modsDirectory, Path.GetFileName(modDirectories[i]));
            Console.WriteLine($"Loading mod from \"{modPath}\"...");
            string dllPath = Path.Combine(modPath, Path.GetFileName(modDirectories[i]) + ".dll");
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
                        type = types[t];
                        loadMethod = type.GetMethod("Load");
                        if (loadMethod != null)
                        {
                            break;
                        }
                    }
                    object instanceOfType = Activator.CreateInstance(type);

                    Console.WriteLine("Number of types: " + types.Length.ToString());

                    int audioGroup = 0;
                    loadMethod.Invoke(instanceOfType, new object[] { audioGroup, data });
                    Console.WriteLine($"Successfully loaded mod \"{Path.GetFileName(modDirectories[i])}\"");
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
        Process.Start(gameExecutable, $"-game \"{outputDataWinPath}\"");
    }
}