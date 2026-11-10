Attribute VB_Name = "Form_Seldate"
Attribute VB_Base = "0{C1564A76-3D4F-4336-91FC-9668B81AE1FB}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Dim Mydate As String, Salstr As String, MAHstr As String, Daystr As String
Private Sub Command23_Click()
    DoCmd.Close
End Sub

Private Sub Day_AfterUpdate()
    If IsNull(Me.Day) Or Me.Day = "" Then
        Makedate
    End If
End Sub

Private Sub Day_GotFocus()
    S = "01;02;03;04;05;06;07;08;09;10;11;12;13;14;15;16;17;18;19;20;21;22;23;24;25;26;27;28;29"
    Me.Day.RowSource = S & Me.Mah.Column(2)
End Sub

Private Sub Form_Open(Cancel As Integer)
    Makedate
End Sub

Private Sub Frame1_AfterUpdate()
    Select Case Me.Frame1
        Case 1
            Salstr = "1381;1382;1383;1384;1385;1386;1387;1388;1389;1390;1391"
            MAHstr = "01;'فروردين';';30;31';02;'ارديبهشت';';30;31';03;'خرداد';';30;31';04;'تير';';30;31';05;'مرداد';';30;31';06;'شهريور';';30;31';07;'مهر';';30';08;'آبان';';30';09;'آذر';';30';10;'دي';';30';11;'بهمن';';30';12;'اسفند';';30'"
        Case 2
            Salstr = "2001;2002;2003;2004;2005;2006;2007;2008;2009;2010;2011"
            MAHstr = "01;'January';';30;31';02;'February';';30';03;'March';';30;31';04;'April';';30';05;'May';';30;31';06;'June';';30';07;'July';';30;31';08;'August';';30;31';09;'September';';30';10;'October';';30;31';11;'November';';30';12;'December';';30;31'"
    End Select
    Me.Sal.RowSource = Salstr
    Me.Mah.RowSource = MAHstr
    Makedate
End Sub
Private Sub Mah_AfterUpdate()
    If IsNull(Me.Mah) Or Me.Mah = "" Then
        Makedate
    End If
End Sub
Private Sub Sal_AfterUpdate()
    If IsNull(Me.Sal) Or Me.Sal = "" Then
        Makedate
    End If
End Sub
Private Sub Makedate()
    If Me.Frame1 = 1 Then
        Mydate = CurrentDateFarsi(Date)
    Else
        Mydate = Replace(ConvetFarsiDateToEnglandDate(CurrentDateFarsi(Date)), "/", "")
    End If
    Salstr = Mid(Mydate, 1, 4)
    MAHstr = Mid(Mydate, 5, 2)
    Daystr = Mid(Mydate, 7, 2)
    Me.Sal = Salstr
    Me.Sal.Requery
    Me.Mah = MAHstr
    Me.Mah.Requery
    Me.Day = Daystr
    Me.Day.Requery
    Me.Mah.SetFocus
End Sub

