// Texture packer by Samuel Roy
// Uses code from https://github.com/mfascia/TexturePacker
// Uses code from ExportAllTextures.csx

using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

bool recursiveCheck = ScriptQuestion(@"This script requires will import all valid sprites from all subdirectories.
If you do not want this to occur, please click ""No"" to cancel the script.
Then make sure that the sprites you wish to import are in a separate directory with no subdirectories.
");
if (!recursiveCheck)
    throw new ScriptException("Script cancelled.");

// Get import folder
string importFolder = PromptChooseDirectory();
if (importFolder == null)
    throw new ScriptException("The import folder was not set.");

//Stop the script if there's missing sprite entries or w/e.
string[] dirFiles = Directory.GetFiles(importFolder, "*.png", SearchOption.AllDirectories);
foreach (string file in dirFiles)
{
    string FileNameWithExtension = Path.GetFileName(file);
    string stripped = Path.GetFileNameWithoutExtension(file);
    int lastUnderscore = stripped.LastIndexOf('_');
    string spriteName = "";
    try
    {
        spriteName = stripped.Substring(0, lastUnderscore);
    }
    catch
    {
        throw new ScriptException("Getting the sprite name of " + FileNameWithExtension + " failed.");
    }
    Int32 validFrameNumber = 0;
    try
    {
        validFrameNumber = Int32.Parse(stripped.Substring(lastUnderscore + 1));
    }
    catch
    {
        throw new ScriptException("The index of " + FileNameWithExtension + " could not be determined.");
    }
    int frame = 0;
    try
    {
        frame = Int32.Parse(stripped.Substring(lastUnderscore + 1));
    }
    catch
    {
        throw new ScriptException(FileNameWithExtension + " is using letters instead of numbers. The script has stopped for your own protection.");
    }
    int prevframe = 0;
    if (frame != 0)
    {
        prevframe = (frame - 1);
    }
    if (frame < 0)
    {
        throw new ScriptException(spriteName + " is using an invalid numbering scheme. The script has stopped for your own protection.");
    }
    string[] dupFiles = Directory.GetFiles(importFolder, FileNameWithExtension, SearchOption.AllDirectories);
    if (dupFiles.Length > 1)
        throw new ScriptException("Duplicate file detected. There are " + dupFiles.Length + " files named: " + FileNameWithExtension);
    var prevFrameName = spriteName + "_" + prevframe.ToString() + ".png";
    string[] previousFrameFiles = Directory.GetFiles(importFolder, prevFrameName, SearchOption.AllDirectories);
    if (previousFrameFiles.Length < 1)
        throw new ScriptException(spriteName + " is missing one or more indexes. The detected missing index is: " + prevFrameName);
}

// Get directory path
DirectoryInfo dir = Directory.CreateDirectory(Path.Combine(ExePath, "Packager"));

// Clear any files if they already exist
foreach (FileInfo file in dir.GetFiles())
    file.Delete();
foreach (DirectoryInfo di in dir.GetDirectories())
    di.Delete(true);

// Start export of all existing textures

int progress = 0;
string exportedTexturesFolder = dir.FullName + Path.DirectorySeparatorChar + "Textures" + Path.DirectorySeparatorChar;
TextureWorker worker = new TextureWorker();
Dictionary<string, int[]> assetCoordinateDict = new Dictionary<string, int[]>();
Dictionary<string, string> assetTypeDict = new Dictionary<string, string>();

Directory.CreateDirectory(exportedTexturesFolder);

SetProgressBar(null, "Existing Textures Exported", 0, Data.TexturePageItems.Count);
StartProgressBarUpdater();

await DumpSprites();
await DumpFonts();
await DumpBackgrounds();
worker.Cleanup();

await StopProgressBarUpdater();
HideProgressBar();

async Task DumpSprites()
{
    await Task.Run(() => Parallel.ForEach(Data.Sprites, DumpSprite));
}

async Task DumpBackgrounds()
{
    await Task.Run(() => Parallel.ForEach(Data.Backgrounds, DumpBackground));
}

async Task DumpFonts()
{
    await Task.Run(() => Parallel.ForEach(Data.Fonts, DumpFont));
}

void DumpSprite(UndertaleSprite sprite)
{
    for (int i = 0; i < sprite.Textures.Count; i++)
    {
        if (sprite.Textures[i]?.Texture != null)
        {
            UndertaleTexturePageItem tex = sprite.Textures[i].Texture;
            worker.ExportAsPNG(tex, exportedTexturesFolder + sprite.Name.Content + "_" + i + ".png");
            assetCoordinateDict.Add(sprite.Name.Content + "_" + i, new int[] { tex.TargetX, tex.TargetY, tex.TargetWidth, tex.TargetHeight, tex.BoundingWidth, tex.BoundingHeight });
            assetTypeDict.Add(sprite.Name.Content + "_" + i, "spr");
        }
    }

    AddProgress(sprite.Textures.Count);
}

void DumpFont(UndertaleFont font)
{
    if (font.Texture != null)
    {
        UndertaleTexturePageItem tex = font.Texture;
        worker.ExportAsPNG(tex, exportedTexturesFolder + font.Name.Content + ".png");
        assetCoordinateDict.Add(font.Name.Content, new int[] { tex.TargetX, tex.TargetY, tex.TargetWidth, tex.TargetHeight, tex.BoundingWidth, tex.BoundingHeight });
        assetTypeDict.Add(font.Name.Content, "fnt");

        AddProgress(1);
    }
}

void DumpBackground(UndertaleBackground background)
{
    if (background.Texture != null)
    {
        UndertaleTexturePageItem tex = background.Texture;
        worker.ExportAsPNG(tex, exportedTexturesFolder + background.Name.Content + ".png");
        assetCoordinateDict.Add(background.Name.Content, new int[] { tex.TargetX, tex.TargetY, tex.TargetWidth, tex.TargetHeight, tex.BoundingWidth, tex.BoundingHeight });
        assetTypeDict.Add(background.Name.Content, "bg");

        AddProgress(1);
    }
}

// End export

string sourcePath = exportedTexturesFolder;
string searchPattern = "*.png";
string outName = dir.FullName + Path.DirectorySeparatorChar + "atlas.txt";
int textureSize = 2048;
int PaddingValue = 2;
bool debug = false;

// Add imported textures to existing textures, overwrite those with the same name.
DirectoryInfo textureDirectory = new DirectoryInfo(importFolder);
FileInfo[] files = textureDirectory.GetFiles(searchPattern, SearchOption.AllDirectories);
foreach (FileInfo file in files)
{
    string destFile = Path.Combine(exportedTexturesFolder, file.Name);
    string sourceFile = Path.Combine(importFolder, file.Name);
    string stripped = Path.GetFileNameWithoutExtension(sourceFile);
    if (assetCoordinateDict.ContainsKey(stripped))
        assetCoordinateDict.Remove(stripped);
    File.Copy(sourceFile, destFile, true);
}

try
{
    string[] marginLines = File.ReadAllLines(importFolder + Path.DirectorySeparatorChar + "margins.txt");
    foreach (String str in marginLines)
    {
        string key = str.Substring(0, str.IndexOf(','));
        string tmp = str;
        tmp = tmp.Substring(str.IndexOf(',') + 1);
        int[] marginValues = new int[6];
        for (int i = 0; i < 5; i++)
        {
            marginValues[i] = Int32.Parse(tmp.Substring(0, tmp.IndexOf(',')), System.Globalization.NumberStyles.Integer);
            tmp = tmp.Substring(tmp.IndexOf(',') + 1);
        }
        marginValues[5] = Int32.Parse(tmp, System.Globalization.NumberStyles.Integer);
        if (assetCoordinateDict.ContainsKey(key))
            assetCoordinateDict[key] = marginValues;
        else
            assetCoordinateDict.Add(key, marginValues);
    }
}
catch (IOException e)
{
    if (!ScriptQuestion("Margin values were not found.\nImport with default values?"))
        return;
}

// Delete all existing Textures and TextureSheets
Data.TexturePageItems.Clear();
Data.EmbeddedTextures.Clear();

// Run the texture packer using borrowed and slightly modified code from the
// Texture packer sourced above
Packer packer = new Packer();
packer.Process(sourcePath, searchPattern, textureSize, PaddingValue, debug);
packer.SaveAtlasses(outName);

int lastTextPage = Data.EmbeddedTextures.Count - 1;
int lastTextPageItem = Data.TexturePageItems.Count - 1;

// Import everything into UMT
string prefix = outName.Replace(Path.GetExtension(outName), "");
int atlasCount = 0;
foreach (Atlas atlas in packer.Atlasses)
{
    string atlasName = String.Format(prefix + "{0:000}" + ".png", atlasCount);
    Bitmap atlasBitmap = new Bitmap(atlasName);
    UndertaleEmbeddedTexture texture = new UndertaleEmbeddedTexture();
    texture.Name = new UndertaleString("Texture " + ++lastTextPage);
    texture.TextureData.TextureBlob = File.ReadAllBytes(atlasName);
    Data.EmbeddedTextures.Add(texture);
    foreach (Node n in atlas.Nodes)
    {
        if (n.Texture != null)
        {
            // Initalize values of this texture
            UndertaleTexturePageItem texturePageItem = new UndertaleTexturePageItem();
            texturePageItem.Name = new UndertaleString("PageItem " + ++lastTextPageItem);
            texturePageItem.SourceX = (ushort)n.Bounds.X;
            texturePageItem.SourceY = (ushort)n.Bounds.Y;
            texturePageItem.SourceWidth = (ushort)n.Bounds.Width;
            texturePageItem.SourceHeight = (ushort)n.Bounds.Height;
            texturePageItem.BoundingWidth = (ushort)n.Bounds.Width;
            texturePageItem.BoundingHeight = (ushort)n.Bounds.Height;
            texturePageItem.TexturePage = texture;

            // Add this texture to UMT
            Data.TexturePageItems.Add(texturePageItem);

            // String processing
            string stripped = Path.GetFileNameWithoutExtension(n.Texture.Source);
            int firstUnderscore = stripped.IndexOf('_');
            string spriteType = "";
            try
            {
                if (assetTypeDict.ContainsKey(stripped))
                    spriteType = assetTypeDict[stripped];
                else
                    spriteType = stripped.Substring(0, firstUnderscore);
            }
            catch (Exception e)
            {
                if (stripped.Equals("background0") || stripped.Equals("background1"))
                {
                    UndertaleBackground background = Data.Backgrounds.ByName(stripped); //Use stripped instead of sprite name or else the last index calculation gives us a bad string.
                    background.Texture = texturePageItem;
                    setTextureTargetBounds(texturePageItem, stripped, n);
                    continue;
                }
                else
                {
                    ScriptMessage("Error: Image " + stripped + " has an invalid name.");
                    continue;
                }
            }
            setTextureTargetBounds(texturePageItem, stripped, n);
            // Special Cases for backgrounds and fonts
            if (spriteType.Equals("bg"))
            {
                UndertaleBackground background = Data.Backgrounds.ByName(stripped); // Use stripped instead of sprite name or else the last index calculation gives us a bad string.
                background.Texture = texturePageItem;
            }
            else if (spriteType.Equals("fnt"))
            {
                UndertaleFont font = Data.Fonts.ByName(stripped); // Use stripped instead of sprite name or else the last index calculation gives us a bad string.
                font.Texture = texturePageItem;
            }
            else
            {
                // Get sprite to add this texture to
                string spriteName;
                int lastUnderscore, frame;
                try
                {
                    lastUnderscore = stripped.LastIndexOf('_');
                    spriteName = stripped.Substring(0, lastUnderscore);
                    frame = Int32.Parse(stripped.Substring(lastUnderscore + 1));
                }
                catch (Exception e)
                {
                    ScriptMessage("Error: Image " + stripped + " has an invalid name. Skipping...");
                    continue;
                }
                UndertaleSprite sprite = null;
                sprite = Data.Sprites.ByName(spriteName);

                // Create TextureEntry object
                UndertaleSprite.TextureEntry texentry = new UndertaleSprite.TextureEntry();
                texentry.Texture = texturePageItem;

                // Set values for new sprites
                if (sprite == null)
                {
                    UndertaleString spriteUTString = Data.Strings.MakeString(spriteName);
                    UndertaleSprite newSprite = new UndertaleSprite();
                    newSprite.Name = spriteUTString;
                    newSprite.Width = (uint)n.Bounds.Width;
                    newSprite.Height = (uint)n.Bounds.Height;
                    newSprite.MarginLeft = 0;
                    newSprite.MarginRight = n.Bounds.Width - 1;
                    newSprite.MarginTop = 0;
                    newSprite.MarginBottom = n.Bounds.Height - 1;
                    newSprite.OriginX = 0;
                    newSprite.OriginY = 0;
                    if (frame > 0)
                    {
                        for (int i = 0; i < frame; i++)
                            newSprite.Textures.Add(null);
                    }
                    newSprite.CollisionMasks.Add(newSprite.NewMaskEntry());
                    Rectangle bmpRect = new Rectangle(n.Bounds.X, n.Bounds.Y, n.Bounds.Width, n.Bounds.Height);
                    System.Drawing.Imaging.PixelFormat format = atlasBitmap.PixelFormat;
                    Bitmap cloneBitmap = atlasBitmap.Clone(bmpRect, format);
                    int width = ((n.Bounds.Width + 7) / 8) * 8;
                    BitArray maskingBitArray = new BitArray(width * n.Bounds.Height);
                    for (int y = 0; y < n.Bounds.Height; y++)
                    {
                        for (int x = 0; x < n.Bounds.Width; x++)
                        {
                            Color pixelColor = cloneBitmap.GetPixel(x, y);
                            maskingBitArray[y * width + x] = (pixelColor.A > 0);
                        }
                    }
                    BitArray tempBitArray = new BitArray(width * n.Bounds.Height);
                    for (int i = 0; i < maskingBitArray.Length; i += 8)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            tempBitArray[j + i] = maskingBitArray[-(j - 7) + i];
                        }
                    }
                    int numBytes;
                    numBytes = maskingBitArray.Length / 8;
                    byte[] bytes = new byte[numBytes];
                    tempBitArray.CopyTo(bytes, 0);
                    for (int i = 0; i < bytes.Length; i++)
                        newSprite.CollisionMasks[0].Data[i] = bytes[i];
                    newSprite.Textures.Add(texentry);
                    Data.Sprites.Add(newSprite);
                    continue;
                }
                if (frame > sprite.Textures.Count - 1)
                {
                    while (frame > sprite.Textures.Count - 1)
                    {
                        sprite.Textures.Add(texentry);
                    }
                    continue;
                }
                sprite.Textures[frame] = texentry;
            }
        }
    }
    // Increment atlas
    atlasCount++;
}

