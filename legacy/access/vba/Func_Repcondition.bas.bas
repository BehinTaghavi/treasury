Attribute VB_Name = "Func_Repcondition"
Option Compare Database

Dim rst As Recordset
Dim mystr As String, originrstr As String
Dim ORDFLD As String, Ord As String

Public Function Repcondition(mytype As Byte) As String
    originstr = ""
    Set rst = CurrentDb.OpenRecordset("SELECT * FROM Shart WHERE Ittype= " & mytype, dbOpenDynaset, dbSeeChanges)
        With rst
            If .RecordCount = 0 Then
                Exit Function
            Else
                .MoveLast
                .MoveFirst
            End If
            Dim myfld As String, Myshart As String, C1 As String, C2 As String, ISBET As String, ANDSTR As String, MYCOUNTER As Integer
            Dim RABET As String
            Dim A As String, B As String
            MYCOUNTER = 0
            While Not .EOF
                Myshart = !Fsymbol
                C1 = IIf(IsNull(!D1), 0, !D1)
                C2 = IIf(IsNull(!D2), 0, !D2)
                A = BSPACE(C1)

                B = "'*" & Trim(A) & "*'"
                myfld = IIf(!Fldtype <> 1, !Fldname, !Fldname & B)
                RABET = IIf(!Andor = 0, "AND ", "OR ")
                
                If !Fsymbol = "BETWEEN" Then
                  Select Case !Fldtype
                    Case 2
                        ISBET = RABET & myfld & " " & Myshart & " " & C1 & " AND " & C2
                    Case Else
                        ISBET = RABET & myfld & " " & Myshart & " '" & C1 & "' " & "AND '" & C2 & "' "
                  End Select
                Else
                    Select Case !Fldtype
                        Case 0
                            ISBET = RABET & myfld & " " & Myshart & " '" & C1 & "' "
                        Case 1
                            ISBET = RABET & myfld
                        Case 2
                            ISBET = RABET & myfld & " " & Myshart & " " & C1
                    End Select
                    
                End If
                originstr = originstr & ISBET
                MYCOUNTER = MYCOUNTER + 1
                .MoveNext
            Wend
        End With
 Repcondition = originstr
End Function
