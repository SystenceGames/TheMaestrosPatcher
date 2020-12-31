Dim WSHSHELL, NEWDIR
Set WSHSHELL = CreateObject("WScript.Shell")
NEWDIR = Left(wscript.scriptFullName, InstrRev(wscript.scriptFullName, "\") )
WSHSHELL.currentdirectory = NEWDIR
WSHSHELL.run ("Icacls * /T /C /grant Everyone:F")