ScriptMessage("Import Complete!");


void setTextureTargetBounds(UndertaleTexturePageItem tex, string textureName, Node n)
{
    if (assetCoordinateDict.ContainsKey(textureName))
    {
        int[] coords = assetCoordinateDict[textureName];
        tex.TargetX = (ushort)coords[0];
        tex.TargetY = (ushort)coords[1];
        tex.TargetWidth = (ushort)coords[2];
        tex.TargetHeight = (ushort)coords[3];
        tex.BoundingWidth = (ushort)coords[4];
        tex.BoundingHeight = (ushort)coords[5];
    }
    else
    {
        tex.TargetX = 0;
        tex.TargetY = 0;
        tex.TargetWidth = (ushort)n.Bounds.Width;
        tex.TargetHeight = (ushort)n.Bounds.Height;
    }
}

public class TextureInfo
{
    public string Source;
    public int Width;
    public int Height;
}

public enum SplitType
{
    Horizontal,
    Vertical,
}

public enum BestFitHeuristic
{
    Area,
    MaxOneAxis,
}

public class Node
{
    public Rectangle Bounds;
    public TextureInfo Texture;
    public SplitType SplitType;
}

public class Atlas
{
    public int Width;
    public int Height;
    public List<Node> Nodes;
}

public class Packer
{
    public List<TextureInfo> SourceTextures;
    public StringWriter Log;
    public StringWriter Error;
    public int Padding;
    public int AtlasSize;
    public bool DebugMode;
    public BestFitHeuristic FitHeuristic;
    public List<Atlas> Atlasses;

