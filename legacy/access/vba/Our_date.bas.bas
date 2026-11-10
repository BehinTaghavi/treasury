Attribute VB_Name = "Our_date"
Option Compare Database

'Public d As New Cdate1
Public Function CurrentDate(FarsiOrMiladi As Boolean) As String
If FarsiOrMiladi = True Then CurrentDate = CurrentDateFarsi(Date)
If FarsiOrMiladi = False Then CurrentDate = CurrentDateEngland()
End Function
Public Function ConvetFarsiDateToEnglandDate(shamsiDate As String) As String
 Dim i As Integer
 Dim S As String
 Dim MiladiDate As String
 shamsiDate = Delete_Slash(shamsiDate)
 S = CurrentDateFarsi(Date)
 i = DefranceOfTwoDate(shamsiDate, S, True)
 MiladiDate = CurrentDateEngland()
 ConvetFarsiDateToEnglandDate = AddNumberAndConvertToNewDate(MiladiDate, i, False)
End Function

Public Function IsKabiseh(year_k As Integer) As Boolean
 Dim mk As Long
 
 'If year_k < 1300 Then year_k = year_k + 1300
 mk = IIf(year_k < 1374, 1366, 1379)
 If (year_k = 1371) Then IsKabiseh = True: Exit Function
 If (year_k = 1370) Then IsKabiseh = False: Exit Function
 mk = mk - year_k
 If mk < 0 Then mk = -mk
 IsKabiseh = IIf((mk Mod 4) = 0, True, False)
 If mk = 0 Then IsKabiseh = True
End Function


Public Function NumberWeekOfYear(y As String, englandORfarsi As Boolean) As Integer
Dim Op1, Op2 As Integer
Dim sum1, sum2 As Double
Dim S11, S12, S2 As Date
Dim d, D1, D2 As Integer
If englandORfarsi = True Then
sum1 = 0
sum2 = 0
sum1 = NumberOfDate(y, englandORfarsi)
sum2 = sum1 Mod 7
sum1 = (sum1 \ 7)
If sum2 <> 0 Then sum1 = sum1 + 1
NumberWeekOfYear = sum1
Else
If Len(y) > 6 Then y = Right(y, 6)
 S11 = DateSerial(Val(Left(y, 2)), Val(Mid(y, 3, 2)), Val(Right(y, 2)))
 S12 = DateSerial(Val(Left(y, 2)), Val(Mid(y, 3, 2)), Val(Right(y, 2) + 1))
 S2 = DateSerial(Val(Left(y, 2)), 1, 1)
 d = DateDiff("ww", S11, S2)
  If d < 0 Then d = -d
  D1 = DateDiff("ww", S11, S2)
  D2 = DateDiff("ww", S12, S2)
  If D1 = D2 + 1 Then d = d + 1
 NumberWeekOfYear = d
End If
End Function

Public Function NumberMonthOfYear(y As String) As Integer
  NumberMonthOfYear = Val(Left(Right(y, 4), 2))
End Function

Public Function NameMonthOfYear(y As String, englandORfarsi As Boolean) As String
Dim op As Integer
op = NumberMonthOfYear(y)
If englandORfarsi = True Then
Select Case op
Case 1:  NameMonthOfYear = "???????"
Case 2:  NameMonthOfYear = "????????"
Case 3: NameMonthOfYear = "?????"
Case 4: NameMonthOfYear = "???"
Case 5: NameMonthOfYear = "?????"
Case 6: NameMonthOfYear = "??????"
Case 7: NameMonthOfYear = "???"
Case 8: NameMonthOfYear = "????"
Case 9: NameMonthOfYear = "???"
Case 10: NameMonthOfYear = "??"
Case 11: NameMonthOfYear = "????"
Case 12: NameMonthOfYear = "?????"
End Select
Else
Select Case op
Case 1: NameMonthOfYear = "Janvary"
Case 2: NameMonthOfYear = "February"
Case 3: NameMonthOfYear = "March"
Case 4: NameMonthOfYear = "April"
Case 5: NameMonthOfYear = "May"
Case 6: NameMonthOfYear = "June"
Case 7: NameMonthOfYear = "Jule"
Case 8: NameMonthOfYear = "August"
Case 9: NameMonthOfYear = "September"
Case 10: NameMonthOfYear = "October"
Case 11: NameMonthOfYear = "November"
Case 12: NameMonthOfYear = "December"
End Select
End If
End Function



