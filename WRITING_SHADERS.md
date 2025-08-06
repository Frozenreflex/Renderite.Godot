# Writing Shaders

These are some notes and things to keep in mind when converting shaders from Resonite's Unity-based format to Godot

## Resonite's Shader Files and File Format

Resonite distributes it's shaders through the cloud within specially formatted Zip archives. The URLs are static, and can be found within the ``OfficialAssets`` static class when decompiling ``FrooxEngine.dll``.

A copy of all of the shader sources can be obtained with something like this, with a project referencing ``FrooxEngine.dll``

```csharp
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using ZipFile = System.IO.Compression.ZipFile;

namespace Scratchboard;

public static class Program
{
    public static void Main()
    {
        const string downloadRoot = "/home/frozenreflex/Downloads/resoniteshaders"; //YOUR PATH HERE
        var properties = typeof(OfficialAssets.Shaders).GetProperties().Where(i => i.PropertyType == typeof(Uri)).ToArray();

        foreach (var property in properties)
        {
            var name = property.Name;
            if (property.GetValue(null) is not Uri value) continue;

            var hash = value.ToString().Replace("resdb:///", "").Replace(".unityshader", "");

            var filePath = Path.Combine(downloadRoot, name);
            Directory.CreateDirectory(filePath);
            var tempFilePath = Path.Combine(filePath, "tmp");
            
            if (File.Exists(Path.Combine(filePath, "metadata.json"))) continue;
            
            Console.WriteLine($"{name}: {hash}");

            while (true)
            {
                try
                {
                    using var client = new HttpClient();
                    using var s = client.GetStreamAsync($"https://assets.resonite.com/{hash}");

                    using (var fs = new FileStream(tempFilePath, FileMode.Create))
                    {
                        s.Wait();
                        Console.WriteLine(s.Status);
                        s.Result.CopyTo(fs);
                        using var zip = ZipFile.Open(tempFilePath, ZipArchiveMode.Read);
                        foreach (var entry in zip.Entries)
                        {
                            if (!entry.Name.Contains('.')) continue;

                            var newFilePath = Path.Combine(filePath, entry.Name);
                            using var file = File.Open(newFilePath, FileMode.Create);
                            using var es = entry.Open();

                            es.CopyTo(file);
                        }       
                    }
            
                    File.Delete(tempFilePath);
                    break;
                }
                catch
                {
                    Console.WriteLine("Retrying");
                    File.Delete(tempFilePath);
                }
            }
        }
    }
}
```
Do note that these files contain compiled Unity shaders, and can take up a decent amount of space while downloading (over 1GB). This script will delete the compiled variants, since those aren't useful for this project.

Each zip contains a ``metadata.json`` file describing the structure of the shader, any source files used to compile the shader, and the compiled Unity shader variants themselves. Again, we're ignoring the compiled variants since these aren't important.

An example ``metadata.json`` file, sourced from PBS Metallic, will be shown below.

```json
{
  "metadataVersion": 0,
  "assetIdenfitier": null,
  "origin": "2019.4.19f1",
  "sourceFile": {
    "filename": "PBSMetallic.shader",
    "hash": "l7mJJ-tb5w3jxrflBDYaY7aOlxY"
  },
  "includeFiles": [],
  "variantGroups": [
    [
      "_",
      "_NORMALMAP"
    ],
    [
      "_",
      "_ALPHATEST_ON",
      "_ALPHABLEND_ON",
      "_ALPHAPREMULTIPLY_ON"
    ],
    [
      "_",
      "_EMISSION"
    ],
    [
      "_",
      "_METALLICGLOSSMAP"
    ],
    [
      "_",
      "_DETAIL_MULX2"
    ],
    [
      "_",
      "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A"
    ],
    [
      "_",
      "_SPECULARHIGHLIGHTS_OFF"
    ],
    [
      "_",
      "_GLOSSYREFLECTIONS_OFF"
    ],
    [
      "_",
      "_PARALLAXMAP"
    ]
  ],
  "uniqueKeywords": [
    "_ALPHABLEND_ON",
    "_ALPHAPREMULTIPLY_ON",
    "_ALPHATEST_ON",
    "_DETAIL_MULX2",
    "_EMISSION",
    "_GLOSSYREFLECTIONS_OFF",
    "_METALLICGLOSSMAP",
    "_NORMALMAP",
    "_PARALLAXMAP",
    "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A",
    "_SPECULARHIGHLIGHTS_OFF"
  ]
}
```

