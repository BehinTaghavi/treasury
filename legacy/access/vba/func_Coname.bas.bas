Attribute VB_Name = "func_Coname"
Option Compare Database
Option Explicit
Public Function Tellconame() As String
    Tellconame = Nz(DLookup("Coname", "Globals"), "نام شركت وارد نشده!")
End Function
Public Function FLnot(Lacc As Byte) As Boolean
    FLnot = IIf(Lacc < Nz(DLookup("Lnot", "Globals"), 0), True, False)
End Function
Public Function Bedtejari() As String
    Bedtejari = Nz(DLookup("Bedtejari", "Globals"), "00000")
End Function
Public Function Chnazdsand() As String
    Chnazdsand = Nz(DLookup("Checknazdsandogh", "Globals"), "00000")
End Function

Public Function BSPACE(T As String) As String
    BSPACE = Replace(T, " ", "")
    'BSPACE = IIf(IsNull(T), "و.ن", Replace(T, " ", ""))
End Function
