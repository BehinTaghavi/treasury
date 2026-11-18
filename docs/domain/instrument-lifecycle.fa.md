<div dir="rtl">

# چرخه عمر اسناد (چک، سفته، اقساط)

> نسخه فارسی [instrument-lifecycle.md](instrument-lifecycle.md). در صورت اختلاف، نسخه انگلیسی مرجع کد است؛ لطفاً هر دو با هم به‌روز شوند.

مرجع در کد: `src/Services/Cashflow/Treasury.Cashflow.Domain/Instruments/InstrumentLifecycle.cs`.
برنامه قدیم: جدول `dbo.Checks` در پایگاه مشترک `Cashnajoo86`، با وضعیت در `Stcode` به‌علاوه چند پرچم؛ فرم‌های `Lcash.mdb` در جدول‌ها نام برده شده‌اند.

نام وضعیت‌ها و گام‌ها به انگلیسی آمده است، چون در کد و آزمون‌ها همین نام‌ها به کار می‌روند.

## دریافتی (در سیستم قدیم Dp = 1)

| کد قدیم | وضعیت جدید | گام | از وضعیت | فرم قدیم | انواع |
|---|---|---|---|---|---|
| ۱۱ — نزد صندوق | InCashDesk | Receive (دریافت) | — | D_Check | همه |
| ۱۲ — واگذار جهت وصول | DepositedForCollection | DepositForCollection (حساب وصول لازم است) | SayadRegistered (قاعدهٔ N3؛ در داده‌های قدیم: InCashDesk) | D_Check_Vag_m؛ مدیرعامل ۲۷ آبان ۱۴۰۵ | چک عادی، چک بانکی |
| ۱۳ — وصول شده | Cleared | RecordCleared (ثبت وصول) | DepositedForCollection | D_Check_vos_m | همه |
| ۱۴ — برگشت شده | Bounced | RecordBounced (ثبت برگشت) | DepositedForCollection | D_Check_vos_m، Checkbacked | همه |
| ۱۵ — عودت شده | ReturnedToIssuer | ReturnToIssuer (عودت به صادرکننده) | InCashDesk | D_Check_Od_m | همه |
| — عودت شده از بانک | ReturnedFromBank | ReturnFromBank (عودت به شرکت) | DepositedForCollection | بند ۱۴ (ج) | همه |
| ۱۲ — واگذار جهت وصول | DepositedForCollection | DepositForCollection (واگذاری مجدد) | ReturnedFromBank | بند ۱۴، Q33 | چک عادی، چک بانکی |
| ۱۴ — برگشت شده | Bounced | RecordBounced (شمارهٔ اعلامیهٔ برگشت الزامی؛ تاریخ مرحله تاریخ اعلامیه است) | DepositedForCollection، ReturnedFromBank | بند ۱۴ (ه)، Q33 | همه |
| ۱۴ — برگشت شده | Bounced | RecordEndorsementReturned (برگشت خرج، با شمارهٔ اعلامیه) | Endorsed | بند ۱۴ (ح) | همه |
| ۱۵ — عودت شده | ReturnedToIssuer | ReturnToIssuer | ReturnedFromBank | دستورالعمل | همه |
| — تعویض چک | Replaced | Replace (با رسید دریافت چک‌های جدید از همان پرداخت‌کننده) | InCashDesk، SayadRegistered، ReturnedFromBank، Bounced، InLegal | بند ۱۴ (د)، Q31؛ مدیرعامل ۲۷ آبان ۱۴۰۵ | غیر تضمینی |
| — ابطال شده | Cancelled | Cancel (شماره و تاریخ دستور مدیریت، علت) | InCashDesk، SayadRegistered، ReturnedFromBank، Bounced، InLegal | بند ۱۴ (و)، Q30 | همه |
| — ثبت صیادی شد | SayadRegistered | RegisterSayad (تاریخ؛ کد رهگیری ثبت، اختیاری) | InCashDesk | مدیرعامل، ۲۷ آبان ۱۴۰۵ | چک‌ها |
| ۱۵، ۱۶ | ReturnedToIssuer، Endorsed | سایر مراحل چک نزد صندوق (عودت از صندوق، خرج) | SayadRegistered | مدیرعامل، ۲۷ آبان ۱۴۰۵ | مانند InCashDesk |
| ۱۴ + GoInlaw — اقدام حقوقی | InLegal | SendToLegal (تاریخ و شرح اقدام، در صفحهٔ «اقدام حقوقی»؛ اسکن ضمائم پس از آن اضافه می‌شود) | Bounced | مدیرعامل، ۲۷ آبان ۱۴۰۵ | همه |
| — اقدام حقوقی | InLegal | SendToLegal: چک تضمینی و سفتهٔ تضمینی؛ چک عادی به‌عنوان حالت استثنایی | InCashDesk، SayadRegistered | پاسخ مدیرعامل به Q38 و ۲۷ آبان ۱۴۰۵ | چک‌ها و سفتهٔ تضمینی (نه سفتهٔ عادیِ برگشت‌نخورده) |
| ۱۶ — واگذار به سایرین | Endorsed | Endorse (خرج؛ گیرنده (هر تفصیل)، گروه بابت پرداخت، بابت، کد پیگیری صیاد) | InCashDesk | D_Check_Kh_m | چک عادی، چک بانکی |

بند ۱۴ مدیرعامل (۲۴ آبان ۱۴۰۵) و پاسخ‌های Q30 تا Q35: `docs/domain/received-cheque-steps.fa.md`. «برگشت شده» و «خرج شده» دیگر پایانی نیستند.