Public Function NumberOfDate(y As String, englandORfarsi As Boolean) As Integer
 Dim Op1, Op2 As Integer
 Dim S1, S2 As Date
 Dim d As Integer
 If englandORfarsi = True Then
 Op1 = Val(Right(y, 2)) ' DAY
 Op2 = Val(Left(Right(y, 4), 2)) ' MOUNTH
 If Op2 > 6 Then
  Op2 = Op2 - 6
  sum1 = sum1 + 186
  sum1 = ((Op2 - 1) * 30) + sum1
 Else
  sum1 = (Op2 - 1) * 31
 End If
 sum1 = sum1 + Op1
  NumberOfDate = sum1
 Else
If Len(y) > 6 Then y = Right(y, 6)
 S1 = DateSerial(Val(Left(y, 2)), Val(Mid(y, 3, 2)), Val(Right(y, 2)))
 S2 = DateSerial(Val(Left(y, 2)), 1, 0)
 d = DateDiff("y", S1, S2)
 If d < 0 Then d = -d
NumberOfDate = d
 End If
End Function

Public Function DefranceOfTwoDate(Y1 As String, Y2 As String, englandORfarsi As Boolean) As Integer
 Dim j, Op2, Op1, Date1, Date2 As Integer
 Dim i As Integer
 Dim S1, S2 As Date
 If englandORfarsi = True Then
 Y1 = Right(Y1, 6)
  Y2 = Right(Y2, 6)
 Date1 = Val(Left(Y1, 2))
 Date2 = Val(Left(Y2, 2))
 Op1 = (Date1 - Date2) * 365
 If Op1 < 0 Then Op2 = NumberOfDate(Y1, englandORfarsi) + Op1 - NumberOfDate(Y2, englandORfarsi)
 If Op1 > 0 Then Op2 = NumberOfDate(Y1, englandORfarsi) - Op1 + NumberOfDate(Y2, englandORfarsi)
 If Op1 = 0 Then Op2 = NumberOfDate(Y1, englandORfarsi) - NumberOfDate(Y2, englandORfarsi)
 j = 0
 If Date2 > Date1 Then
 For i = Date1 To Date2 Step 1
  If IsKabiseh(i) = True Then j = j + 1
Next i
End If
If Date2 < Date1 Then
 For i = Date2 To Date1 Step 1
  If IsKabiseh(i) = True Then j = j + 1
Next i
End If
If Date2 < Date1 Then DefranceOfTwoDate = -(Op2 + j): Exit Function
If Date2 > Date1 Then DefranceOfTwoDate = (Op2 + j): Exit Function
If Date2 = Date1 Then DefranceOfTwoDate = Op2
Else
If Len(Y1) > 6 Then Y1 = Right(Y1, 6)
If Len(Y2) > 6 Then Y2 = Right(Y2, 6)
 S1 = DateSerial(Val(Left(Y1, 2)), Val(Mid(Y1, 3, 2)), Val(Right(Y1, 2)))
 S2 = DateSerial(Val(Left(Y2, 2)), Val(Mid(Y2, 3, 2)), Val(Right(Y2, 2)))
DefranceOfTwoDate = DateDiff("y", S1, S2)
End If
End Function

Public Function LessOrGreat(Y1 As String, Y2 As String) As Boolean
 Y1 = d.Delete_Slash(Y1)
 Y2 = d.Delete_Slash(Y2)
 If Val(Left(Y1, 2)) > Val(Left(Y2, 2)) Then less_or_great = True: Exit Function
 If Val(Left(Right(Y1, 4), 2)) > Val(Left(Right(Y2, 4), 2)) Then less_or_great = True: Exit Function
 If Val(Right(Right(Y1, 4), 2)) > Val(Right(Right(Y2, 4), 2)) Then less_or_great = True: Exit Function
 LessOrGreat = False