    public Packer()
    {
        SourceTextures = new List<TextureInfo>();
        Log = new StringWriter();
        Error = new StringWriter();
    }

    public void Process(string _SourceDir, string _Pattern, int _AtlasSize, int _Padding, bool _DebugMode)
    {
        Padding = _Padding;
        AtlasSize = _AtlasSize;
        DebugMode = _DebugMode;
        //1: scan for all the textures we need to pack
        ScanForTextures(_SourceDir, _Pattern);
        List<TextureInfo> textures = new List<TextureInfo>();
        textures = SourceTextures.ToList();
        //2: generate as many atlasses as needed (with the latest one as small as possible)
        Atlasses = new List<Atlas>();
        while (textures.Count > 0)
        {
            Atlas atlas = new Atlas();
            atlas.Width = _AtlasSize;
            atlas.Height = _AtlasSize;
            List<TextureInfo> leftovers = LayoutAtlas(textures, atlas);
            if (leftovers.Count == 0)
            {
                // we reached the last atlas. Check if this last atlas could have been twice smaller
                while (leftovers.Count == 0)
                {
                    atlas.Width /= 2;
                    atlas.Height /= 2;
                    leftovers = LayoutAtlas(textures, atlas);
                }
                // we need to go 1 step larger as we found the first size that is to small
                atlas.Width *= 2;
                atlas.Height *= 2;
                leftovers = LayoutAtlas(textures, atlas);
            }
            Atlasses.Add(atlas);
            textures = leftovers;
        }
    }

