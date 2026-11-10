Attribute VB_Name = "Func_Reporder"
Option Compare Database

Dim rst As Recordset
Dim mystr As String, orderstr As String
Dim ORDFLD As String, Ord As String

Public Function Reporder(mytype As Byte) As String
        Set rst = CurrentDb.OpenRecordset("SELECT * FROM Sorts WHERE Ittype= " & mytype, dbOpenDynaset, dbSeeChanges)
        Dim Trecs, CONT As Integer
        With rst
            If .RecordCount = 0 Then
                orderstr = ""
                Exit Function
            Else
                .MoveLast
                .MoveFirst
                Trecs = .RecordCount
                CONT = 1
                orderstr = ""
            End If
            
            While Not .EOF
                ORDFLD = !Sfld
                If CONT < Trecs Then
                    Ord = IIf(IsNull(!Ssort), "DESC", IIf(!Ssort = 1, "", "DESC")) & ","
                Else
                    Ord = IIf(IsNull(!Ssort), "DESC", IIf(!Ssort = 1, "", "DESC"))
                End If
                orderstr = orderstr & ORDFLD & " " & Ord
                CONT = CONT + 1
                .MoveNext
            Wend
        End With
 Reporder = orderstr
End Function