End Function


Public Function AddNumberAndConvertToNewDate(y As String, number As Integer, farsiORengland As Boolean) As String
 Dim j, i, D1, m1, Y1 As Integer
 Dim j1, j2 As String
 Dim S As Date
 Dim st As String
 Dim help1, help2, help3 As String
 If farsiORengland = True Then
 Y1 = Val(Left(Right(y, 6), 2))
 m1 = Val(Left(Right(y, 4), 2))
 D1 = Val(Right(Right(y, 4), 2))
 If number = 0 Then AddNumberAndConvertToNewDate = y
 If number > 0 Then
 For i = 1 To number Step 1
  D1 = D1 + 1
  If m1 < 7 Then
   If D1 > 31 Then
      D1 = 1
      m1 = m1 + 1
   End If
  End If
 If m1 < 12 And m1 > 6 Then
   If D1 > 30 Then
      D1 = 1
      m1 = m1 + 1
   End If
 End If
 If m1 = 12 Then
  j = IIf(IsKabiseh(Y1) = True, 30, 29)
   If D1 > j Then
      D1 = 1
      m1 = m1 + 1
      If m1 > 12 Then Y1 = Y1 + 1: m1 = 1
   End If
  End If
 Next i
j1 = ""
j2 = ""
 If Len(Str(D1)) < 3 Then j1 = "0"
 If Len(Str(m1)) < 3 Then j2 = "0"
 help1 = IIf(Len(LTrim(Str(D1))) < 2, j1 + LTrim(Str(D1)), LTrim(Str(D1)))
 help2 = IIf(Len(LTrim(Str(m1))) < 2, j2 + LTrim(Str(m1)), LTrim(Str(m1)))
 AddNumberAndConvertToNewDate = LTrim(Str(Y1)) + help2 + help1
 End If
 If number < 0 Then
 For i = 1 To -number Step 1
  D1 = D1 - 1
  If m1 < 8 And m1 > 1 Then
   If D1 = 0 Then
      D1 = 31
      m1 = m1 - 1
   End If
  End If
 If m1 < 13 And m1 > 7 Then
   If D1 = 0 Then
      D1 = 30
      m1 = m1 - 1
   End If
 End If
 If m1 = 1 Then
  j = IIf(IsKabiseh(Y1 - 1) = True, 30, 29)
   If D1 = 0 Then
      D1 = j
      m1 = 12
      Y1 = Y1 - 1
   End If
  End If
 Next i
j1 = ""
j2 = ""
 If Len(Str(D1)) < 3 Then j1 = "0"
 If Len(Str(m1)) < 3 Then j2 = "0"
 help1 = IIf(Len(LTrim(Str(D1))) < 2, j1 + LTrim(Str(D1)), LTrim(Str(D1)))
 help2 = IIf(Len(LTrim(Str(m1))) < 2, j2 + LTrim(Str(m1)), LTrim(Str(m1)))
 AddNumberAndConvertToNewDate = LTrim(Str(Y1)) + help2 + help1
 End If
 Else
'******************************************************
'******************   For England Date   *******************
'  *****************  if   input = false   **********************
If Len(y) > 6 Then y = Right(y, 6)
 S = DateSerial(Int(Left(y, 2)), Int(Mid(y, 3, 2)), Int(Right(y, 2)))
 AddNumberAndConvertToNewDate = DateAdd("d", number, S)
End If
End Function
Public Function Format1_YYMMDD(y As String) As String
  y = Trim(y)
  Format1_YYMMDD = LTrim((Left(y, Len(y) - 4))) + "/" + LTrim(Left(Right(y, 4), 2)) + "/" + LTrim(Right(Right(y, 4), 2))
End Function

Public Function Format2_DDMMYY(y As String) As String
 y = Right(y, 6)
  Format2_DDMMYY = LTrim((Left(y, 2))) + "_" + LTrim(Left(Right(y, 4), 2)) + "_" + LTrim(Right(Right(y, Len(y) - 4), 2))
