Attribute VB_Name = "func_tellmelast"
Option Compare Database
Option Explicit
Dim dbs As Database
Dim rst As Recordset
Dim mystr As String, myfldname As String, mytblname As String

Public Function tellmelast(Myrow As Byte) As Long
    
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
    
    mystr = "select max(" & CStr(myfldname) & ") as mymax from " & CStr(mytblname)

    Set rst = dbs.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
    
    With rst
        If IsNull(!mymax) Then
            tellmelast = 1
        Else
            tellmelast = !mymax + 1
        End If
    .Close
    End With
End Function
Public Function tellmelastbp() As Long
    Dim ssal As String
    ssal = [Forms]![Sal]![Tsal]
    ' mystr = "select max(Bpno) as mymax from Check Where Dp=2 and sal=" & [Forms]![Sal]![Tsal]
    'mystr = "SELECT Max(Bpno) AS mymax FROM Check INNER JOIN [Check] ON Mains.Gid = Check.Gid WHERE Gcode=22 AND Sal =" & "'" & CStr(ssal) & "'"
    mystr = "SELECT Max(Bpno) AS mymax FROM Check WHERE Mid([D1],1,4)= " & "'" & CStr(ssal) & "'"
    Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
    
    With rst
        If IsNull(!mymax) Then
            tellmelastbp = 1
        Else
            tellmelastbp = !mymax + 1
        End If
    .Close
    End With
    
End Function
Public Function tellmelastresid() As Long
    mystr = "select max(Rfpid) as mymax from Check where dp=1 and sal=" & [Forms]![Sal]![Tsal]

    Set rst = CurrentDb.OpenRecordset(mystr, dbOpenDynaset, dbSeeChanges)
    
    With rst
        If IsNull(!mymax) Then
           tellmelastresid = 1
        Else
           tellmelastresid = !mymax + 1
        End If
    .Close
    End With
End Function