آخرین وضعیت طبق توضیح مدیرعامل در ۲۷ آبان ۱۴۰۵ (`received-cheque-steps.fa.md`، بخش ۵): چکی که ثبت صیادی شده همچنان نزد صندوق است و
همهٔ مراحل «نزد صندوق» برای آن باز می‌ماند؛ چک برگشتی که به وکیل تحویل شده هنوز قابل تعویض یا ابطال است.
**«سررسید چک» یک وضعیت ثبت‌شدنی نیست**: نشانه‌ای است روی چک مدت‌دار (سررسید بعد از روز دریافت) که در تاریخ سررسید یا بعد از آن هنوز
نزد صندوق یا نزد بانک است؛ برای آن مرحله‌ای ثبت نمی‌شود.

## صادره (در سیستم قدیم Dp = 2)

| کد قدیم | وضعیت جدید | گام | از وضعیت | فرم قدیم |
|---|---|---|---|---|
| ۲۰ — در جریان صدور | BeingIssued | PrepareIssue (پرداخت تأییدشده) | — | Porder |
| ۲۱ — چک صادره | Issued | Issue (یک برگ از دسته چک رزرو می‌شود) | BeingIssued | Check_Print_single |
| ۲۲ — تحویل شده | Delivered | Deliver (تحویل) | Issued | Check_deliv_m |
| ۲۶ — ابطال شده | Cancelled | Cancel (ذکر دلیل لازم است) | BeingIssued، Issued | Check_Print_single |
| ۲۱/۲۲ + Vos، RegVos | PaidByBank | RecordPaidByBank (پرداخت توسط بانک ما) | Issued، Delivered | Check_p_vos_m |
| ۲۱/۲۲ + Backed، RegVos | BouncedAtBank | RecordBouncedAtBank (برگشت در بانک ما) | Issued، Delivered | Check_p_vos_m |
| Ctocash | (همان وضعیت، IsMatured) | RecordMatured (ثبت سررسید) — فقط اسناد مدت‌دار، یک‌بار | Issued، Delivered | Check_longtocash_m |

## انواع اسناد (Checktype قدیم)

| کد قدیم | دریافتی | صادره |
|---|---|---|
| ۰ | OrdinaryCheque (چک عادی) | OrdinaryCheque (چک نقدی) |
| ۱ | GuaranteeCheque (چک انتظامی) | GuaranteeCheque (چک انتظامی) |
| ۲ | BankCheque (چک رمزدار/بانکی) | PostDatedCheque (چک مدت‌دار) |
| ۳ | Instalment (اقساط) | Instalment (اقساط) |
| ۴ | PromissoryNote (سفته) | PromissoryNote (سفته) |
| ۵ | Other (سایر) | Other (سایر) |
| ۶ | GuaranteeNote (سفته انتظامی) | GuaranteeNote (سفته انتظامی) |
| ۹ | — | پرداخت نقدی: سند (چک/سفته) محسوب نمی‌شود (Q8) |

## تاریخ هر گام در داده‌های قدیم (مورد استفاده در مهاجرت)

- دریافتی: `D1` دریافت، `D3` واگذاری جهت وصول / عودت / واگذاری به سایرین، `D4` وصول یا برگشت. چک برگشتی با پرچم `GoInlaw` با وضعیت
  «اقدام حقوقی» (InLegal) منتقل می‌شود؛ برنامهٔ قدیم تاریخی برای آن نگه نداشته، پس تاریخ برگشت برای این مرحله به کار می‌رود.
- صادره: `D1` صدور، `D2` تحویل (یا ابطال)، `D3` انتقال به سررسید، `D4` پرداخت یا برگشت توسط بانک ما.

هر گام متعلق به سال مالی‌ای است که در آن رخ می‌دهد؛ خود سند متعلق به سالی است که در آن ایجاد شده است (بخش ۷.۴ سند معماری).

## قواعد تاریخ برای عملیات جدید

- **N1** (جدید، Q6): تاریخ هر گام نمی‌تواند قبل از تاریخ گام قبلی همان سند باشد.
- **N3** (مدیرعامل، ۲۷ آبان ۱۴۰۵): **«ثبت صیادی» اجباری است**: چک فقط پس از ثبت صیادی به بانک واگذار می‌شود
  (`Instrument.SayadRegistrationRequired`). چکی که بانک عودت داده قبل از واگذاری اول ثبت صیادی شده و بدون ثبت دوباره واگذار می‌شود.
  در برنامهٔ قدیم نبود (چک مستقیم از صندوق واگذار می‌شد)؛ سابقهٔ منتقل‌شده همان‌گونه که ثبت شده نگه داشته می‌شود.
- **N2** (مدیرعامل، ۱۹ آبان ۱۴۰۵): تاریخ **وصول** چک دریافتی نمی‌تواند قبل از تاریخ سررسید باشد (`Instrument.ClearedBeforeDueDate`). واگذاری جهت وصول قبل از سررسید همچنان مجاز است. این قاعده در برنامه قدیم نبود؛ سوابق مهاجرت‌شده‌ای که آن را رعایت نمی‌کنند همان‌طور که ثبت شده‌اند نگه داشته می‌شوند و در گزارش هشدارها فهرست می‌شوند. همین قاعده برای **برگشت** چک هم صدق می‌کند (`Instrument.BouncedBeforeDueDate`؛ پاسخ Q11، ۱۹ آبان ۱۴۰۵).

</div>
