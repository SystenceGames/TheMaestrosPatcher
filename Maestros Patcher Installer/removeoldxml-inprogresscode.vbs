Dim fso, Folder2Delete
Folder2Delete = Session.Property("CustomActionData")
Set fso = CreateObject("Scripting.FileSystemObject")
If fso.FolderExists(Folder2Delete) Then
  fso.DeleteFolder(Folder2Delete)
End If
'http://stackoverflow.com/questions/7261981/installshield-removing-a-file-using-vbscript-customaction-fails-when-theres-n