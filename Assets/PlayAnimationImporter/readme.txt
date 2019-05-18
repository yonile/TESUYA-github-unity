PLAY Animation Importer for Unity ======================================

Thank you for downloading our plugin, PLAY Animation Importer for Unity!
Please read the text below or visit the official website
to make maximal use of features.

Plugin official website: http://doga.jp/PLAY/unity-plugin/

* Release notes ========================================================

- Dec.23, 2016: Changed texture file format in asset directory from
  "*.asset" to "*.png".
- Aug.31, 2015: Supported Unity5. (both 32bit and 64bit editors)
- Nov.02, 2014: Combined meshes with same materials to minimize draw call.
  Fixed failure of applying L2/Totekan/PLAY palette to some parts.
  (for example: tire parts)
- Oct.23, 2014: Fixed dual color texture was not imported. Fixed detect
  failure of DOGA-L3 for Professional license. Fixed license relations.
  (L3/L3Pro should include L2)
- Oct.20, 2014: Fixed error and corrupt import window when no registered
  DoGA products for dropped files was found. Fixed textures were not
  attached, and incorrect textures were attached. Fixed import failure
  for L3P files with original texture image.
- Oct.15, 2014: First release.

* About PLAY Animation Importer for Unity ==============================

This is an import plugin to convert parts assembler files created by
PLAY Animation, TotekanCG, DOGA-E1, DOGA-L3, DOGA-L2, DOGA-L1 into Unity,
the world's most widely used game engine. You can bring your original
objects into Unity and use it in your original games.

* About DoGA products ==================================================

DoGA products (PLAY Animation, TotekanCG, etc.) are the softwares
developed for entirely novice users to enable to learn 3D CGI. They can
quickly create cool mechanical objects or building structures by
repeating to attach prepared parts like plastic models. If you want to
show many 3D mechanical objects or buildings in your games on Unity,
let's create objects with these softwares and import them to Unity! We
will recommend "TotekanCG" for Japanese speakers, "PLAY Animation" for
English speakers.

PLAY Animation official website: http://doga.jp/PLAY/

* Supprted file formats ================================================

Parts assembler files (*.e1p, *.l3p, *.l2p, and *.fsc).
Connection files (*.l3c, *.l2c) and motion files (*.e1m, *.l3m, *.l2m,
*.frm) are not supported.

* How to use ===========================================================

(This section with screenshots are available at the official site:
http://doga.jp/PLAY/unity-plugin-manual/)

1. Dispalying import window
Select "Window" menu, and then "PLAY Animation (TotekanCG) Importer",
then import window will be displayed.

2. Parameters
- "Installed DoGA Products" shows the installed and registered DoGA 
softwares. (If your DoGA products are installed but not registered, they
will not be shown there. Only L1 will be displayed without registration
because it is not required.)
- You can set scale parameter of objects to import. Default value is 
(0.005, 0.005, 0.005). Modified value will be saved after you close the
window. Press "Reset" to set default one.

3. Drag & drop Parts of assembler files
Drag & drop Parts assembler files (*.e1p, *.l3p, *.l2p, and *.fsc) into
the bottom area of the importer dialog, and a new import task will be 
added. Or drag & drop them into Assets directory in Project browser will
bring the same result.

4. Select product (for *.e1p files only)
Since *.e1p files are used at 3 products (DOGA-E1, TotekanCG, and PLAY
Animation), which contains differrent sets of parts respectively, you
need to select one of them to retrieve parts. For DOGA-L series, you don't
need to du anything because only 1 product can be selected.

5. Select color (for *.fsc files only)
For DOGA-L1 (*.fsc) files, select color before import.

6. Start importing
Press "Import All" to start import. It might take a long time, because
it searches and read all dependent parts (suf and atr files) from DoGA
product directory. And be sure that you cannot cancel import until it is
completed.

7. Completed importing
When import is finished, a new GameObject is created in the scene
hierarchy, and the imported objects will be displayed at 3D view. And
a folder that contains objects, material and textures and a prefab file
will be created below "Assets" filder in the project browser.

* Easter egg? ==========================================================

This plugin also supports importing Yanagisawa pic file format, which is
widely used for the users X68000 personal workstation in 1990s. Just
drag and drop pic files into "Assets" folder, they are imported as 
textures. Only 15bit(32768) colors and 16bit(65536) colors are supported.

* Contacts =============================================================

DoGA Corporation
Website: http://doga.jp/PLAY/
E-mail: e-support@doga.jp

