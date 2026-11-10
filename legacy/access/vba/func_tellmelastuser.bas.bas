Attribute VB_Name = "func_tellmelastuser"
Option Compare Database
Option Explicit

Public Function tellmelastuser(Myrow As Byte, mytype As Integer) As String
    Dim dbs As Database
    Dim rst, rst1 As Recordset
    Dim mystr As String, myfldname As String, mytblname As String, gennumber, basenumber As Long
    Set dbs = CurrentDb
'**************
    Set dbs = CurrentDb
    
    mystr = "select * from sys_fldlist where rowid=" & Myrow
    Set rst = dbs.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
    
    With rst
        If .RecordCount = 0 Then
            MsgBox "مجوز استفاده از سيستم وجود ندارد"
            Exit Function
        Else

            myfldname = !Fldname
            mytblname = !TBLNAME
        End If
    .Close
    End With
            mystr = "select max(Gno) as mymax from Mains Where Gcode=" & mytype & " and sal=" & [Forms]![Sal]![Tsal]
    Set rst = dbs.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
    
    With rst
        If IsNull(!mymax) Then
            Set rst1 = dbs.OpenRecordset("Globals", dbOpenDynaset, dbSeeChanges)
            With rst1
                Select Case mytype
                    Case 1
                            basenumber = !Pono
                    Case 102
                            basenumber = !l_order
                    Case 103
                            basenumber = !l_deliv
                    Case 104
                    Case 105
                            basenumber = !l_inv
                    Case 106
                            basenumber = !l_back
                    Case 107
                            basenumber = !l_exit
                    Case 108
                            basenumber = !l_basket
                    Case 109
                            basenumber = !l_contract
                    Case 111
                            basenumber = !l_plist
                    Case 113
                            basenumber = !l_quota
                    End Select
            .Close
            End With
            tellmelastuser = basenumber + 1
            
        Else
            tellmelastuser = !mymax + 1
        End If
    .Close
    End With
    
'************
    
End Function
