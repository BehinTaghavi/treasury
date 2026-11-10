Attribute VB_Name = "Report_Dailyrepbanks"
Attribute VB_Base = "0{1040EA6D-F05E-11D7-BC94-8154B80D7F75}"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Attribute VB_TemplateDerived = False
Attribute VB_Customizable = False
Option Compare Database
Option Explicit

Private Sub Report_Activate()
DoCmd.Maximize
Dim dbs As Database
Dim rst As Recordset
Set dbs = CurrentDb
    Set rst = dbs.OpenRecordset("Globals", dbOpenDynaset, dbSeeChanges)
    With rst
    Textco = !Coname
    .Close
    End With
End Sub

Private Sub Report_Close()
    Forms![Deaily_rep].Visible = True
End Sub

Private Sub Report_NoData(Cancel As Integer)
    MsgBox "اطلاعاتي جهت نمايش وجود ندارد"
End Sub

Private Sub Report_Open(Cancel As Integer)
    Forms![Deaily_rep].Visible = False
End Sub

