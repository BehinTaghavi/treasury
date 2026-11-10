Attribute VB_Name = "Sub_Changeinmain"
Option Compare Database
Dim rst As Recordset
Dim mystr As String

Public Sub Changeinmain(mygid As Long, mygdate As String, mybacc As String)
        mystr = "Select * from Subs Where Gid=" & mygid
        Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
        With rst
            If .RecordCount = 0 Then
                Exit Sub
            Else
                .MoveLast
                .MoveFirst
            End If
            
            While Not .EOF
                .Edit
                 !Sdate = mygdate
                 !Baccno = mybacc
                .Update
                .MoveNext
            Wend
        .Close
        End With
End Sub


