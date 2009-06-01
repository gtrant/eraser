----------------------------------------------------------------
Eraser 5.8 Source Code
----------------------------------------------------------------

CONTENTS
1. LEGAL
   1.1. Copyright
   1.2. License
   1.3. Digital signature
2. RELEASE NOTES
   2.1. Changes
   2.2. Stability
   2.3. TODO
3. CODE STRUCTURE
   3.1. Eraser
   3.2. EraserDll
   3.3. Erasext
   3.4. Launcher
   3.5. EraserD
   3.6. Verify
4. COMPILING
   4.1. Tools
   4.2. Compiling
5. CONTACT
   5.1. Web site

----------------------------------------------------------------
1. LEGAL

   1.1. Copyright
        Eraser Copyright © 1997-2006 by Sami Tolvanen, Garrett Trant
        All rights reserved.

   1.2. License

        Eraser source code is published under GNU General Public
        License (see COPYING.txt).

   1.3. Digital signature

        This archive is signed with Pretty Good Privacy (PGP)
        (public key available at web site) for verifying the
        authenticity. The signature file should be included
        with the source distribution.

        If you did not receive a separate digital signature file
        with this source archive, download the official source
        distribution at the home page (see part 4).

----------------------------------------------------------------
2. RELEASE NOTES

   2.2. Changes (since 5.2.1)

        Eraser

          Changes
            1 Now showing a wait cursor when searching for
              files to erase on the On-Demand view.
          Bug fixes
            1 If there was nothing to erase, the On-Demand view
              would not show an error message.

        EraserDll

          UI changes
            1 Repositioned text fields on Custom Method Editor
              window.
            2 If selected method has only one pass, the Erasing
              Preferences window says pass instead of passes.
            3 Rewrote some of the error messages, maybe they are
              less confusing now (?).
          Internal changes
            1 Using AfxBeginThread instead of _beginthread in
              random number generator.


        Erasext

          NONE


        Launcher

          Bug fixes
            1 Now clears Recycle Bin even if it only has empty
              folders. Problems with slowPoll though.


        EraserD

          NONE


        Verify

          NONE


   2.3. Stability

        This release is stable.

   2.4. TODO

        This is the last release of Eraser.


----------------------------------------------------------------
3. CODE STRUCTURE

   3.1. Eraser

        The main user interface. On-Demand, Scheduler and Eraser
        Explorer.


        Components written by others or originally based on
        their work:

          AUTHOR            COMPONENT           CHANGES

         From http://www.codeguru.com/:

          Iuri Apollonio    GfxGroupEdit        No
          Iuri Apollonio    GfxSplitterWnd      No
          Iuri Apollonio    GfxOutbarCtrl       Yes
          Iuri Apollonio    GfxPopupMenu        Yes
          Maarten Hoeben    FlatHeaderCtrl      Yes
          Steve Bryndin     InfoBar             Yes
          Keith Rule        MemDC               No
          Selom Ofori       ShellPidl           Major
          Selom Ofori       ShellTree           Major
          Chris Maunder     HyperLink           No
          Chris Maunder     ProgressBar         No
          Chris Maunder     SystemTray          No
          (unknown)         MaskEd              Yes
          (unknown)         SeException         No
          (unknown)         TimeOutMessageBox   Yes

         From http://msdn.microsoft.com/:

          Paul DiLascia     FileDialogEx        No

        See the source files for more information.


   3.2. EraserDll

        The Eraser Library. Contains all of the overwriting
        code. See "EraserDll.h" for usage information.


        Acknowledgements:

        Handling of compressed files on NTFS partitions is based
        on the work of Mark Russinovich. For more information,
        see http://www.sysinternals.com/.

        See the source files for more information.


   3.3. Erasext

        The shell extension.


   3.4. Launcher

        The command line user interface.


   3.5. EraserD

        File eraser for DOS.


   3.6. Verify

        For verifying that overwriting works.

----------------------------------------------------------------
4. COMPILING

   4.1. Tools

        The binary distribution of Eraser was compiled using
        Microsoft Visual .NET with the Oct 2002 version
        of MS Platform SDK installed. The help file was compiled
        with Microsoft Help Workshop 4.03.0002.

        The DOS version of Eraser (EraserD) was compiled with
        Microsoft C/C++ Compiler 8.00c that shipped with MS
        Visual C++ 1.52c.

        The setup program is not included in the archive and
        is not available for download.

   4.2. Compiling

        Open "Eraser.sln" with Visual C++ .NET, select the active
        configuration to be "Eraser - Win32 Release" and
        compile. You should be able to fix possible compilation
        errors yourself. To build EraserD, set the environment
        and run "Build.bat".

        If the archive did not come with digital signature or
        you did not receive it from trusted source, do not run
        the compiled program without inspecting the source code
        first.

        NOTE! If you have problems compiling "Random.cpp",
        install MS Platform SDK for updated <wincrypt.h>.

----------------------------------------------------------------
5. CONTACT

   5.1. Web site

        The latest version of the source can be accessed through
        Subversion

        https://eraser.svn.sourceforge.net/svnroot/eraser

----------------------------------------------------------------