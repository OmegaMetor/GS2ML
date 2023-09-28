namespace GS2ML.Interop;

[AttributeUsage(AttributeTargets.Method)]
public class GmlInterop : Attribute
{
    public string Name;
    public ushort Argc;

    public GmlInterop(string name, ushort argc)
    {
        Name = name;
        Argc = argc;
    }
}