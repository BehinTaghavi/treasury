Attribute VB_Name = "Sub_delchecks"
Option Compare Database
Dim rst As Recordset
Dim mystr As String

Public Sub Delacheck(Mycheckid As Long)
        mystr = "Select * from Subs Where Checkid=" & Mycheckid
        Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
        With rst
            If .RecordCount = 0 Then
                GoTo Line10
            Else
                .MoveLast
                .MoveFirst
            End If
    
            While Not .EOF
                .Delete
                .MoveNext
            Wend
        .Close
        End With
Line10:
        mystr = "Select * from Voucher_row Where Checkid=" & Mycheckid
        Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
        With rst
            If .RecordCount = 0 Then
                Exit Sub
            Else
                .MoveLast
                .MoveFirst
            End If
    
            While Not .EOF
                .Delete
                .MoveNext
            Wend
        .Close
        End With
End Sub


