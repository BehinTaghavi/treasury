"""Writes the MADE-UP Excel fixtures used by ErpExportReaderTests, in the layout of the accounting system's exports.

No client data: every name below is invented. Layouts copied from the exports Iran Nazho Co. sent on 10 Nov 2026
(chart "تجمیعی": 8 columns, one level per row; تفصیل "محدوده": 5 columns; تفصیل "رقم": name + code with a "----" row).
Arabic ي/ك are used on purpose, as in the real exports.

    pip install xlwt && python make_fixtures.py
"""
import pathlib

import xlwt

HERE = pathlib.Path(__file__).resolve().parent


def write(name, rows):
    book = xlwt.Workbook(encoding="utf-8")
    sheet = book.add_sheet("Sheet1")
    for r, row in enumerate(rows):
        for c, value in enumerate(row):
            if value != "":
                sheet.write(r, c, value)
    book.save(str(HERE / name))


# Chart of accounts: columns شرح معين در معين، شرح معين، شرح كل، شرح گروه، معين در معين، معين، كل، گروه (right-to-left sheet).
write("chart.xls", [
    [" شرح معين در معين ", " شرح معين", " شرح كل", "شرح گروه ", "معين در معين ", "معين", "كل", "گروه"],
    ["", "", "", "داراييهاي جاري", "", "", "", 1.0],
    ["", "", "موجودي نقد و بانكها", "", "", "", 11.0, ""],
    ["", "موجودي صندوق", "", "", "", 1101.0, "", ""],
    ["", "موجودي بانكها", "", "", "", 1103.0, "", ""],
    ["", "", "حسابها و اسناد دريافتني", "", "", "", 13.0, ""],
    ["", "اسناد دريافتني تجاري", "", "", "", 1301.0, "", ""],
    ["اسناد دريافتني تجاري نمونه", "", "", "", 130100.0, "", "", ""],
    ["", "", "", "", "", "", "", ""],
    ["", "تاريخ تهيه گزارش", 1.0, "1405/08/19", "", "", "", ""],
])

# تفصیل list with type, status and cost centre ("محدوده").
write("details-with-types.xls", [
    ["وضعيت", "نوع حساب تفصيل", "مركز هزينه", "نام  حساب", "شماره حساب"],
    ["فعال", "بانكها", "نيست", "بانك نمونه شعبه مركزي جاري", 100000.0],
    ["فعال", "اشخاص وشركتها", "نيست", "شركت نمونه بازرگاني", 111078.0],
    ["غير فعال", "كاركنان كارخانه", "نيست", "كارمند نمونه", 600046.0],
    ["فعال", "مراكز هزينه توليد", "است", "مركز توليد نمونه", 770100.0],
    ["فعال", "", "نيست", "كنترل", 999999.0],
    ["", "", "", "", ""],
    ["", "1405/08/19", ":تاريخ تهيه گزارش", 1.0, ""],
])

# تفصیل list with name and code only ("رقم").
write("details-short.xls", [
    ["نام  حساب", "شماره حساب", ""],
    ["----", "", ""],
    ["بانك نمونه شعبه مركزي جاري", 100000.0, ""],
    ["بانك نمونه شعبه بازار جاري", "۱۰۰۰۰۱", ""],
    ["1405/08/19", "تاريخ تهيه گزارش", 1.0],
])

# Not an export at all (wrong file chosen).
write("unrelated.xls", [["ستون اول", "ستون دوم"], ["چيزي", 1.0]])
print("written to", HERE)