End Function
Public Function Format3_YYMMDD(y As String) As String
  Format3_YYMMDD = LTrim((Left(y, Len(y) - 4))) + "/" + LTrim(Left(Right(y, 4), 2)) + "/" + LTrim(Right(Right(y, 4), 2))
End Function

Public Function Find_ID_Day(new_y As String, englandORfarsi As Boolean) As Integer
Dim sum As Integer
Dim op As Integer
Dim i As Integer
Dim current_y As String
Dim S As Date
Dim Mydate
If englandORfarsi = True Then
Mydate = Weekday(Date)
sum = 0
current_y = CurrentDateFarsi(Date)
sum = DefranceOfTwoDate(current_y, new_y, englandORfarsi)
i = Mydate
If sum > 0 Then
 For op = 1 To sum Step 1
  If i = 7 Then i = 0
  i = i + 1
 Next op
End If
If sum < 0 Then
 For op = 1 To -sum Step 1
  If i = -1 Then i = 6
  i = i - 1
  If i = -1 Then i = 6
 Next op
End If
Else
If Len(new_y) > 6 Then new_y = Right(new_y, 6)
S = DateSerial(Val(Left(new_y, 2)), Val(Mid(new_y, 3, 2)), Val(Right(new_y, 2)))
i = DatePart("d", S)
End If
Find_ID_Day = i
End Function

Public Function FindNameDay(i As Integer, englandORfarsi As Boolean) As String
If englandORfarsi = True Then
If i = 1 Then FindNameDay = "يكشنبه": Exit Function
If i = 2 Then FindNameDay = "دوشنبه": Exit Function
If i = 3 Then FindNameDay = "سه شنبه": Exit Function
If i = 4 Then FindNameDay = "چهارشنبه": Exit Function
If i = 5 Then FindNameDay = "پنج شنبه": Exit Function
If i = 6 Then FindNameDay = "جمعه": Exit Function
FindNameDay = "شنبه"
Else
i = i + 1
If i = 1 Then FindNameDay = "Sunday": Exit Function
If i = 2 Then FindNameDay = "Monday": Exit Function
If i = 3 Then FindNameDay = "Tuesday": Exit Function
If i = 4 Then FindNameDay = "Wednesday": Exit Function
If i = 5 Then FindNameDay = "Tursday": Exit Function
If i = 6 Then FindNameDay = "Friyday": Exit Function
FindNameDay = "Saturday"
End If
End Function


Public Function Century(y As String, i As Integer) As String
  Century = IIf(Len(y) = 8, LTrim(Str(i)) + LTrim(Right(y, 6)), LTrim(Str(i)) + LTrim(y))
End Function

Public Function DigitDay(y As String) As String
  DigitDay = Len(LTrim(y))
End Function

 Public Function Convert_To_Four_Digit_Year(number As Integer, y As String, englandORfarsi As Boolean)
  If englandORfarsi = True Then
  Convert_To_Four_Digit_Year = Century(y, number)
  Else
 Convert_To_Four_Digit_Year = Century(y, number)
  End If
 End Function

Private Function CurrentDateEngland() As String
Dim j, j1 As String
j = ""
j1 = ""
If Len(Str(Day(Date))) < 3 Then j = "0"
If Len(Str(Month(Date))) < 3 Then j1 = "0"
  CurrentDateEngland = LTrim(Str(Year(Date))) + LTrim(LTrim(j1) + LTrim(Str(Month(Date)))) + LTrim(LTrim(j) + LTrim(Str(Day(Date))))
End Function