    public void SaveAtlasses(string _Destination)
    {
        int atlasCount = 0;
        string prefix = _Destination.Replace(Path.GetExtension(_Destination), "");
        string descFile = _Destination;
        StreamWriter tw = new StreamWriter(_Destination);
        tw.WriteLine("source_tex, atlas_tex, x, y, width, height");
        foreach (Atlas atlas in Atlasses)
        {
            string atlasName = String.Format(prefix + "{0:000}" + ".png", atlasCount);
            //1: Save images
            Image img = CreateAtlasImage(atlas);
            img.Save(atlasName, System.Drawing.Imaging.ImageFormat.Png);
            //2: save description in file
            foreach (Node n in atlas.Nodes)
            {
                if (n.Texture != null)
                {
                    tw.Write(n.Texture.Source + ", ");
                    tw.Write(atlasName + ", ");
                    tw.Write((n.Bounds.X).ToString() + ", ");
                    tw.Write((n.Bounds.Y).ToString() + ", ");
                    tw.Write((n.Bounds.Width).ToString() + ", ");
                    tw.WriteLine((n.Bounds.Height).ToString());
                }
            }
            ++atlasCount;
        }
        tw.Close();
        tw = new StreamWriter(prefix + ".log");
        tw.WriteLine("--- LOG -------------------------------------------");
        tw.WriteLine(Log.ToString());
        tw.WriteLine("--- ERROR -----------------------------------------");
        tw.WriteLine(Error.ToString());
        tw.Close();
    }

