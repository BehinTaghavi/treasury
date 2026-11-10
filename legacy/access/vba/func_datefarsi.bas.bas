Attribute VB_Name = "func_datefarsi"

Option Compare Database
Option Explicit

Dim farsinum(1000) As String
Dim StringNum As String
Dim FARSI As String
Dim dahgan As Integer
Dim yekan As Integer
Dim LoopCount As Integer, StartVal As Integer
Dim TensDone As Integer
Dim farsimah(99) As String
Dim GSAL As String
Dim Sal As Integer
Dim Mah As Integer
Dim Day As Integer
Dim MS1 As String
Dim ER As String


Static Function DATEfarsi(d As String) As String
    If Not farsinum(1) = "يك" Then
        farsinum(0) = ""
        farsinum(1) = "يك"
        farsinum(2) = "دو"
        farsinum(3) = "سه"
        farsinum(4) = "چهار"
        farsinum(5) = "پنج"
        farsinum(6) = "شش"
        farsinum(7) = "هفت"
        farsinum(8) = "هشت"
        farsinum(9) = "نه"
        farsinum(10) = "ده"
        farsinum(11) = "يازده"
        farsinum(12) = "دوازده"
        farsinum(13) = "سيزده"
        farsinum(14) = "چهارده"
        farsinum(15) = "پانزده"
        farsinum(16) = "شانزده"
        farsinum(17) = "هفده"
        farsinum(18) = "هجده"
        farsinum(19) = "نوزده"
        farsinum(20) = "بيست"
        farsinum(30) = "سي"
        farsinum(40) = "چهل"
        farsinum(50) = "پنجاه"
        farsinum(60) = "شصت"
        farsinum(70) = "هفتاد"
        farsinum(80) = "هشتاد"
        farsinum(90) = "نود"
        farsinum(100) = "يكصد"
        farsinum(200) = "دويست"
        farsinum(300) = "سيصد"
    End If
    
    If Not farsimah(1) = "فروردين" Then
        farsimah(0) = ""
        farsimah(1) = "فروردين"
        farsimah(2) = "ارديبهشت"
        farsimah(3) = "خرداد"
        farsimah(4) = "تير"
        farsimah(5) = "مرداد"
        farsimah(6) = "شهريور"
        farsimah(7) = "مهر"
        farsimah(8) = "آبان"
        farsimah(9) = "آذر"
        farsimah(10) = "دي"
        farsimah(11) = "بهمن"
        farsimah(12) = "اسفند"
    End If
StringNum = Format(d, "00000000")
FARSI = ""
'گLoopCount = 1
'StartVal = 7
'While LoopCount <= 2
Mah = (Val(Mid(StringNum, 5, 2)))
'MsgBox (Mid(StringNum, 2, 1))

Dim spsal
Dim spsaltxt



spsal = Mid(StringNum, 2, 1)


If spsal = 4 Then
 If Val(Mid(StringNum, 3, 2)) = 0 Then
     spsaltxt = "ماه هزارو چهارصد"
   Else
     spsaltxt = "ماه هزارو چهارصدو"
 End If
Else
 spsaltxt = "ماه هزارو سيصدو"
End If
ER = 0
If Mah < 1 And Mah > 12 Then
ER = 1
DoCmd.CancelEvent
End If
GSAL = (Mid(StringNum, 7, 2))
dahgan = (Val(Mid(GSAL, 1, 2)))
yekan = (Val(Mid(GSAL, 2, 1)))

TensDone = False
If dahgan = 3 Or dahgan = 23 Or dahgan = 30 Then GoTo Line1
If dahgan < 10 Then
FARSI = FARSI & " " & farsinum(dahgan) + "م " + farsimah(Mah) + " " + spsaltxt
TensDone = True
End If
If (dahgan >= 11 And dahgan <= 19) Then
FARSI = FARSI & farsinum(dahgan) + "م " + farsimah(Mah) + " " + spsaltxt
TensDone = True
End If
If (dahgan / 10#) = Int(dahgan / 10#) Then
FARSI = FARSI & farsinum(dahgan) + "م " + farsimah(Mah) + " " + spsaltxt
TensDone = True
End If
If Not TensDone Then
    FARSI = FARSI & farsinum((Int(dahgan / 10)) * 10) + " " + "و"
    FARSI = FARSI & " " & (farsinum(yekan)) + "م " + farsimah(Mah) + " " + spsaltxt
End If
Line1:
Select Case dahgan
Case 3
FARSI = FARSI & "سوم " + farsimah(Mah) + " " + spsaltxt
Case 23
FARSI = FARSI & "بيست وسوم " + farsimah(Mah) + " " + spsaltxt
Case 30
FARSI = FARSI & "سي ام " + farsimah(Mah) + " " + spsaltxt

End Select
GSAL = (Mid(StringNum, 3, 2))
dahgan = (Val(Mid(GSAL, 1, 2)))
yekan = (Val(Mid(GSAL, 2, 1)))
TensDone = False
If dahgan < 10 Then
FARSI = FARSI & " " & farsinum(dahgan)
TensDone = True
End If
If (dahgan >= 11 And dahgan <= 19) Then
FARSI = FARSI & farsinum(dahgan)
TensDone = True
End If
If (dahgan / 10#) = Int(dahgan / 10#) Then
FARSI = FARSI & farsinum(dahgan)
TensDone = True
End If
If Not TensDone Then
    FARSI = FARSI & farsinum((Int(dahgan / 10)) * 10) + " " + "و"
    FARSI = FARSI & " " & (farsinum(yekan))
End If
DATEfarsi = Trim(FARSI) + " " + ""
End Function


Public Function Controldate(Userdate As String, Ittype As Byte) As Boolean
    If Len(Userdate) <> 8 Then
        MsgBox "تاريخ نادرست است"
        GoTo linenotok
    End If
    Dim rst As Recordset, Mylongaz As Integer, Mylongta As Integer
    
    Set rst = CurrentDb.OpenRecordset("Globals", dbOpenDynaset, dbSeeChanges)
    With rst
        If Ittype = 0 Then
            Mylongaz = Nz(!Salaz, Forms!Sal!Tsal)
            Mylongta = Nz(!Salta, Forms!Sal!Tsal)
        Else
            Mylongaz = Nz(!Sallongsaz, Forms!Sal!Tsal - 1)
            Mylongta = Nz(!Sallongsta, Forms!Sal!Tsal + 2)
        End If
    .Close
    End With
    
    Dim Mysal As Integer, Mymonth As Byte, Myday As Byte
    Mysal = Val(Mid([Userdate], 1, 4))
    Mymonth = Val(Mid([Userdate], 5, 2))
    Myday = Val(Mid([Userdate], 7, 2))
    
    If Mysal < Mylongaz Or Mysal > Mylongta Then
        MsgBox "عددسال نادرست است"
        GoTo linenotok
    End If
    
    
    If Mymonth < 1 Or Mymonth > 12 Then
        MsgBox "عددماه نادرست است"
        GoTo linenotok
    End If
    Select Case Mymonth
        Case 1 To 6
        If Myday < 1 Or Myday > 31 Then
            MsgBox "عددروز نادرست است"
            GoTo linenotok
        End If
        Case 7 To 12
        If Myday < 1 Or Myday > 30 Then
            MsgBox "عددروز نادرست است"
            GoTo linenotok
        End If
    End Select
    
    Controldate = True
    Exit Function
linenotok:
    Controldate = False
End Function