Private Function CurrentDateFarsi(hala As Date) As String
    Dim E(12), d(3, 12), P(3, 12), N(2, 12)
    E(1) = 31: E(2) = 28: E(3) = 31: E(4) = 30: E(5) = 31: E(6) = 30: E(7) = 31: E(8) = 31: E(9) = 30: E(10) = 31: E(11) = 30: E(12) = 31
    d(1, 1) = 20: d(1, 2) = 19: d(1, 3) = 19: d(1, 4) = 19: d(1, 5) = 20: d(1, 6) = 20: d(1, 7) = 21: d(1, 8) = 21: d(1, 9) = 21: d(1, 10) = 21: d(1, 11) = 20: d(1, 12) = 20
    d(2, 1) = 19: d(2, 2) = 18: d(2, 3) = 20: d(2, 4) = 40: d(2, 5) = 21: d(2, 6) = 21: d(2, 7) = 22: d(2, 8) = 22: d(2, 9) = 22: d(2, 10) = 22: d(2, 11) = 21: d(2, 12) = 21
    d(3, 1) = 20: d(3, 2) = 19: d(3, 3) = 20: d(3, 4) = 20: d(3, 5) = 21: d(3, 6) = 21: d(3, 7) = 22: d(3, 8) = 22: d(3, 9) = 22: d(3, 10) = 22: d(3, 11) = 21: d(3, 12) = 21
    P(1, 1) = 11: P(1, 2) = 12: P(1, 3) = 11: P(1, 4) = 13: P(1, 5) = 12: P(1, 6) = 12
    P(1, 7) = 11: P(1, 8) = 11: P(1, 9) = 11: P(1, 10) = 10: P(1, 11) = 11: P(1, 12) = 11
    P(2, 1) = 12: P(2, 2) = 13: P(2, 3) = 11: P(2, 4) = 12: P(2, 5) = 11: P(2, 6) = 11
    P(2, 7) = 10: P(2, 8) = 10: P(2, 9) = 10: P(2, 10) = 9: P(2, 11) = 10: P(2, 12) = 10
    P(3, 1) = 11: P(3, 2) = 12: P(3, 3) = 10: P(3, 4) = 12: P(3, 5) = 11: P(3, 6) = 11
    P(3, 7) = 10: P(3, 8) = 10: P(3, 9) = 10: P(3, 10) = 9: P(3, 11) = 10: P(3, 12) = 10
    N(1, 1) = 10: N(1, 2) = 11: N(1, 3) = 12: N(1, 4) = 1: N(1, 5) = 2: N(1, 6) = 3
    N(1, 7) = 4: N(1, 8) = 5: N(1, 9) = 6: N(1, 10) = 7: N(1, 11) = 8: N(1, 12) = 9
    N(2, 1) = 11: N(2, 2) = 12: N(2, 3) = 1: N(2, 4) = 2: N(2, 5) = 3: N(2, 6) = 4
    N(2, 7) = 5: N(2, 8) = 6: N(2, 9) = 7: N(2, 10) = 8: N(2, 11) = 9: N(2, 12) = 10
    Dim yy, ye, mn, ls, i, sd, z, j, DD, U, rag, H, ym, x, rp, W, S, kkb As Integer
    Dim ny, r As String
    r = Format(Now, "mm")
    yy = Year(hala): j = r: DD = Day(hala)
    U = 0
    If yy Mod 4 = 0 Then U = 1
    ny = CStr(yy)
    r = Right(ny, 2)
    rag = Val(r)
    If rag = 0 And yy Mod 400 <> 0 Then U = 0
    H = 0
    If j = 2 And U = 1 Then H = 1
    ym = yy - 622
    x = Int((ym + 15) / 33)
    rp = 0
    W = ym - x - 17
    If W Mod 4 = 0 Then rp = 1
    S = Int((ym + 16) / 33)
    kkb = S * 33 - 16
    If kkb + 1 = ym Then rp = 0
    If U = 1 And rp = 0 Then i = 1
    If U = 0 And rp = 1 Then i = 2
    If U = 0 And rp = 0 Then i = 3
    If U = 1 And rp = 1 Then i = 4
    z = 0
    If i = 4 And j = 3 Then z = 1
    If i = 4 Then i = 3
    If 1 <= DD And DD <= d(i, j) Then
        sd = P(i, j) + DD + z - 1
        mn = N(1, j)
        ls = 1
    Else
        sd = DD - d(i, j)
        mn = N(2, j)
        ls = 2
    End If
    If j <= 3 Then ye = yy - 622
    If j = 3 And ls = 2 Then ye = yy - 621
    If j > 3 Then ye = yy - 621
    If sd > 31 Then sd = sd - 31: mn = mn + 1
   CurrentDateFarsi = ye & IIf(mn < 10, "0" & mn, mn) & IIf(sd < 10, "0" & sd, sd)
  End Function
  
