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
| ۱۲ — واگذار جهت وصول | DepositedForCollection | DepositForCollection (حساب وصول لازم است؛ چک باید قبلاً «ثبت صیادی شد» تیک خورده باشد) | InCashDesk | D_Check_Vag_m | چک عادی، چک بانکی |
| ۱۳ — وصول شده | Cleared | RecordCleared (ثبت وصول) | DepositedForCollection | D_Check_vos_m | همه |
| ۱۴ — برگشت شده | Bounced | RecordBounced (ثبت برگشت) | DepositedForCollection | D_Check_vos_m، Checkbacked | همه |
| ۱۵ — عودت شده | ReturnedToIssuer | ReturnToIssuer (عودت به صادرکننده) | InCashDesk | D_Check_Od_m | همه |
| — عودت شده از بانک | ReturnedFromBank | ReturnFromBank (عودت به شرکت) | DepositedForCollection | بند ۱۴ (ج) | همه |
| ۱۲ — واگذار جهت وصول | DepositedForCollection | DepositForCollection (واگذاری مجدد) | ReturnedFromBank | بند ۱۴، Q33 | چک عادی، چک بانکی |
| ۱۴ — برگشت شده | Bounced | RecordBounced (شمارهٔ اعلامیهٔ برگشت الزامی؛ تاریخ مرحله تاریخ اعلامیه است) | DepositedForCollection، ReturnedFromBank | بند ۱۴ (ه)، Q33 | همه |
| ۱۴ — برگشت شده | Bounced | RecordEndorsementReturned (برگشت خرج، با شمارهٔ اعلامیه) | Endorsed | بند ۱۴ (ح) | همه |
| ۱۵ — عودت شده | ReturnedToIssuer | ReturnToIssuer | ReturnedFromBank | دستورالعمل | همه |
| — تعویض شده | Replaced | Replace (با رسید دریافت چک‌های جدید از همان پرداخت‌کننده) | InCashDesk، ReturnedFromBank، Bounced | بند ۱۴ (د)، Q31 | غیر تضمینی |
| — ابطال شده | Cancelled | Cancel (شماره و تاریخ دستور مدیریت، علت) | InCashDesk، ReturnedFromBank، Bounced | بند ۱۴ (و)، Q30 | همه |
| ۱۶ — واگذار به سایرین | Endorsed | Endorse (خرج؛ گیرنده (هر تفصیل)، گروه بابت پرداخت، بابت، کد پیگیری صیاد) | InCashDesk | D_Check_Kh_m | چک عادی، چک بانکی |

بند ۱۴ مدیرعامل (۲۴ آبان ۱۴۰۵) و پاسخ‌های Q30 تا Q35: `docs/domain/received-cheque-steps.fa.md`. «برگشت شده» و «خرج شده» دیگر پایانی نیستند.

## صادره (در سیستم قدیم Dp = 2)

| کد قدیم | وضعیت جدید | گام | از وضعیت | فرم قدیم |
|---|---|---|---|---|
| ۲۰ — در جریان صدور | BeingIssued | PrepareIssue (پرداخت تأییدشده) | — | Porder |
| ۲۱ — چک صادره | Issued | Issue (یک برگ از دسته چک رزرو می‌شود) | BeingIssued | Check_Print_single |
| ۲۲ — تحویل شده | Delivered | Deliver (تحویل؛ چک باید قبلاً «ثبت صیادی شد» تیک خورده باشد) | Issued | Check_deliv_m |
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

- دریافتی: `D1` دریافت، `D3` واگذاری جهت وصول / عودت / واگذاری به سایرین، `D4` وصول یا برگشت.
- صادره: `D1` صدور، `D2` تحویل (یا ابطال)، `D3` انتقال به سررسید، `D4` پرداخت یا برگشت توسط بانک ما.

هر گام متعلق به سال مالی‌ای است که در آن رخ می‌دهد؛ خود سند متعلق به سالی است که در آن ایجاد شده است (بخش ۷.۴ سند معماری).

## قواعد تاریخ برای عملیات جدید

- **N1** (جدید، Q6): تاریخ هر گام نمی‌تواند قبل از تاریخ گام قبلی همان سند باشد.
- **N2** (مدیرعامل، ۱۹ آبان ۱۴۰۵): تاریخ **وصول** چک دریافتی نمی‌تواند قبل از تاریخ سررسید باشد (`Instrument.ClearedBeforeDueDate`). واگذاری جهت وصول قبل از سررسید همچنان مجاز است. این قاعده در برنامه قدیم نبود؛ سوابق مهاجرت‌شده‌ای که آن را رعایت نمی‌کنند همان‌طور که ثبت شده‌اند نگه داشته می‌شوند و در گزارش هشدارها فهرست می‌شوند. همین قاعده برای **برگشت** چک هم صدق می‌کند (`Instrument.BouncedBeforeDueDate`؛ پاسخ Q11، ۱۹ آبان ۱۴۰۵).

## «ثبت صیادی شد» (مدیرعامل، ۲۴ آبان ۱۴۰۵)

هر چک پیش از رفتن به بانک یا تحویل به ذینفع در سامانهٔ صیاد بانک‌ها ثبت می‌شود. کاربر روی چک «ثبت صیادی شد» را تیک می‌زند و سیستم تاریخ و ساعت کامپیوتر و نام کاربر را نگه می‌دارد. این تیک **نه وضعیت است و نه مرحله** (جدول گام‌های مجاز تغییری نکرده و سندی ندارد)؛ شرط دو گام است:

- **چک دریافتی**، روز یا مدت‌دار: پیش از تیک، `DepositForCollection` (واگذاری به بانک) ممکن نیست (`Instrument.SayadNotRegistered`) و تا آن زمان پیشنهاد هم نمی‌شود.
- **چک صادره**: پیش از تیک، `Deliver` (تحویل به ذینفع) ممکن نیست. تیک پس از گرفتن شمارهٔ چک (وضعیت Issued) ممکن است.
- فقط چک‌ها (عادی، ضمانتی، بانکی، مدت‌دار)؛ سفته و قسط در صیاد نیستند.
- تیک فقط تا وقتی برداشته می‌شود که گامِ وابسته به آن هنوز انجام نشده است (نزد صندوق یا عودت‌شده از بانک؛ صادرشده و تحویل‌نشده). چک عودت‌شده از بانک تیک خود را نگه می‌دارد.
- در برنامهٔ قدیم نبود (`Lcash.mdb` قدیمی‌تر از صیاد است). چک‌های مهاجرت‌شده بدون تیک می‌آیند؛ آنهایی که هنوز نزد صندوق‌اند پیش از واگذاری باید تیک بخورند (Q39).

</div>
