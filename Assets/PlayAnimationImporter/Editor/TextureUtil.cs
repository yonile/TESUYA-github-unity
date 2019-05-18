using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using TotekanReaderLib;
using YanagisawaPicLoaderLib;
using ArrayImageProcessingLib;

public class TextureUtil
{
	public static Texture2D loadAndBlendTextures(List<string> namesFileTexture)
	{
		if (namesFileTexture.Count == 0) return null;
		if (namesFileTexture.Count == 1) {
			return loadTexture(namesFileTexture[0]);
		}
		var textures = new List<Texture2D>();

		int width = int.MaxValue;
		int height = int.MaxValue;
		bool isTransparency = false;
		foreach (var nameFileTexture in namesFileTexture) {
			var texture = loadTextureInternal(nameFileTexture);

			if (width == int.MaxValue || height == int.MaxValue) {
				width = texture.width;
				height = texture.height;
			}
			if (texture.alphaIsTransparency) isTransparency = true;
			textures.Add(texture);
		}

		return blendTexture(textures, isTransparency);
	}

	public static Texture2D blendTexture(List<Texture2D> textures, bool isTransparency)
	{
		if (textures.Count == 0) return null;
		if (textures.Count == 1) return textures[0];

		int widthSum = textures[0].width;
		int heightSum = textures[0].height;

		const int numElems = 4;
		var pixelsSum = new byte[widthSum * heightSum * numElems];
		int indexTexture = 0;
		foreach (var texture in textures) {
			int width = texture.width;
			int height = texture.height;
			var bytesTexture = convertColor32ArrayToByteArray(texture.GetPixels32());
			ArrayImageProcessing.blendImageResized((0 < indexTexture) ? true : false,
				numElems, width, height, bytesTexture, widthSum, heightSum, pixelsSum);
			indexTexture ++;
		}

		var pixelsResult = convertByteArrayToColor32Array(pixelsSum);
		var textureResult = new Texture2D(widthSum, heightSum, TextureFormat.ARGB32, false);
		textureResult.SetPixels32(pixelsResult);
		textureResult.alphaIsTransparency = isTransparency;
		textureResult.Apply(true, false);

		return textureResult;				
	}

	public static Texture2D loadTexture(string nameAssetFullPath)
	{
		var texture = loadTextureInternal(nameAssetFullPath);
		texture.Apply(true, false);

		return texture;
	}

	private static Texture2D loadTextureInternal(string nameAssetFullPath)
	{
		byte[] bytes = null;
		try {
			bytes = File.ReadAllBytes(nameAssetFullPath);
		} catch (FileNotFoundException) {
			return null;
		}
		var nameExt = Path.GetExtension(nameAssetFullPath).ToLower();
		Texture2D texture = null;
		if (nameExt.EndsWith(".pic")) {
			texture = TextureUtil.loadTextureYanagisawaPic(bytes);
		} else {
			texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
			texture.LoadImage(bytes);
		}
		texture.alphaIsTransparency = TextureUtil.getIsTextureTransparent(texture) ? true : false;
		bytes = null;

		return texture;
	}

	public static Texture2D loadTextureYanagisawaPic(byte[] bytes)
	{
		var loader = new YanagisawaPicLoader(bytes);
		loader.extract();
		var texture = new Texture2D(loader.width, loader.height, TextureFormat.ARGB32, false, false);
		texture.LoadRawTextureData(loader.bytesImageExtracted);

		return texture;
	}

	public static bool getIsTextureTransparent(Texture2D texture)
	{
		var colors = texture.GetPixels32();
		int nums0 = 0;
		int nums255 = 0;
		int numsOther = 0;
		foreach (var color in colors) {
			if (color.a == 0) {
				nums0 ++;
			} else if (color.a == 255) {
				nums255 ++;
			} else {
				numsOther ++;
			}
		}
		return (numsOther > 0 || (nums0 > 0 && nums255 > 0)) ? true : false;
	}

	private static byte[] convertColor32ArrayToByteArray(Color32[] colors)
	{
		if (colors == null || colors.Length == 0)
			return null;

		int lengthOfColor32 = Marshal.SizeOf(typeof(Color32));
		int length = lengthOfColor32 * colors.Length;
		byte[] bytes = new byte[length];

		GCHandle handle = default(GCHandle);
		try {
			handle = GCHandle.Alloc(colors, GCHandleType.Pinned);
			IntPtr ptr = handle.AddrOfPinnedObject();
			Marshal.Copy(ptr, bytes, 0, length);
		} finally {
			if (handle != default(GCHandle))
				handle.Free();
		}

		return bytes;
	}

	private static Color32[] convertByteArrayToColor32Array(byte[] bytes)
	{
		if (bytes == null || bytes.Length == 0)
			return null;

		int length = bytes.Length;
		int lengthOfColor32 = Marshal.SizeOf(typeof(Color32));
		Color32[] colors = new Color32[length / lengthOfColor32];

		GCHandle handle = default(GCHandle);
		try {
			handle = GCHandle.Alloc(colors, GCHandleType.Pinned);
			IntPtr ptr = handle.AddrOfPinnedObject();
			Marshal.Copy(bytes, 0, ptr, length);
		} finally {
			if (handle != default(GCHandle))
				handle.Free();
		}

		return colors;
	}


}

