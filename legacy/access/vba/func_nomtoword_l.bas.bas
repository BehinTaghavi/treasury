Attribute VB_Name = "func_nomtoword_l"
Option Compare Database
Option Explicit

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
Dim Pennies As String

Static Function NUMBERTOEN(ADAD As String) As String
If IsNull(ADAD) Or ADAD = "" Then Exit Function

    If Not farsinum(1) = "One" Then
        farsinum(0) = ""
        farsinum(1) = "One"
        farsinum(2) = "Two"
        farsinum(3) = "Three"
        farsinum(4) = "Four"
        farsinum(5) = "Five"
        farsinum(6) = "Six"
        farsinum(7) = "Seven"
        farsinum(8) = "Eight"
        farsinum(9) = "Nine"
        farsinum(10) = "Ten"
        farsinum(11) = "Eleven"
        farsinum(12) = "Twelve"
        farsinum(13) = "Thirteen"
        farsinum(14) = "Fourteen"
        farsinum(15) = "Fifteen"
        farsinum(16) = "Sixteen"
        farsinum(17) = "Seventeen"
        farsinum(18) = "Eighteen"
        farsinum(19) = "Nineteen"
        farsinum(20) = "Twenty"
        farsinum(30) = "Thirty"
        farsinum(40) = "Forty"
        farsinum(50) = "Fifty"
        farsinum(60) = "Sixty"
        farsinum(70) = "Seventy"
        farsinum(80) = "Eighty"
        farsinum(90) = "Ninety"
        
        farsinum(100) = "One Hundred"
        farsinum(200) = "Two Hundred"
        farsinum(300) = "Three Hundred"
        farsinum(400) = "Four Hundred"
        farsinum(500) = "Five Hundred"
        farsinum(600) = "Six Hundred"
        farsinum(700) = "Seven Hundred"
        farsinum(800) = "Eight Hundred"
        farsinum(900) = "Nine Hundred"
    End If
StringNum = Format(ADAD, "000000000000.00")
FARSI = ""
LoopCount = 1
StartVal = 1
Pennies = Mid$(StringNum, 14, 2)

While LoopCount <= 4
seta = Mid(StringNum, StartVal, 3)
sadgan = (Val(Mid(seta, 1, 1))) * 100
dahgan = Val(Mid(seta, 2, 2))
yekan = Val(Mid(seta, 3, 1))

If Val(seta) > 99 Then
         FARSI = FARSI & farsinum(sadgan) & " "
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
    FARSI = FARSI & farsinum((Int(dahgan / 10)) * 10)
    FARSI = FARSI & " " & (farsinum(yekan))
End If

If StringNum > 999999999.99 And LoopCount = 1 Then
    FARSI = FARSI + " " + "Billion "
 End If
 
If StringNum > 999999.99 And LoopCount = 2 Then
    FARSI = FARSI + " " + "Million "
End If

If StringNum > 999.99 And LoopCount = 3 Then
    FARSI = FARSI + " " + "Thousand "
End If

LoopCount = LoopCount + 1
StartVal = StartVal + 3
 Wend
Line1:
NUMBERTOEN = Trim(FARSI) & " and " & Pennies & "/100"
End Function

