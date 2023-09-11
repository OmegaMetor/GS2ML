using UndertaleModLib;
#if( easymode )
using System.Runtime;
using UndertaleModLib.Models;
using GMHooker;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text.Json;
#endif

namespace GS2ML_MOD_NAME;

public class GS2ML_MOD_NAME
{
#if( easymode )
    public string modPath;

    public Dictionary<string, string> files = new Dictionary<string, string>();

    public UndertaleData moddingData;
#endif 
    public void Load(int audiogroup, UndertaleData data)
    {
        if(audiogroup != 0){
            return;
        }
        
        // Here, you can modify the data however you want. Go nuts!
#if( easymode )
        moddingData = data;


        //Get the path of the dll that the mod is running on.
        modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        
        
        
        Console.WriteLine($"[GS2ML_MOD_NAME]: Adding objects...");
        AddObjects();

        Console.WriteLine($"[GS2ML_MOD_NAME]: Loading code from files...");
        files = LoadCodeFromFiles(Path.Combine(modPath, "code"));


        //  For adding sprites and sounds, I suggest you use GML's sprite_add, sprite_replace, and audio_create_stream functions instead of C#,
        //  and ask the user to include a folder that contains the PNG and OGG files that you load the sprites and sounds from.
        //
        //  This is because no one knows how to add sprites because of UMLib's lack of documentation,
        //  and as of now I'm too lazy to learn how to add sounds, cause I think it's still complicated.
        //
        //  Be sure to only call audio_create_stream ONCE per sound and store it in a global variable to reference it.
        //  Either that or use audio_destroy_stream() each time after you are done using the sound.
        //  Same thing with sprite_add(), only call it once per sprite and store it in a global variable so you can reference it.
        //  Otherwise there will be a memory leak that will eventuallly crash your game.



        Console.WriteLine($"[GS2ML_MOD_NAME]: Adding code...");
        AddCode();
#endif
    }

#if( easymode)

    public void AddCode(){
        //Add the code with the code adding functions here.
    }
    
    public void AddObjects(){
        //Put your code to add objects here.
    }

    // no touchy (unless you know what you're doing, in which case, please make a pr.)


    public UndertaleGameObject NewObject(string objectName, UndertaleSprite sprite = null, bool visible = true, bool solid = false, bool persistent = false, UndertaleGameObject parentObject=null){
        UndertaleString name = new UndertaleString(objectName);
        UndertaleGameObject newObject = new UndertaleGameObject(){
            Sprite = sprite,
            Persistent = persistent,
            Visible = visible,
            Solid = solid,
            Name = name,
            ParentId=parentObject
        };

        moddingData.Strings.Add(name);
        moddingData.GameObjects.Add(newObject);

        return newObject;
    }

    public UndertaleRoom.GameObject AddObjectToRoom(string roomName, UndertaleGameObject objectToAdd, string layerName){
        UndertaleRoom room = GetRoomFromData(roomName);

        UndertaleRoom.GameObject object_inst = new UndertaleRoom.GameObject()
        {
            InstanceID = moddingData.GeneralInfo.LastObj,
            ObjectDefinition = objectToAdd,
            X = -120,
            Y = -120
        };
        moddingData.GeneralInfo.LastObj++;

        room.Layers.First(layer => layer.LayerName.Content == layerName).InstancesData.Instances.Add(object_inst);

        
        room.GameObjects.Add(object_inst);

        return object_inst;
    }


    public UndertaleGameObject GetObjectFromData(string name){
        return moddingData.GameObjects.ByName(name);
    }
    public UndertaleSprite GetSpriteFromData(string name){
        return moddingData.Sprites.ByName(name);
    }
    public UndertaleRoom GetRoomFromData(string name){
        return moddingData.Rooms.ByName(name);
    }
    public UndertaleCode GetObjectCodeFromData(string name){
        return moddingData.Code.ByName(name);
    }
    public UndertaleFunction GetFunctionFromData(string name){
        return moddingData.Functions.ByName(name);
    }
    public UndertaleScript GetScriptFromData(string name){
        return moddingData.Scripts.ByName(name);
    }
    public UndertaleSound GetSoundFromData(string name){
        return moddingData.Sounds.ByName(name);
    }
    public UndertaleVariable GetVariableFromData(string name){
        return moddingData.Variables.ByName(name);
    }



    public void HookFunctionFromFile(string path, string function)
    {
        string value = "";
        if (files.TryGetValue(path, out value))
        {
            Console.WriteLine($"[GS2ML_MOD_NAME]: loading {path}");
            moddingData.HookFunction(function, value);
        }
        else
        {
            Console.WriteLine($"[GS2ML_MOD_NAME]: Couldn't hook function {path}, it wasn't in the files dictionary.");
        }
    }
    public void CreateFunctionFromFile(string path, string function, ushort argumentCount = 0)
    {
        string value = "";
        if (files.TryGetValue(path, out value))
        {
            Console.WriteLine($"[GS2ML_MOD_NAME]: loading {path}");
            moddingData.CreateFunction(function, value, argumentCount);
        }
        else
        {
            Console.WriteLine($"[GS2ML_MOD_NAME]: Couldn't create function {path}, it wasn't in the files dictionary.");
        }
    }

