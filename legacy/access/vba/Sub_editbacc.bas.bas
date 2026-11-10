Attribute VB_Name = "Sub_editbacc"
Option Compare Database
Dim rst As Recordset
Dim mystr As String

Public Sub Editbacc(oldacc As String, newacc As String)

        mystr = "Select * from Bundle Where Baccno=" & "'" & CStr(oldacc) & "'"
        Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
        With rst
            If .RecordCount = 0 Then
                GoTo Line10
            Else
                .MoveLast
                .MoveFirst
            End If
    
            While Not .EOF
                .Edit
                    !Baccno = newacc
                .Update
                .MoveNext
            Wend
        .Close
        End With

Line10:
        mystr = "Select * from Check Where Baccno=" & "'" & CStr(oldacc) & "' Or Vagno=" & "'" & CStr(oldacc) & "' or transaccid =" & "'" & CStr(oldacc) & "'"
        Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
        With rst
            If .RecordCount = 0 Then
                GoTo Line20
            Else
                .MoveLast
                .MoveFirst
            End If
    
            While Not .EOF
                .Edit
                    If !Baccno = oldacc Then
                        !Baccno = newacc
                    End If
                    If !Vagno = oldacc Then
                        !Vagno = newacc
                    End If
                    If !Transaccid = oldacc Then
                        !Transaccid = newacc
                    End If
                .Update
                .MoveNext
            Wend
        .Close
        End With
        
        
Line20:
        mystr = "Select * from Mains Where Baccno=" & "'" & CStr(oldacc) & "'"
        Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
        With rst
            If .RecordCount = 0 Then
                GoTo Line30
            Else
                .MoveLast
                .MoveFirst
            End If
    
            While Not .EOF
                .Edit
                    !Baccno = newacc
                .Update
                .MoveNext
            Wend
        .Close
        End With

Line30:
        mystr = "Select * from Subs Where Baccno=" & "'" & CStr(oldacc) & "'"
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
                    !Baccno = newacc
                .Update
                .MoveNext
            Wend
        .Close
        End With
End Sub