    private void ScanForTextures(string _Path, string _Wildcard)
    {
        DirectoryInfo di = new DirectoryInfo(_Path);
        FileInfo[] files = di.GetFiles(_Wildcard, SearchOption.AllDirectories);
        foreach (FileInfo fi in files)
        {
            Image img = Image.FromFile(fi.FullName);
            if (img != null)
            {
                if (img.Width <= AtlasSize && img.Height <= AtlasSize)
                {
                    TextureInfo ti = new TextureInfo();

                    ti.Source = fi.FullName;
                    ti.Width = img.Width;
                    ti.Height = img.Height;

                    SourceTextures.Add(ti);

                    Log.WriteLine("Added " + fi.FullName);
                }
                else
                {
                    Error.WriteLine(fi.FullName + " is too large to fix in the atlas. Skipping!");
                }
            }
        }
    }

    private void HorizontalSplit(Node _ToSplit, int _Width, int _Height, List<Node> _List)
    {
        Node n1 = new Node();
        n1.Bounds.X = _ToSplit.Bounds.X + _Width + Padding;
        n1.Bounds.Y = _ToSplit.Bounds.Y;
        n1.Bounds.Width = _ToSplit.Bounds.Width - _Width - Padding;
        n1.Bounds.Height = _Height;
        n1.SplitType = SplitType.Vertical;
        Node n2 = new Node();
        n2.Bounds.X = _ToSplit.Bounds.X;
        n2.Bounds.Y = _ToSplit.Bounds.Y + _Height + Padding;
        n2.Bounds.Width = _ToSplit.Bounds.Width;
        n2.Bounds.Height = _ToSplit.Bounds.Height - _Height - Padding;
        n2.SplitType = SplitType.Horizontal;
        if (n1.Bounds.Width > 0 && n1.Bounds.Height > 0)
            _List.Add(n1);
        if (n2.Bounds.Width > 0 && n2.Bounds.Height > 0)
            _List.Add(n2);
    }

