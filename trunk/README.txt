----------------------------------------------------------------
NOTE: This version uses a new installer system. You MUST remove
ALL EARLIER VERSIONS before installing this Beta. When installing
an earlier stable build, UNINSTALL the Beta first.
----------------------------------------------------------------
----------------------------------------------------------------
Eraser 5.87 beta 4: Latest changes
----------------------------------------------------------------
- Fixed error message when user deletes files and cancels in the
  Eraser Explorer.
- Deleting tasks now require confirmation (as per Windows)
- Fixed a hang when users expanded the Network bit of the file
  data selection dialog (now only affects Verifier)
- Run the post-task operation when it is complete.
- Replaced the custom file/folder selection dialog with the Windows
  built-in ones.
- The "no files to erase" error no longer appears for file/folder
  erasures.
- Fixed the Hotkey selection dialog in General Preferences
- System passwords must be specified for it to be active. Throw
  an error if the user wanted a password but didn't set one
- x64 compatibility fixes (NTAPI call)
- The scheduler log size must be positive, the dialog enforces it
  now.
- Allow users to tell Eraser to remember the resolve locked files
  setting for the current erase. Like a No to All or Yes to All
  button.
- Implemented the deletion of run-at-reboot scheduled tasks.
- Compile everything using VS 2008.

Beta 3 Changes
----------------------------------------------------------------
- Fixed command line error when selecting subfolders to be erased
  with the parent folder
- Fixed a few issues with EraserL creating a registry key after an
  erase
- Fixed the weird bug of right-clicking the various erase methods
  and Eraser hangs. Hopefully this resolves the problem of the
  file/folder selection dialog blinking and resetting the selections.
- Implemented NTFS EFS file erasure (EXPERIMENTAL!)
- Further complicate forensics: Set MACE to invalid values (NTFS only)
- Fixed #6: Incorrect 'When Finished' option
- Implemented #37: Eraser should not allow system to hibernate or
  standby when running
- Fixed #36: Eraser overriding Windows hotkeys in context menu
- Fixed #48: Inaccurate determination of process elevation

Beta 2 Changes
----------------------------------------------------------------
- Do not create "New Eraser Document" in the New context menu of
  Explorer when using a Portable version

Beta 1 changes
----------------------------------------------------------------
- Schedlog.txt will always be kept in the local Application Data
  folder.
- Fixed shutdown issues after erase for NT-based computers.
- Fixed erasure of recycle bin contents when the erase was cancelled.
- Fixed Windows 98 compatibility.
- Fixed error checking when querying for elevation.
- Renamed Verify.exe to Erschk.exe. Resolved #418558.
- Fixed the VC2005 redistributable being extracted over each other.
  when both x86 and x64 builds are selected for install.
- Fixed command line error when -silent was passed to EraserL.

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

        Eraser Copyright © 2007-2008 by the Eraser Project. All rights
        reserved.
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

        The setup program is signed with Pretty Good Privacy
        (PGP) (public key available at web site) for verifying
        the authenticity. The signature file should be included
        with the archive. This file has the extension '.asc'.

        If you did not receive a separate digital signature file
        with this archive, download the official binary
        distribution at the home page (see section 5.1).

----------------------------------------------------------------
2. SYSTEM REQUIREMENTS

   This version of Eraser runs on Windows 95, 98, ME, NT 4.0,
   2000, XP and all editions of Vista. Windows XP x64 and all
   64-bit SKUs of Vista can also be used (using the 64-bit
   version)

----------------------------------------------------------------
3. HOW TO INSTALL & UNINSTALL

   3.1. Before installing

        Be sure to uninstall any previous version of Eraser
        before installing. 

        If the setup program asks you to reboot Windows after
        installation, you must do that before running Eraser.

   3.2. Installing

        EraserSetup.exe is a self-extracting executable containing the
        program files.

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
   
   The new maintainer of this product is the Eraser Project.
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
