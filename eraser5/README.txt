-----------------------------------------------------------------
NOTE: You MUST remove ALL VERSIONS (including BETAS) earlier than
      5.8.7 before installing this version.
=================================================================
In this Release: Eraser 5.8.8
-----------------------------------------------------------------
- Do not erase sparse, compressed or encrypted files when FL2KB
  erasure is selected to prevent disk corruption.
- Fix Win32 Eraser builds to be truly Unicode builds.
- A few 64-bit fixes.
- Ensure that Eraser uses the latest runtimes packaged with the
  installer. This should fix hangs when using the Context menu.
- Fixed the About dialog after migrating to Unicode
- Sign all binaries packaged with the installer
- Allow erasing UNC paths
- Fixed memory exhaustion when doing a free space erase for FAT
  drives
- Migrate non-Unicode custom erasure methods to the Unicode methods
  so that old custom erasure methods are preserved upon upgrade
- Ensure that Eraser is not running when the Eraser setup is run

=================================================================
CONTENTS
1. LEGAL
   1.1. Copyright
   1.2. License agreement and Disclaimer
   1.3. Digital signature
2. SYSTEM REQUIREMENTS
   2.1. Libraries
3. HOW TO INSTALL & UNINSTALL
   3.1. Before installing
   3.2. Installing
   3.3. Help?
   3.4. Uninstalling
4. DESCRIPTION
5. AUTHOR
   5.1. Web site

----------------------------------------------------------------
1. LEGAL

   1.1. Copyright

        Eraser Copyright © 2007-2009 by The Eraser Project. All
        rights reserved.
        Eraser Copyright © 2002-2006 by Garrett Trant. All rights
        reserved.
        Eraser Copyright © 1997-2002 by Sami Tolvanen. All rights
        reserved.

        By using this software you accept all the terms and
        conditions of the license agreement and disclaimer
        below.

        All registered and unregistered trademarks mentioned in
        this document are the property of their respective
        holders.

   1.2. License agreement and Disclaimer

        This program is free software; you can redistribute it
        and/or modify it under the terms of the GNU General
        Public License as published by the Free Software
        Foundation; either version 2 of the License, or (at your
        option) any later version.

	    This program is distributed in the hope that it will be
	    useful, but WITHOUT ANY WARRANTY; without even the
	    implied warranty of MERCHANTABILITY or FITNESS FOR A
	    PARTICULAR PURPOSE.  See the GNU General Public License
	    for more details.

      1.2.1 GNU General Public License
	        The GNU General Public License (GNU GPL) is attached
            in the file COPYING.txt

   1.3. Digital signature

        The setup program is signed with an Authenticode signature
        for authenticity verification.

----------------------------------------------------------------
2. SYSTEM REQUIREMENTS

   This version of Eraser runs on Windows 2000, XP and all editions
   of Vista. Windows XP x64 and all 64-bit SKUs of Vista can also
   be used (the 64-bit version will be chosen automatically).
   Windows 7 installs may work but Eraser 5 is not supported
   under that platform.

----------------------------------------------------------------
3. HOW TO INSTALL & UNINSTALL

   3.1. Before installing

        Be sure to uninstall any previous version of Eraser
        before installing. 

        If the setup program asks you to reboot Windows after
        installation, you must do that before running Eraser.

   3.2. Installing

        This setup file is a self-extracting executable containing
        the program files.

        Run EraserSetup.exe to install Eraser. The setup program will
        extract the files and copy them to a desired location.

   3.3. Help?

        After installing, you can open the help file (in the
        Eraser directory) by double-clicking the ERASER.HLP
        file.

        You can also get answers to some frequently asked
        questions and read a step-by-step installation
        instructions at Eraser home page (see section 5.1).

   3.4. Uninstalling

        You can uninstall Eraser normally via Control Panel,
        Add/Remove Programs. You should not remove the files
        manually because the settings in the registry would not
        be removed.

----------------------------------------------------------------
4. DESCRIPTION

   Eraser is an advanced security tool, which allows you to
   completely remove sensitive data from your hard drive by
   overwriting it several times with carefully selected
   patterns.

   You can drag and drop files and folders to the on-demand
   eraser, use the convenient Explorer shell extension or use
   the integrated scheduler to program overwriting of unused
   disk space or, for example, browser cache files to happen
   regularly, at night, during your lunch break, at weekends or
   whenever you like.

   The patterns used for overwriting are based on Peter
   Gutmann's paper "Secure Deletion of Data from Magnetic and
   Solid-State Memory" and they are selected to effectively
   remove the magnetic remnants from the hard disk making it
   impossible to recover the data.

   Other methods include the one defined in the National
   Industrial Security Program Operating Manual of the US
   Department of Defense and overwriting with pseudo-random data
   up to one hundred times.

----------------------------------------------------------------
5. AUTHOR
   
   The new maintainer of this product is The Eraser Project.
   For more information visit
   http://sourceforge.net/projects/eraser
  
   Sami Tolvanen, the original author of this software is a 
   student in the university of technology in Finland. For 
   more information, visit his home page at 
   http://www.tolvanen.com/sami/.

   5.1. Web site

        You can download the latest version of Eraser and the
        required libraries at the Eraser SourceForge page.

        http://sourceforge.net/project/showfiles.php?group_id=37015

----------------------------------------------------------------