    private void VerticalSplit(Node _ToSplit, int _Width, int _Height, List<Node> _List)
    {
        Node n1 = new Node();
        n1.Bounds.X = _ToSplit.Bounds.X + _Width + Padding;
        n1.Bounds.Y = _ToSplit.Bounds.Y;
        n1.Bounds.Width = _ToSplit.Bounds.Width - _Width - Padding;
        n1.Bounds.Height = _ToSplit.Bounds.Height;
        n1.SplitType = SplitType.Vertical;
        Node n2 = new Node();
        n2.Bounds.X = _ToSplit.Bounds.X;
        n2.Bounds.Y = _ToSplit.Bounds.Y + _Height + Padding;
        n2.Bounds.Width = _Width;
        n2.Bounds.Height = _ToSplit.Bounds.Height - _Height - Padding;
        n2.SplitType = SplitType.Horizontal;
        if (n1.Bounds.Width > 0 && n1.Bounds.Height > 0)
            _List.Add(n1);
        if (n2.Bounds.Width > 0 && n2.Bounds.Height > 0)
            _List.Add(n2);
    }

    private TextureInfo FindBestFitForNode(Node _Node, List<TextureInfo> _Textures)
    {
        TextureInfo bestFit = null;
        float nodeArea = _Node.Bounds.Width * _Node.Bounds.Height;
        float maxCriteria = 0.0f;
        foreach (TextureInfo ti in _Textures)
        {
            switch (FitHeuristic)
            {
                // Max of Width and Height ratios
                case BestFitHeuristic.MaxOneAxis:
                    if (ti.Width <= _Node.Bounds.Width && ti.Height <= _Node.Bounds.Height)
                    {
                        float wRatio = (float)ti.Width / (float)_Node.Bounds.Width;
                        float hRatio = (float)ti.Height / (float)_Node.Bounds.Height;
                        float ratio = wRatio > hRatio ? wRatio : hRatio;
                        if (ratio > maxCriteria)
                        {
                            maxCriteria = ratio;
                            bestFit = ti;
                        }
                    }
                    break;
                // Maximize Area coverage
                case BestFitHeuristic.Area:
                    if (ti.Width <= _Node.Bounds.Width && ti.Height <= _Node.Bounds.Height)
                    {
                        float textureArea = ti.Width * ti.Height;
                        float coverage = textureArea / nodeArea;
                        if (coverage > maxCriteria)
                        {
                            maxCriteria = coverage;
                            bestFit = ti;
                        }
                    }
                    break;
            }
        }
        return bestFit;
    }

    private List<TextureInfo> LayoutAtlas(List<TextureInfo> _Textures, Atlas _Atlas)
    {
        List<Node> freeList = new List<Node>();
        List<TextureInfo> textures = new List<TextureInfo>();
        _Atlas.Nodes = new List<Node>();
        textures = _Textures.ToList();
        Node root = new Node();
        root.Bounds.Size = new Size(_Atlas.Width, _Atlas.Height);
        root.SplitType = SplitType.Horizontal;
        freeList.Add(root);
        while (freeList.Count > 0 && textures.Count > 0)
        {
            Node node = freeList[0];
            freeList.RemoveAt(0);
            TextureInfo bestFit = FindBestFitForNode(node, textures);
            if (bestFit != null)
            {
                if (node.SplitType == SplitType.Horizontal)
                {
                    HorizontalSplit(node, bestFit.Width, bestFit.Height, freeList);
                }
                else
                {
                    VerticalSplit(node, bestFit.Width, bestFit.Height, freeList);
                }
                node.Texture = bestFit;
                node.Bounds.Width = bestFit.Width;
                node.Bounds.Height = bestFit.Height;
                textures.Remove(bestFit);
            }
            _Atlas.Nodes.Add(node);
        }
        return textures;
    }

    private Image CreateAtlasImage(Atlas _Atlas)
    {
        Image img = new Bitmap(_Atlas.Width, _Atlas.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        Graphics g = Graphics.FromImage(img);
        foreach (Node n in _Atlas.Nodes)
        {
            if (n.Texture != null)
            {
                Image sourceImg = Image.FromFile(n.Texture.Source);
                g.DrawImage(sourceImg, n.Bounds);
            }
        }
        // DPI FIX START
        Bitmap ResolutionFix = new Bitmap(img);
        ResolutionFix.SetResolution(96.0F, 96.0F);
        Image img2 = ResolutionFix;
        return img2;
        // DPI FIX END
    }
}