using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

public class AssetNameUtil
{
	public static bool getNameAssetUtil(string nameDirParent, string nameFileDirWithOutExt, string nameExt, out string nameFileDirResultWithOutExt,
						int numSearch)
	{
		var nameDirFirst = nameDirParent + Path.DirectorySeparatorChar + nameFileDirWithOutExt;
		var nameFileFirst = nameDirFirst + nameExt;
		if (!Directory.Exists(nameDirFirst) && !File.Exists(nameFileFirst)) {
			nameFileDirResultWithOutExt = nameFileDirWithOutExt;
			return true;
		}

		string[] namesFile = Directory.GetFiles(nameDirParent, "*" + nameExt, SearchOption.TopDirectoryOnly);
		var tableNamesFile = new HashSet<string>();
		foreach (var nameFile in namesFile) {
			tableNamesFile.Add(Path.GetFileNameWithoutExtension(nameFile));
		}
		string[] namesDir = Directory.GetDirectories(nameDirParent, "*.*", SearchOption.TopDirectoryOnly);
		var tableNamesDir = new HashSet<string>();
		foreach (var nameDir in namesDir) {
			tableNamesDir.Add(nameDir);
		}

		for (var index = 1; index < numSearch; index++) {
			var nameDirSearch = nameDirFirst + @"_" + index.ToString();
			var nameFileSearch = nameDirSearch + nameExt;
			if (!tableNamesDir.Contains(nameDirSearch) && !tableNamesFile.Contains(nameFileSearch)) {
				nameFileDirResultWithOutExt = nameFileDirWithOutExt + @"_" + index.ToString();
				return true;
			}
		}
			
		nameFileDirResultWithOutExt = @"";
		return false;
	}
}
