using Godot.Collections;
using Renderite.Shared;

namespace Renderite.Godot.Source;

public class MaterialManager
{
	//we need to handle materials differently to how frooxengine/unity would due to edge cases, such as stencil shaders (needs new shaders for stencil modes and channels) and material property blocks (needs new materials to emulate material property blocks)
	public Dictionary<int, string> ShaderMap = new();
	public void Handle(ShaderUpload command)
	{
		//TODO: i have no idea how process-safe their strings are
		ShaderMap[command.assetId] = new string(command.file);
	}
	public void Handle(ShaderUnload command) => ShaderMap.Remove(command.assetId);
}