Private Function N(x As Variant) As Variant
  If IsNull(x) Then
    N = 0
  Else
    N = x
  End If
End Function

Public Function Delete_Slash(y As String) As String

  Dim i As Integer
  Dim S, S1 As String
  For i = 1 To Len(y) Step 1
  S1 = Mid(y, i, 1)
   If (S1 <> "/") And (S1 <> "_") And (S1 <> ".") Then S = S + S1
  Next i
  Delete_Slash = S
End Function

Public Function ConvetEnglandDateToFarsiDate(miladi As String) As String
 Dim i As Integer
 Dim S As String
 Dim shamsiDate As String
miladi = Delete_Slash(miladi)
 S = CurrentDateEngland
  i = DefranceOfTwoDate(miladi, S, False)
 shamsiDate = CurrentDateFarsi(Date)
 
 ConvetEnglandDateToFarsiDate = AddNumberAndConvertToNewDate(shamsiDate, -i, True)
 If Val(ConvetEnglandDateToFarsiDate) < 13000000 Then
    ConvetEnglandDateToFarsiDate = "13" & ConvetEnglandDateToFarsiDate
 End If
 
End Function
'not completed yet
Public Function CheckValidFarsiDate(p_format_id As Integer, p_Date As String) As Boolean
Dim OK  As Boolean
    
    Select Case p_format_id
        Case 1 'yy/mm/dd format
            OK = True
            Dim y As String
            Dim M As String
            Dim d As String
            Dim length As Integer
            length = Len(p_Date)
            If length <> 8 And length <> 7 And length <> 6 Then
                OK = False
                CheckValidFarsiDate = False
                Exit Function
            End If
            y = Left(p_Date, 2)
            If Not IsNumeric(Val(y)) Then
                OK = False
            End If
            If Mid(p_Date, 3, 1) <> "/" Then
                OK = False
            End If
            Dim i, j As Integer
            i = 4
            While Mid(p_Date, i, 1) <> "/"
            j = j + 1
            i = i + 1
            Wend
            If j = 1 Then
                M = Mid(p_Date, 4, 1)
                If Mid(p_Date, 5, 1) <> "/" Then
                    OK = False
                End If
                d = Mid(p_Date, 6)
            ElseIf j = 2 Then
                M = Mid(p_Date, 4, 2)
                If Mid(p_Date, 6, 1) <> "/" Then
                    OK = False
                End If
                d = Mid(p_Date, 7)
            End If
            If Not IsNumeric(Val(M)) Then
                OK = False
            Else
                If Val(M) < 1 Or Val(M) > 12 Then
                    OK = False
                End If
            End If
            If Not IsNumeric(Val(d)) Then
                OK = False
            Else
                If Val(d) < 1 Or Val(d) > 31 Then
                    OK = False
                End If
            End If
    End Select
    CheckValidFarsiDate = OK
End Function

Public Function DateOk(ByVal fDate As String) As Boolean
Dim iYear, iMonth, iDay As Integer
  If Len(Trim(fDate)) = 0 Then
     DateOk = True
  Else
     iYear = Val(Mid(fDate, 1, 4))
     iMonth = Val(Mid(fDate, 5, 2))
     iDay = Val(Mid(fDate, 7, 2))
     If iYear = 0 Or iMonth = 0 Or iDay = 0 Or iMonth > 12 Or _
        iDay > 31 Or (iMonth > 6 And iDay > 30) Then
        DateOk = False
     ElseIf iMonth = 12 Then
        If iDay <= 29 Then
           DateOk = True
        ElseIf (iYear < 1374 And (iYear Mod 4 = 2)) Or _
               (iYear > 1374 And (iYear Mod 4 = 3)) Then
           DateOk = True
        Else
           DateOk = False
        End If
     Else
        DateOk = True
     End If
  End If
End Function




