Attribute VB_Name = "Sub_delpovouch"
Option Compare Database

Public Sub Delpovouch(Myid As Long, mys As Byte)
    DoCmd.SetWarnings False
    Dim S As String
    
    If mys = 1 Then
        S = "Delete * from PoVouch Where Checkid= " & Myid
    Else
        S = "Delete * from PoVouch Where Sid= " & Myid
    End If
    DoCmd.RunSQL S
            
    DoCmd.SetWarnings True
End Sub
