

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 7.00.0500 */
/* at Fri Jan 02 21:19:52 2009
 */
/* Compiler settings for .\DllMain.idl:
    Oicf, W1, Zp8, env=Win32 (32b run)
    protocol : dce , ms_ext, c_ext, robust
    error checks: stub_data 
    VC __declspec() decoration level: 
         __declspec(uuid()), __declspec(selectany), __declspec(novtable)
         DECLSPEC_UUID(), MIDL_INTERFACE()
*/
//@@MIDL_FILE_HEADING(  )

#pragma warning( disable: 4049 )  /* more than 64k source lines */


/* verify that the <rpcndr.h> version is high enough to compile this file*/
#ifndef __REQUIRED_RPCNDR_H_VERSION__
#define __REQUIRED_RPCNDR_H_VERSION__ 475
#endif

#include "rpc.h"
#include "rpcndr.h"

#ifndef __RPCNDR_H_VERSION__
#error this stub requires an updated version of <rpcndr.h>
#endif // __RPCNDR_H_VERSION__


#ifndef __ShellExt_i_h__
#define __ShellExt_i_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __CtxMenu_FWD_DEFINED__
#define __CtxMenu_FWD_DEFINED__

#ifdef __cplusplus
typedef class CtxMenu CtxMenu;
#else
typedef struct CtxMenu CtxMenu;
#endif /* __cplusplus */

#endif 	/* __CtxMenu_FWD_DEFINED__ */


/* header files for imported files */
#include "oaidl.h"
#include "ocidl.h"

#ifdef __cplusplus
extern "C"{
#endif 



#ifndef __EraserShellExtLib_LIBRARY_DEFINED__
#define __EraserShellExtLib_LIBRARY_DEFINED__

/* library EraserShellExtLib */
/* [custom][helpstring][version][uuid] */ 


EXTERN_C const IID LIBID_EraserShellExtLib;

EXTERN_C const CLSID CLSID_CtxMenu;

#ifdef __cplusplus

class DECLSPEC_UUID("BC9B776A-90D7-4476-A791-79D835F30650")
CtxMenu;
#endif
#endif /* __EraserShellExtLib_LIBRARY_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


