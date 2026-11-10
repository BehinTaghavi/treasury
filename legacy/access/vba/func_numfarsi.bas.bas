Attribute VB_Name = "func_numfarsi"

Option Compare Database
Option Explicit
'Variables used in Numfarsi() procedure defined here in Declarations.
Dim farsinum(1000) As String
Dim StringNum As String
Dim seta As String
Dim FARSI As String
Dim sadgan As Integer
Dim dahgan As Integer
Dim yekan As Integer
Dim LoopCount As Integer, StartVal As Integer
Dim saddone As Integer
Dim TensDone As Integer
Dim edame As Integer
Dim bmil As Integer
Dim bzar As Integer
Static Function NUMFARSI(ADAD As String) As String
If IsNull(ADAD) Or ADAD = "" Then Exit Function
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
        farsinum(400) = "چهارصد"
        farsinum(500) = "پانصد"
        farsinum(600) = "ششصد"
        farsinum(700) = "هفتصد"
        farsinum(800) = "هشتصد"
        farsinum(900) = "نهصد"
    End If
StringNum = Format(ADAD, "000000000000000")
FARSI = ""
LoopCount = 1
StartVal = 1
While LoopCount <= 5
seta = Mid(StringNum, StartVal, 3)
sadgan = (Val(Mid(seta, 1, 1))) * 100
dahgan = Val(Mid(seta, 2, 2))
yekan = Val(Mid(seta, 3, 1))

If Val(seta) > 99 Then
     If sadgan = seta Then
          FARSI = FARSI & farsinum(sadgan)
     Else
         FARSI = FARSI & farsinum(sadgan) + " " + "و"
     End If
 End If

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

If StringNum > 999999999999# And LoopCount = 1 Then
    If (Val(Mid(StringNum, 4, 13)) = 0) Then
        FARSI = FARSI + " " + "تريليون"
        GoTo Line1
    Else
    FARSI = FARSI + " " + "هزار و"
    End If
 End If
'bmle = (Val(Mid(StringNum, 4, 3)))
If StringNum > 999999999 And LoopCount = 2 Then
    If (Val(Mid(StringNum, 7, 9)) = 0) Then
        FARSI = FARSI + " " + "ميليارد"
        GoTo Line1
    Else
    FARSI = FARSI + " " + "ميلياردو"
    End If
 End If
 bmil = (Val(Mid(StringNum, 7, 3)))
If StringNum > 999999 And LoopCount = 3 And bmil <> 0 Then
    If (Val(Mid(StringNum, 10, 6)) = 0) Then
        FARSI = FARSI + " " + "ميليون"
        GoTo Line1
    Else
    FARSI = FARSI + " " + "ميليون و"
    End If
End If
 bzar = (Val(Mid(StringNum, 10, 3)))
If StringNum > 999 And LoopCount = 4 And bzar <> 0 Then
    If (Val(Mid(StringNum, 13, 3)) = 0) Then
        FARSI = FARSI + " " + "هزار"
        GoTo Line1
        Else
    FARSI = FARSI + " " + "هزارو"
    End If
End If

LoopCount = LoopCount + 1
StartVal = StartVal + 3
 Wend
Line1:
NUMFARSI = Trim(FARSI)
End Function