    public void HookCodeFromFile(string path, string function)
    {
        string value = "";
        if (files.TryGetValue(path, out value))
        {
            Console.WriteLine($"[GS2ML_MOD_NAME]: loading {path}");
            moddingData.HookCode(function, value);
        }
        else
        {
            Console.WriteLine($"[GS2ML_MOD_NAME]: Couldn't hook object script {path}, it wasn't in the files dictionary.");
        }
    }


    public void CreateObjectCodeFromFile(string path, string objName, EventType eventType)
    {
        string value = "";
        UndertaleGameObject obj = moddingData.GameObjects.ByName(objName);

        if (files.TryGetValue(path, out value))
        {
            obj.EventHandlerFor(eventType, moddingData.Strings, moddingData.Code, moddingData.CodeLocals)
            .ReplaceGmlSafe(value, moddingData);
        }
        else
        {
            Console.WriteLine($"[GS2ML_MOD_NAME]: Couldn't change/create object script {path}, it wasn't in the files dictionary.");
        }
    }

    public void CreateObjectCodeFromFile(string path, string objName, EventType eventType, EventSubtypeDraw EventSubtype)
    {
        string value = "";
        UndertaleGameObject obj = moddingData.GameObjects.ByName(objName);

        if (files.TryGetValue(path, out value))
        {
            obj.EventHandlerFor(eventType, EventSubtype, moddingData.Strings, moddingData.Code, moddingData.CodeLocals)
            .ReplaceGmlSafe(value, moddingData);
        }
        else
        {
            Console.WriteLine($"[GS2ML_MOD_NAME]: Couldn't change/create object script {path}, it wasn't in the files dictionary.");
        }
    }
    public void CreateObjectCodeFromFile(string path, string objName, EventType eventType, uint EventSubtype)
    {
        string value = "";
        UndertaleGameObject obj = moddingData.GameObjects.ByName(objName);

        if (files.TryGetValue(path, out value))
        {
            obj.EventHandlerFor(eventType, EventSubtype, moddingData.Strings, moddingData.Code, moddingData.CodeLocals)
            .ReplaceGmlSafe(value, moddingData);
        }
        else
        {
            Console.WriteLine($"[GS2ML_MOD_NAME]: Couldn't change/create object script {path}, it wasn't in the files dictionary.");
        }
    }
    public void CreateObjectCodeFromFile(string path, string objName, EventType eventType, EventSubtypeKey EventSubtype)
    {
        string value = "";
        UndertaleGameObject obj = moddingData.GameObjects.ByName(objName);

        if (files.TryGetValue(path, out value))
        {
            obj.EventHandlerFor(eventType, EventSubtype, moddingData.Strings, moddingData.Code, moddingData.CodeLocals)
            .ReplaceGmlSafe(value, moddingData);
        }
        else
        {
            Console.WriteLine($"[GS2ML_MOD_NAME]: Couldn't change/create object script {path}, it wasn't in the files dictionary.");
        }
    }

    public void CreateObjectCodeFromFile(string path, string objName, EventType eventType, EventSubtypeMouse EventSubtype)
    {
        string value = "";
        UndertaleGameObject obj = moddingData.GameObjects.ByName(objName);

        if (files.TryGetValue(path, out value))
        {
            obj.EventHandlerFor(eventType, EventSubtype, moddingData.Strings, moddingData.Code, moddingData.CodeLocals)
            .ReplaceGmlSafe(value, moddingData);
        }
        else
        {
            Console.WriteLine($"[GS2ML_MOD_NAME]: Couldn't change/create object script {path}, it wasn't in the files dictionary.");
        }
    }


    public void CreateObjectCodeFromFile(string path, string objName, EventType eventType, EventSubtypeOther EventSubtype)
    {
        string value = "";
        UndertaleGameObject obj = moddingData.GameObjects.ByName(objName);

        if (files.TryGetValue(path, out value))
        {
            obj.EventHandlerFor(eventType, EventSubtype, moddingData.Strings, moddingData.Code, moddingData.CodeLocals)
            .ReplaceGmlSafe(value, moddingData);
        }
        else
        {
            Console.WriteLine($"[GS2ML_MOD_NAME]: Couldn't change/create object script {path}, it wasn't in the files dictionary.");
        }
    }

    public void CreateObjectCodeFromFile(string path, string objName, EventType eventType, EventSubtypeStep EventSubtype)
    {
        string value = "";
        UndertaleGameObject obj = moddingData.GameObjects.ByName(objName);

        if (files.TryGetValue(path, out value))
        {
            obj.EventHandlerFor(eventType, EventSubtype, moddingData.Strings, moddingData.Code, moddingData.CodeLocals)
            .ReplaceGmlSafe(value, moddingData);
        }
        else
        {
            Console.WriteLine($"[GS2ML_MOD_NAME]: Couldn't change/create object script {path}, it wasn't in the files dictionary.");
        }
    }

    public static Dictionary<string, string> LoadCodeFromFiles(string path)
    {
        Dictionary<string, string> files = new Dictionary<string, string>();
        string[] codeF = Directory.GetFiles(path, "*.gml");
        Console.WriteLine($"[GS2ML_MOD_NAME]: Loading code from {path}");
        foreach (string f in codeF)
        {
            if (!files.ContainsKey(Path.GetFileName(f)))
            {
                files.Add(Path.GetFileName(f), File.ReadAllText(f));
            }
        }
        return files;
    }
#endif
}
