DLLExprorter
============

Exprort methods to unmanaged code in C#

Usage:
	DLLExporter.exe -in:X:\library.dll [-out:X:\some_folder\library.dll] [-noclear]

Add to post-buid in your project for example:
	D:\DLLExprorter.exe -in:"$(TargetPath)" -out:"D:\OutputFolder\$(TargetFileName)"
