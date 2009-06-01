#ifdef _DLL_ERASER
#define ERASER_EXPORT __declspec(dllexport) LONG __stdcall
#define ERASER_API __declspec(dllexport)
#else
#define ERASER_EXPORT __declspec(dllimport) LONG __stdcall
#define ERASER_API __declspec(dllimport)
#endif