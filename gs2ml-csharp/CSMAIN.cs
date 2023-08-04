using UndertaleModLib;
using System.IO;
using System.Xml;
using System.Drawing;
using System.Reflection;
using System.Diagnostics;

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
        string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        string workingDirectory = System.IO.Path.GetDirectoryName(exePath) + "\\..";
        string gamePath = workingDirectory;
        Console.WriteLine("Found game path: " + gamePath);
        string inputDataWin = "data.win";
        string outputDataWin = @"GS2ML_CACHE_data.win";
        string modsDirectory = @"\gs2ml\mods\";



        if (!Directory.Exists(Path.Combine(gamePath, @"gs2ml")))
        {
            Directory.CreateDirectory(Path.Combine(gamePath, @"gs2ml"));
        }
        if (!Directory.Exists(Path.Combine(gamePath, @"gs2ml\cache\")))
        {
            Directory.CreateDirectory(Path.Combine(gamePath, @"gs2ml\cache\"));
        }
        if (!Directory.Exists(Path.Combine(gamePath, @"gs2ml\mods\")))
        {
            Directory.CreateDirectory(Path.Combine(gamePath, @"gs2ml\mods\"));
        }

        Console.WriteLine("Creating file stream...");
        FileStream readStream = File.OpenRead(Path.Combine(gamePath, inputDataWin));
        Console.WriteLine("Reading unmodified data.win from \"" + Path.Combine(gamePath, inputDataWin) + "\"...");
        UndertaleData unmodifiedData = UndertaleIO.Read(readStream, (UndertaleReader.WarningHandlerDelegate)handler, (UndertaleReader.MessageHandlerDelegate)handler2);
        readStream.Dispose();

        UndertaleData data = unmodifiedData;

        Console.WriteLine("Getting mod directory...");
        Console.WriteLine(workingDirectory + modsDirectory);
        string[] modDirectories = Directory.GetDirectories(workingDirectory + modsDirectory);

        for (int i = 0; i < modDirectories.Length; i++)
        {
            string modPath = workingDirectory + modsDirectory + Path.GetFileName(modDirectories[i]);
            Console.WriteLine("Loading mod from \"" + modPath + "\"...");
            string dllPath = modPath + "\\" + Path.GetFileName(modDirectories[i]) + ".dll";
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
                    Console.WriteLine("Successfully loaded mod \"" + Path.GetFileName(modDirectories[i]) + "\"");
                }
                catch (TargetInvocationException tie)
                {
                    Exception e = tie.InnerException;
                    Console.WriteLine("ERROR WHILE LOADING DLL:\n" + e.Message + "\nSTACK TRACE:\n" + e.StackTrace + "\nSkipping to next mod...");
                    data = backupOfBeforeData;
                }
            }
            else
            {
                Console.WriteLine("ERROR: Dll file does not exist: " + dllPath + "! Skipping to next mod...");
            }
        }

        UndertaleData outputData = data;
        if (File.Exists(workingDirectory + "\\" + outputDataWin))
        {
            File.Delete(workingDirectory + "\\" + outputDataWin);
        }
        Console.WriteLine("Creating file stream...");
        FileStream writeStream = File.OpenWrite(workingDirectory + "\\" + outputDataWin);
        Console.WriteLine("Writing modified data.win to \"" + (workingDirectory + "\\" + outputDataWin) + "\"...");
        UndertaleIO.Write(writeStream, outputData);
        writeStream.Dispose();
        Console.WriteLine("Done!");
        Console.WriteLine("Launching Executable from " + workingDirectory + "\\ ORIGINAL.exe...");
        // Hard Coded to wys!!!!!! Fix soon, not good.
        Process.Start(workingDirectory + "\\Will You Snail.exe", "-game " + ".\\" + outputDataWin);
        
    }
}