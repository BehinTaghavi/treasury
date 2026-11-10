Attribute VB_Name = "func_fdate"
Option Compare Database
Option Explicit

Static Function CurrentDateFarsi(hala As Date) As String
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
   CurrentDateFarsi = "14" & Mid(ye, 3) & IIf(mn < 10, "0" & mn, mn) & IIf(sd < 10, "0" & sd, sd)
  End Function