``sourceFile`` contains information about the file. Of note, ``filename`` is what your shader should be named, with the extension replaced with ``.gdshader``. In some instances, this does not match the folder name the script provides.

``Keywords`` are essentially pragma ``#define`` values. If a keyword is enabled, it is equivalent to ``#define <KEYWORD>`` being placed at the top of the shader file. This is done at runtime, you do not need to define these values in your code (unless you are testing it in the editor).

``uniqueKeywords`` is the list of all Keywords used in ``variantGroups``. All Keywords must belong to one group and one group only.

``variantGroups`` are groups of mutually exclusive Keywords, with an underscore counted as a null, or no Keyword being defined. All keywords must belong to one. Keep in mind the mutual exclusivity, if ``_ALPHATEST_ON`` is enabled, then ``_ALPHABLEND_ON`` *can't* be enabled because they belong to the same group.

## Pragma Macros

We use a set of pragma macros to simplify porting certain shader behaviors. All macros are stored in ``res://Resources/ShaderIncs/``, but some noteworthy ones will be noted here.

### ResoniteIncludes

Contains some helper functions, some hlsl function renames and ports, and most importantly, an include for ``ResoniteTextureHelpers``

### ResoniteTextureHelpers

If a shader uses a texture of *any* kind, you will need to include this due to differences between how Unity and Godot handle texture sampling. This is included by default by ``ResoniteIncludes``.

To define a texture you use one of a set of macros:
```
ColorTexture2D
ColorTexture2DWhite
ColorTexture2DBlack
ColorTexture2DTransparent
MapTexture2D
MapTexture2DOne
MapTexture2DZero
```
This macro needs to be provided with the name of the texture, without the preceeding underscore (ie ``ColorTexture2D(MainTex)``), with no following semicolon.

The prefix determines the color sampling type of the texture, while the suffix determines the default value when no texture is provided.

``Color`` is used for anything color related.

``Map`` is used for everything else, including normal maps.

``White`` and ``One`` default to ``(1,1,1,1)``

``Black`` default to ``(0,0,0,1)``

``Transparent`` and ``Zero`` default to ``(0,0,0,0)``

I don't know what the macros without suffixes default to.


If a texture contains Scale and Translation data (``ST``), use the ``STData`` macro in the same way as the texture definition (``STData(MainTex)``).

To sample a texture, use ``SampleTexture(T, uv)``, where ``T`` is the name of the texture (``MainTex``), and ``uv`` is your ``vec2`` UV map, such as ``UV``.

To sample a texture using it's corresponding ST data, use ``SampleSTTexture(T, uv)`` in the same manner.

To sample a texture using a different texture's ST data, such as a normal map using an albedo texture's ST, use ``SampleSTTextureData(T, data, uv)``, where ``data`` is the name of the texture with ST data.

### ShaderLab command emulation

Some ShaderLab commands aren't straightforward to emulate, so if one of these parameters or code blocks is used in a Unity shader, use these includes instead.

| Parameter                      | Code Block                      | Include            |
|--------------------------------|---------------------------------|--------------------|
| ``_SrcBlend`` or ``_DstBlend`` | ``Blend[_SrcBlend][_DstBlend]`` | ResoniteBlendMode  |
| ``_Cull``                      | ``Cull[_Cull]``                 | ResoniteCullMode   |
| ``_ZTest``                     | ``ZTest[_ZTest]``               | ResoniteZTestMode  |
| ``_ZWrite``                    | ``ZWrite[_ZWrite]``             | ResoniteZWriteMode |

#### ColorMask

As of now, this is unimplemented.

### Rect Clipping

UI and a few other types of shaders support rect clipping. A complete implementation can be found inside of ``UI_Unlit``.

To implement this, first, add a ``vec4`` uniform, in most cases it will be called ``_Rect``, but it may not always be.

Underneath this uniform, add ``DefineRectClip``, no semicolon.

If the shader does not have a vertex function, add one, and put ``StartRectClip`` in it, again no semicolon.

Finally, within the fragment function, at the very start, call ``DoRectClip(rect);``, where ``rect`` is the name of our uniform from before.