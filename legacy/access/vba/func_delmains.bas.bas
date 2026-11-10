Attribute VB_Name = "func_delmains"
Option Compare Database
Dim rst As Recordset
Dim mystr As String

Public Function Delmains(Myg As String, mygid As Long)
        mystr = "Select * from Check Where " & CStr(Myg) & " = " & mygid
        Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
        With rst
            If .RecordCount = 0 Then
                Delmains = 0
            Else
                .MoveLast
                .MoveFirst
                 Delmains = .RecordCount
            End If
        .Close
        End With
End Function


